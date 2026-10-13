using Moq;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class RelatorioCategoriaServiceTests
{
    private readonly Mock<ILancamentoRepository> _mockLancamentoRepository;
    private readonly Mock<ICategoriaRepository> _mockCategoriaRepository;
    private readonly Mock<ILimiteGastoRepository> _mockLimiteGastoRepository;
    private readonly RelatorioCategoriaService _service;

    private readonly Conta _contaBanco = new() { Id = Guid.NewGuid(), Nome = "Nubank Conta", Tipo = TipoConta.Banco };
    private readonly Conta _contaCartao = new() { Id = Guid.NewGuid(), Nome = "Nubank Cartao", Tipo = TipoConta.Cartao };

    public RelatorioCategoriaServiceTests()
    {
        _mockLancamentoRepository = new Mock<ILancamentoRepository>();
        _mockCategoriaRepository = new Mock<ICategoriaRepository>();
        _mockLimiteGastoRepository = new Mock<ILimiteGastoRepository>();
        _service = new RelatorioCategoriaService(
            _mockLancamentoRepository.Object,
            _mockCategoriaRepository.Object,
            _mockLimiteGastoRepository.Object);

        ConfigurarCategorias();
    }

    #region Resumo - Regra 1: Validacao de ano/mes (ValorInvalidoException)

    [Fact]
    public async Task ObterResumoAsync_MesZero_LancaValorInvalidoExcecao()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(() => _service.ObterResumoAsync(2026, 0));
    }

    [Fact]
    public async Task ObterResumoAsync_Mes13_LancaValorInvalidoExcecao()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(() => _service.ObterResumoAsync(2026, 13));
    }

    [Fact]
    public async Task ObterResumoAsync_AnoZero_LancaValorInvalidoExcecao()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(() => _service.ObterResumoAsync(0, 3));
    }

    #endregion

    #region Resumo - Regra 2: Janeiro consulta dezembro do ano anterior como mes anterior

    [Fact]
    public async Task ObterResumoAsync_Janeiro_ConsultaDezembroDoAnoAnterior()
    {
        // Arrange
        ConfigurarLancamentos(2026, 1);
        ConfigurarLancamentos(2025, 12);

        // Act
        await _service.ObterResumoAsync(2026, 1);

        // Assert
        _mockLancamentoRepository.Verify(r => r.ListarDebitosParaRelatorioAsync(2025, 12), Times.Once);
        _mockLancamentoRepository.Verify(r => r.ListarDebitosParaRelatorioAsync(2026, 1), Times.Once);
    }

    #endregion

    #region Resumo - Regra 3: Total, total do mes anterior e variacao

    [Fact]
    public async Task ObterResumoAsync_CalculaTotalTotalAnteriorEVariacao()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 2, NovoDebito(100m, categoria.Id));
        ConfigurarLancamentos(2026, 3, NovoDebito(120m, categoria.Id));

        // Act
        var resposta = await _service.ObterResumoAsync(2026, 3);

        // Assert
        Assert.Equal(2026, resposta.Ano);
        Assert.Equal(3, resposta.Mes);
        Assert.Equal(120m, resposta.TotalGasto);
        Assert.Equal(100m, resposta.TotalMesAnterior);
        Assert.Equal<decimal?>(20.00m, resposta.VariacaoPercentual);

        var linha = Assert.Single(resposta.Linhas);
        Assert.Equal(categoria.Id, linha.CategoriaId);
        Assert.Equal("Mercado", linha.Nome);
        Assert.Equal(120m, linha.Valor);
        Assert.Equal(100.00m, linha.Percentual);
    }

    [Fact]
    public async Task ObterResumoAsync_MesAnteriorZero_VariacaoNula()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 2);
        ConfigurarLancamentos(2026, 3, NovoDebito(120m, categoria.Id));

        // Act
        var resposta = await _service.ObterResumoAsync(2026, 3);

        // Assert
        Assert.Equal(0m, resposta.TotalMesAnterior);
        Assert.Null(resposta.VariacaoPercentual);
    }

    [Fact]
    public async Task ObterResumoAsync_LancamentoSemCategoria_AparecComCategoriaIdNulo()
    {
        // Arrange
        ConfigurarLancamentos(2026, 2);
        ConfigurarLancamentos(2026, 3, NovoDebito(45m, null));

        // Act
        var resposta = await _service.ObterResumoAsync(2026, 3);

        // Assert
        var linha = Assert.Single(resposta.Linhas);
        Assert.Null(linha.CategoriaId);
        Assert.Equal("Sem categoria", linha.Nome);
        Assert.Null(linha.Icone);
        Assert.Equal(45m, linha.Valor);
    }

    #endregion

    #region Detalhe - Regra 4: Validacao de ano/mes (ValorInvalidoException)

    [Fact]
    public async Task ObterDetalheAsync_MesZero_LancaValorInvalidoExcecao()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.ObterDetalheAsync(2026, 0, Guid.NewGuid()));
    }

    [Fact]
    public async Task ObterDetalheAsync_Mes13_LancaValorInvalidoExcecao()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.ObterDetalheAsync(2026, 13, Guid.NewGuid()));
    }

    [Fact]
    public async Task ObterDetalheAsync_AnoZero_LancaValorInvalidoExcecao()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.ObterDetalheAsync(0, 3, Guid.NewGuid()));
    }

    #endregion

    #region Detalhe - Regra 5: Categoria precisa ser de primeiro nivel e Despesa

    [Fact]
    public async Task ObterDetalheAsync_Subcategoria_LancaCategoriaNaoEhPrimeiroNivelExcecao()
    {
        // Arrange
        var raiz = Despesa("Alimentacao");
        var sub = Despesa("Restaurante", parentId: raiz.Id);
        ConfigurarCategorias(raiz, sub);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaNaoEhPrimeiroNivelException>(
            () => _service.ObterDetalheAsync(2026, 3, sub.Id));

        Assert.Equal(sub.Id, excecao.CategoriaId);
    }

    [Fact]
    public async Task ObterDetalheAsync_CategoriaReceita_LancaCategoriaNaoEhPrimeiroNivelExcecao()
    {
        // Arrange
        var receita = Receita("Salario");
        ConfigurarCategorias(receita);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaNaoEhPrimeiroNivelException>(
            () => _service.ObterDetalheAsync(2026, 3, receita.Id));

        Assert.Equal(receita.Id, excecao.CategoriaId);
    }

    #endregion

    #region Detalhe - Regra 6: Categoria inexistente lanca CategoriaNaoEncontradaException

    [Fact]
    public async Task ObterDetalheAsync_CategoriaInexistente_LancaCategoriaNaoEncontradaExcecao()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        ConfigurarCategorias(Despesa("Mercado"));

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaNaoEncontradaException>(
            () => _service.ObterDetalheAsync(2026, 3, idInexistente));

        Assert.Equal(idInexistente, excecao.CategoriaId);
    }

    #endregion

    #region Detalhe - Regra 7: categoriaId null e o detalhe de "Sem categoria" (sem limite)

    [Fact]
    public async Task ObterDetalheAsync_SemCategoriaId_RetornaSemCategoriaSemLimite()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        var semCategoria = NovoDebito(45m, null);
        ConfigurarLancamentos(2026, 3, semCategoria, NovoDebito(10m, categoria.Id));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, null);

        // Assert
        Assert.Null(resposta.CategoriaId);
        Assert.Equal("Sem categoria", resposta.Nome);
        Assert.Null(resposta.Icone);
        Assert.Equal(45m, resposta.Total);
        Assert.Null(resposta.Limite);

        var unico = Assert.Single(resposta.Lancamentos);
        Assert.Equal(semCategoria.Id, unico.Id);
        Assert.Equal(45m, unico.Valor);

        _mockLimiteGastoRepository.Verify(r => r.ObterPorCategoriaId(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region Detalhe - Regra 8: Limite presente -> GastoRealizado == Total da linha

    [Fact]
    public async Task ObterDetalheAsync_ComLimite_RetornaValorLimiteEGastoRealizadoIgualAoTotal()
    {
        // Arrange
        var categoria = Despesa("Mercado", icone: "cart");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 3, NovoDebito(120m, categoria.Id), NovoDebito(80m, categoria.Id));
        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoria.Id))
            .ReturnsAsync(new LimiteGasto { Id = Guid.NewGuid(), CategoriaId = categoria.Id, ValorLimite = 500m });

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        Assert.Equal(categoria.Id, resposta.CategoriaId);
        Assert.Equal("Mercado", resposta.Nome);
        Assert.Equal("cart", resposta.Icone);
        Assert.Equal(200m, resposta.Total);
        Assert.NotNull(resposta.Limite);
        Assert.Equal(500m, resposta.Limite!.ValorLimite);
        Assert.Equal(resposta.Total, resposta.Limite.GastoRealizado);
    }

    [Fact]
    public async Task ObterDetalheAsync_SemLimite_LimiteNulo()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 3, NovoDebito(120m, categoria.Id));
        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoria.Id))
            .ReturnsAsync((LimiteGasto?)null);

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        Assert.Null(resposta.Limite);
    }

    #endregion

    #region Detalhe - Regra 9: PercentualDoTotal usa o total gasto do mes inteiro (item 19)

    [Fact]
    public async Task ObterDetalheAsync_PercentualDoTotal_UsaTotalDoMesInteiro()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var outra = Despesa("Lazer");
        ConfigurarCategorias(categoria, outra);
        ConfigurarLancamentos(2026, 3, NovoDebito(100m, categoria.Id), NovoDebito(300m, outra.Id));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        Assert.Equal(100m, resposta.Total);
        Assert.Equal(25.00m, resposta.PercentualDoTotal);
    }

    #endregion

    #region Detalhe - Regra 10: SubcategoriaNome preenchido so quando o lancamento esta na subcategoria

    [Fact]
    public async Task ObterDetalheAsync_SubcategoriaNome_PreenchidoSoQuandoLancamentoEstaNaSubcategoria()
    {
        // Arrange
        var raiz = Despesa("Alimentacao");
        var sub = Despesa("Restaurante", parentId: raiz.Id);
        ConfigurarCategorias(raiz, sub);
        ConfigurarLancamentos(
            2026,
            3,
            NovoDebito(50m, sub.Id),
            NovoDebito(30m, raiz.Id));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, raiz.Id);

        // Assert
        Assert.Equal(80m, resposta.Total);
        Assert.Equal(2, resposta.Lancamentos.Count);
        Assert.Equal("Restaurante", resposta.Lancamentos.Single(l => l.Valor == 50m).SubcategoriaNome);
        Assert.Null(resposta.Lancamentos.Single(l => l.Valor == 30m).SubcategoriaNome);
    }

    #endregion

    #region Detalhe - Regra 11: ContaTipo "CARTAO" para conta cartao e "BANCO" para conta banco

    [Fact]
    public async Task ObterDetalheAsync_ContaCartao_ContaTipoCartao()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 3, NovoDebito(100m, categoria.Id, conta: _contaCartao));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        var unico = Assert.Single(resposta.Lancamentos);
        Assert.Equal("CARTAO", unico.ContaTipo);
        Assert.Equal(_contaCartao.Id, unico.ContaId);
        Assert.Equal("Nubank Cartao", unico.ContaNome);
    }

    [Fact]
    public async Task ObterDetalheAsync_ContaBanco_ContaTipoBanco()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 3, NovoDebito(100m, categoria.Id, conta: _contaBanco));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        var unico = Assert.Single(resposta.Lancamentos);
        Assert.Equal("BANCO", unico.ContaTipo);
    }

    #endregion

    #region Detalhe - Regra 12: Categoria valida sem gasto nao e erro (total 0, lista vazia)

    [Fact]
    public async Task ObterDetalheAsync_CategoriaSemGasto_RetornaTotalZeroEListaVazia()
    {
        // Arrange
        var categoriaSemGasto = Despesa("Lazer");
        var outra = Despesa("Mercado");
        ConfigurarCategorias(categoriaSemGasto, outra);
        ConfigurarLancamentos(2026, 3, NovoDebito(100m, outra.Id));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoriaSemGasto.Id);

        // Assert
        Assert.Equal(categoriaSemGasto.Id, resposta.CategoriaId);
        Assert.Equal(0m, resposta.Total);
        Assert.Equal(0m, resposta.PercentualDoTotal);
        Assert.Empty(resposta.Lancamentos);
    }

    #endregion

    #region Detalhe - Regra 13: Lancamentos em ordem crescente de data (desempate por Id)

    [Fact]
    public async Task ObterDetalheAsync_LancamentosEmOrdemCrescenteDeData()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        var tarde = NovoDebito(10m, categoria.Id, data: new DateOnly(2026, 3, 20));
        var cedo = NovoDebito(10m, categoria.Id, data: new DateOnly(2026, 3, 2));
        ConfigurarLancamentos(2026, 3, tarde, cedo);

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        Assert.Equal(new[] { cedo.Id, tarde.Id }, resposta.Lancamentos.Select(l => l.Id));
    }

    #endregion

    #region Helpers

    private void ConfigurarCategorias(params Categoria[] categorias)
    {
        var lista = categorias.ToList();

        _mockCategoriaRepository
            .Setup(r => r.Listar(It.IsAny<TipoCategoria?>(), It.IsAny<bool?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(lista);

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => lista.FirstOrDefault(c => c.Id == id));
    }

    private void ConfigurarLancamentos(int ano, int mes, params Lancamento[] lancamentos)
    {
        _mockLancamentoRepository
            .Setup(r => r.ListarDebitosParaRelatorioAsync(ano, mes))
            .ReturnsAsync(lancamentos.ToList());
    }

    private static Categoria Despesa(string nome, Guid? parentId = null, string? icone = null) => new()
    {
        Id = Guid.NewGuid(),
        Nome = nome,
        Tipo = TipoCategoria.Despesa,
        ParentId = parentId,
        Icone = icone
    };

    private static Categoria Receita(string nome) => new()
    {
        Id = Guid.NewGuid(),
        Nome = nome,
        Tipo = TipoCategoria.Receita
    };

    private Lancamento NovoDebito(
        decimal valor,
        Guid? categoriaId,
        Conta? conta = null,
        DateOnly? data = null)
    {
        var contaUsada = conta ?? _contaBanco;

        return new Lancamento
        {
            Id = Guid.NewGuid(),
            Valor = valor,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Oculto = false,
            CategoriaId = categoriaId,
            ContaId = contaUsada.Id,
            Conta = contaUsada,
            Data = data ?? new DateOnly(2026, 3, 10)
        };
    }

    #endregion
}
