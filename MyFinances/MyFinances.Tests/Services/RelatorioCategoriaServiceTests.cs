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
    private readonly Mock<IPlanejamentoMensalService> _mockPlanejamentoMensalService;
    private readonly RelatorioCategoriaService _service;

    private readonly Conta _contaBanco = new() { Id = Guid.NewGuid(), Nome = "Nubank Conta", Tipo = TipoConta.Banco };
    private readonly Conta _contaCartao = new() { Id = Guid.NewGuid(), Nome = "Nubank Cartao", Tipo = TipoConta.Cartao };

    public RelatorioCategoriaServiceTests()
    {
        _mockLancamentoRepository = new Mock<ILancamentoRepository>();
        _mockCategoriaRepository = new Mock<ICategoriaRepository>();
        _mockPlanejamentoMensalService = new Mock<IPlanejamentoMensalService>();
        _service = new RelatorioCategoriaService(
            _mockLancamentoRepository.Object,
            _mockCategoriaRepository.Object,
            _mockPlanejamentoMensalService.Object);

        ConfigurarCategorias();
        ConfigurarPlanoAusente();
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

    [Fact]
    public async Task ObterResumoAsync_AnoAcimaDe9999_LancaValorInvalidoExcecao()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(() => _service.ObterResumoAsync(10000, 3));
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

    [Fact]
    public async Task ObterDetalheAsync_AnoAcimaDe9999_LancaValorInvalidoExcecao()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.ObterDetalheAsync(10000, 3, Guid.NewGuid()));
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

        _mockPlanejamentoMensalService.Verify(s => s.ObterAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region Detalhe - Regra 8: Limite vem do sublimite do plano; GastoRealizado e o gasto do sublimite (itens 14 e 19)

    [Fact]
    public async Task ObterDetalheAsync_ComSublimite_GastoRealizadoMenorQueTotal_LimiteUsaGastoDoSublimite()
    {
        // Arrange
        var categoria = Despesa("Mercado", icone: "cart");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 3, NovoDebito(120m, categoria.Id), NovoDebito(80m, categoria.Id));
        ConfigurarPlano(2026, 3, Sublimite(categoria.Id, valor: 500m, gastoRealizado: 150m));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        Assert.Equal(categoria.Id, resposta.CategoriaId);
        Assert.Equal("Mercado", resposta.Nome);
        Assert.Equal("cart", resposta.Icone);
        Assert.Equal(200m, resposta.Total);
        Assert.NotNull(resposta.Limite);
        Assert.Equal(500m, resposta.Limite!.ValorLimite);
        Assert.Equal(150m, resposta.Limite.GastoRealizado);
    }

    [Fact]
    public async Task ObterDetalheAsync_GastoAcimaDoSublimite_LimiteEstourado()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 3, NovoDebito(200m, categoria.Id));
        ConfigurarPlano(2026, 3, Sublimite(categoria.Id, valor: 100m, gastoRealizado: 150m));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        Assert.True(resposta.Limite!.Estourado);
    }

    [Fact]
    public async Task ObterDetalheAsync_GastoDentroDoSublimite_LimiteNaoEstourado()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 3, NovoDebito(120m, categoria.Id));
        ConfigurarPlano(2026, 3, Sublimite(categoria.Id, valor: 500m, gastoRealizado: 150m));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        Assert.False(resposta.Limite!.Estourado);
    }

    [Fact]
    public async Task ObterDetalheAsync_SublimiteZeroComGasto_LimiteEstourado()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 3, NovoDebito(50m, categoria.Id));
        ConfigurarPlano(2026, 3, Sublimite(categoria.Id, valor: 0m, gastoRealizado: 50m));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        Assert.True(resposta.Limite!.Estourado);
    }

    [Fact]
    public async Task ObterDetalheAsync_MesAlemDoHorizonte_LimiteNuloSemFalhar()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        var lancamento = NovoDebito(120m, categoria.Id);
        ConfigurarLancamentos(2040, 1, lancamento);
        _mockPlanejamentoMensalService
            .Setup(s => s.ObterAsync(2040, 1))
            .ThrowsAsync(new PlanoMensalForaDoHorizonteException(2040, 1, 12));

        // Act
        var resposta = await _service.ObterDetalheAsync(2040, 1, categoria.Id);

        // Assert
        Assert.Null(resposta.Limite);
        Assert.Equal(120m, resposta.Total);
        Assert.Equal(categoria.Id, resposta.CategoriaId);
        var unico = Assert.Single(resposta.Lancamentos);
        Assert.Equal(lancamento.Id, unico.Id);
    }

    [Fact]
    public async Task ObterDetalheAsync_MesSemPlano_LimiteNulo()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        ConfigurarCategorias(categoria);
        ConfigurarLancamentos(2026, 3, NovoDebito(120m, categoria.Id));
        ConfigurarPlanoAusente();

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, categoria.Id);

        // Assert
        Assert.Equal(120m, resposta.Total);
        Assert.Null(resposta.Limite);
    }

    [Fact]
    public async Task ObterDetalheAsync_PlanoSemSublimiteDaCategoria_LimiteNulo()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var outra = Despesa("Lazer");
        ConfigurarCategorias(categoria, outra);
        ConfigurarLancamentos(2026, 3, NovoDebito(120m, categoria.Id));
        ConfigurarPlano(2026, 3, Sublimite(outra.Id, valor: 300m, gastoRealizado: 40m));

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

    #region Regra 14: categoria arquivada continua entrando no gasto do mes (item 19)

    [Fact]
    public async Task ObterResumoAsync_CategoriaArquivada_GastoEntraNoTotalELinha()
    {
        // Arrange
        var arquivada = DespesaArquivada("Lazer");
        var ativa = Despesa("Mercado");
        ConfigurarCategorias(arquivada, ativa);
        ConfigurarLancamentos(2026, 3, NovoDebito(80m, arquivada.Id), NovoDebito(20m, ativa.Id));

        // Act
        var resposta = await _service.ObterResumoAsync(2026, 3);

        // Assert
        Assert.Equal(100m, resposta.TotalGasto);
        Assert.Equal(2, resposta.Linhas.Count);

        var linhaArquivada = resposta.Linhas.Single(linha => linha.CategoriaId == arquivada.Id);
        Assert.Equal("Lazer", linhaArquivada.Nome);
        Assert.Equal(80m, linhaArquivada.Valor);
    }

    [Fact]
    public async Task ObterDetalheAsync_CategoriaArquivada_RetornaTotalELancamentos()
    {
        // Arrange
        var arquivada = DespesaArquivada("Lazer");
        var ativa = Despesa("Mercado");
        ConfigurarCategorias(arquivada, ativa);
        var lancamento = NovoDebito(80m, arquivada.Id);
        ConfigurarLancamentos(2026, 3, lancamento, NovoDebito(20m, ativa.Id));

        // Act
        var resposta = await _service.ObterDetalheAsync(2026, 3, arquivada.Id);

        // Assert
        Assert.Equal(arquivada.Id, resposta.CategoriaId);
        Assert.Equal("Lazer", resposta.Nome);
        Assert.Equal(80m, resposta.Total);
        Assert.Equal(80.00m, resposta.PercentualDoTotal);

        var unico = Assert.Single(resposta.Lancamentos);
        Assert.Equal(lancamento.Id, unico.Id);
    }

    #endregion

    #region Helpers

    private void ConfigurarCategorias(params Categoria[] categorias)
    {
        var lista = categorias.ToList();

        _mockCategoriaRepository
            .Setup(r => r.ListarTodas())
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

    private void ConfigurarPlano(int ano, int mes, params SublimiteCalculado[] sublimites)
    {
        var plano = new PlanoMensalCalculado(
            ano,
            mes,
            Renda: 0m,
            Fixas: 0m,
            PercentualInvestimento: 0m,
            InvestimentoPlanejado: 0m,
            Investimento: 0m,
            InvestimentoReduzido: false,
            PlanoNaoFecha: false,
            Sobra: 0m,
            NaoAlocado: 0m,
            GastoSemSublimite: 0m,
            Sublimites: sublimites);

        _mockPlanejamentoMensalService
            .Setup(s => s.ObterAsync(ano, mes))
            .ReturnsAsync(new PlanoMensalConsulta(plano, SomenteLeitura: false));
    }

    private void ConfigurarPlanoAusente()
    {
        _mockPlanejamentoMensalService
            .Setup(s => s.ObterAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((PlanoMensalConsulta?)null);
    }

    private static SublimiteCalculado Sublimite(Guid categoriaId, decimal valor, decimal gastoRealizado) => new(
        categoriaId,
        ParentId: null,
        Nome: "Sublimite",
        Icone: null,
        CategoriaArquivada: false,
        Percentual: 0m,
        Valor: valor,
        GastoRealizado: gastoRealizado,
        FracaoUtilizada: valor == 0m ? 0m : gastoRealizado / valor,
        Estourado: gastoRealizado > valor);

    private static Categoria Despesa(string nome, Guid? parentId = null, string? icone = null) => new()
    {
        Id = Guid.NewGuid(),
        Nome = nome,
        Tipo = TipoCategoria.Despesa,
        ParentId = parentId,
        Icone = icone
    };

    private static Categoria DespesaArquivada(string nome) => new()
    {
        Id = Guid.NewGuid(),
        Nome = nome,
        Tipo = TipoCategoria.Despesa,
        Arquivada = true
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
