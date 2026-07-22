using Moq;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class LimiteGastoServiceTests
{
    private readonly Mock<ILimiteGastoRepository> _mockLimiteGastoRepository;
    private readonly Mock<ICategoriaRepository> _mockCategoriaRepository;
    private readonly Mock<ILancamentoRepository> _mockLancamentoRepository;
    private readonly LimiteGastoService _service;

    public LimiteGastoServiceTests()
    {
        _mockLimiteGastoRepository = new Mock<ILimiteGastoRepository>();
        _mockCategoriaRepository = new Mock<ICategoriaRepository>();
        _mockLancamentoRepository = new Mock<ILancamentoRepository>();
        _service = new LimiteGastoService(
            _mockLimiteGastoRepository.Object,
            _mockCategoriaRepository.Object,
            _mockLancamentoRepository.Object);
    }

    #region Regra 1: Definir numa categoria com Tipo == Receita lanca CategoriaInvalidaParaLimiteGastoException

    [Fact]
    public async Task Definir_CategoriaReceitaExistente_LancaCategoriaInvalidaExcecao()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var valorLimite = 500m;

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Receita",
            Tipo = TipoCategoria.Receita,
            Arquivada = false
        };

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaInvalidaParaLimiteGastoException>(
            () => _service.Definir(categoriaId, valorLimite));

        Assert.Equal(categoriaId, excecao.CategoriaId);

        // Verifica que nada foi persistido
        _mockLimiteGastoRepository.Verify(r => r.Adicionar(It.IsAny<LimiteGasto>()), Times.Never);
        _mockLimiteGastoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 2: Definir numa categoria com Arquivada == true lanca CategoriaInvalidaParaLimiteGastoException

    [Fact]
    public async Task Definir_CategoriaArquivada_LancaCategoriaInvalidaExcecao()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var valorLimite = 500m;

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Categoria Arquivada",
            Tipo = TipoCategoria.Despesa,
            Arquivada = true
        };

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaInvalidaParaLimiteGastoException>(
            () => _service.Definir(categoriaId, valorLimite));

        Assert.Equal(categoriaId, excecao.CategoriaId);

        // Verifica que nada foi persistido
        _mockLimiteGastoRepository.Verify(r => r.Adicionar(It.IsAny<LimiteGasto>()), Times.Never);
        _mockLimiteGastoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 3: Definir com valorLimite <= 0 lanca ValorInvalidoException

    [Fact]
    public async Task Definir_ValorLimiteZero_LancaValorInvalidoExcecao()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var valorLimite = 0m;

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Despesa",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.Definir(categoriaId, valorLimite));

        Assert.Equal("valorLimite", excecao.NomeCampo);
        Assert.Equal(valorLimite, excecao.Valor);

        // Verifica que nada foi persistido
        _mockLimiteGastoRepository.Verify(r => r.Adicionar(It.IsAny<LimiteGasto>()), Times.Never);
    }

    [Fact]
    public async Task Definir_ValorLimiteNegativo_LancaValorInvalidoExcecao()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var valorLimite = -100m;

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Despesa",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.Definir(categoriaId, valorLimite));

        Assert.Equal("valorLimite", excecao.NomeCampo);
        Assert.Equal(valorLimite, excecao.Valor);

        // Verifica que nada foi persistido
        _mockLimiteGastoRepository.Verify(r => r.Adicionar(It.IsAny<LimiteGasto>()), Times.Never);
    }

    #endregion

    #region Regra 4: Definir numa categoria inexistente (ObterPorId retorna null) lanca CategoriaNaoEncontradaException

    [Fact]
    public async Task Definir_CategoriaNaoExistente_LancaCategoriaNaoEncontradaExcecao()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var valorLimite = 500m;

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync((Categoria?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaNaoEncontradaException>(
            () => _service.Definir(categoriaId, valorLimite));

        Assert.Equal(categoriaId, excecao.CategoriaId);

        // Verifica que nada foi persistido (validacao ocorreu ANTES de criar o limite)
        _mockLimiteGastoRepository.Verify(r => r.Adicionar(It.IsAny<LimiteGasto>()), Times.Never);
    }

    #endregion

    #region Regra 5: Definir quando JA existe LimiteGasto pra categoria ATUALIZA o ValorLimite (Salvar, NAO Adicionar)

    [Fact]
    public async Task Definir_LimiteJaExiste_AtualizaValoreLimiteEPersiste()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var novoValorLimite = 750m;

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Despesa Existente",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        var limiteExistente = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = categoriaId,
            ValorLimite = 500m,
            Periodo = PeriodoLimiteGasto.Mensal,
            Categoria = categoria
        };

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoriaId))
            .ReturnsAsync(limiteExistente);

        // Act
        var (resultado, criado) = await _service.Definir(categoriaId, novoValorLimite);

        // Assert
        Assert.False(criado);
        Assert.Equal(limiteExistente.Id, resultado.Id);
        Assert.Equal(categoriaId, resultado.CategoriaId);
        Assert.Equal(novoValorLimite, resultado.ValorLimite);

        // Verifica que Adicionar NAO foi chamado (porque ja existe)
        _mockLimiteGastoRepository.Verify(
            r => r.Adicionar(It.IsAny<LimiteGasto>()),
            Times.Never);

        // Verifica que Salvar foi chamado (para persistir a atualizacao)
        _mockLimiteGastoRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 6: Definir numa categoria valida e sem limite CRIA novo limite (Adicionar + Salvar)

    [Fact]
    public async Task Definir_NovoLimitePrimerioVez_CriaEPersiste()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var valorLimite = 500m;

        var categoria = new Categoria
        {
            Id = categoriaId,
            Nome = "Despesa",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(categoria);

        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoriaId))
            .ReturnsAsync((LimiteGasto?)null);

        // Act
        var (resultado, criado) = await _service.Definir(categoriaId, valorLimite);

        // Assert
        Assert.True(criado);
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(categoriaId, resultado.CategoriaId);
        Assert.Equal(valorLimite, resultado.ValorLimite);
        Assert.Equal(PeriodoLimiteGasto.Mensal, resultado.Periodo);

        // Verifica que Adicionar foi chamado uma unica vez
        _mockLimiteGastoRepository.Verify(
            r => r.Adicionar(It.Is<LimiteGasto>(l =>
                l.CategoriaId == categoriaId &&
                l.ValorLimite == valorLimite)),
            Times.Once);

        // Verifica que Salvar foi chamado
        _mockLimiteGastoRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 7: Remover numa categoria sem limite cadastrado lanca LimiteGastoNaoEncontradoException

    [Fact]
    public async Task Remover_SemLimiteExistente_LancaLimiteGastoNaoEncontradoExcecao()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();

        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoriaId))
            .ReturnsAsync((LimiteGasto?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<LimiteGastoNaoEncontradoException>(
            () => _service.Remover(categoriaId));

        Assert.Equal(categoriaId, excecao.CategoriaId);

        // Verifica que Remover NAO foi chamado
        _mockLimiteGastoRepository.Verify(r => r.Remover(It.IsAny<LimiteGasto>()), Times.Never);
        _mockLimiteGastoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 8: Remover um limite existente o deleta e persiste

    [Fact]
    public async Task Remover_LimiteExistente_RemoveEPersiste()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var limite = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = categoriaId,
            ValorLimite = 500m
        };

        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoriaId))
            .ReturnsAsync(limite);

        // Act
        await _service.Remover(categoriaId);

        // Assert
        _mockLimiteGastoRepository.Verify(
            r => r.Remover(It.Is<LimiteGasto>(l => l.Id == limite.Id)),
            Times.Once);

        _mockLimiteGastoRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 9: Listar retorna todos os limites

    [Fact]
    public async Task Listar_RetornaTodosOsLimites()
    {
        // Arrange
        var limites = new List<LimiteGasto>
        {
            new LimiteGasto { Id = Guid.NewGuid(), CategoriaId = Guid.NewGuid(), ValorLimite = 500m },
            new LimiteGasto { Id = Guid.NewGuid(), CategoriaId = Guid.NewGuid(), ValorLimite = 1000m },
            new LimiteGasto { Id = Guid.NewGuid(), CategoriaId = Guid.NewGuid(), ValorLimite = 750m }
        };

        _mockLimiteGastoRepository
            .Setup(r => r.Listar())
            .ReturnsAsync(limites);

        // Act
        var resultado = await _service.Listar();

        // Assert
        Assert.Equal(3, resultado.Count());
        _mockLimiteGastoRepository.Verify(r => r.Listar(), Times.Once);
    }

    [Fact]
    public async Task Listar_ListaVazia_Retorna0()
    {
        // Arrange
        var limites = new List<LimiteGasto>();

        _mockLimiteGastoRepository
            .Setup(r => r.Listar())
            .ReturnsAsync(limites);

        // Act
        var resultado = await _service.Listar();

        // Assert
        Assert.Empty(resultado);
    }

    #endregion

    #region Regra 10: ObterGastoVsLimiteTodasCategorias retorna resultado para cada LimiteGasto

    [Fact]
    public async Task ObterGastoVsLimiteTodasCategorias_RetornaStatusParaCadaLimite()
    {
        // Arrange
        var ano = 2026;
        var mes = 7;

        var categoria1 = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Alimentacao",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        var categoria2 = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Transporte",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        var limite1 = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = categoria1.Id,
            ValorLimite = 500m,
            Categoria = categoria1
        };

        var limite2 = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = categoria2.Id,
            ValorLimite = 300m,
            Categoria = categoria2
        };

        var lancamentos1 = new List<Lancamento>
        {
            new Lancamento { Tipo = TipoLancamento.Debit, Valor = 200m, Oculto = false }
        };

        var lancamentos2 = new List<Lancamento>
        {
            new Lancamento { Tipo = TipoLancamento.Debit, Valor = 250m, Oculto = false }
        };

        _mockLimiteGastoRepository
            .Setup(r => r.Listar())
            .ReturnsAsync(new List<LimiteGasto> { limite1, limite2 });

        // Mock ObterPorCategoriaId para retornar os limites corretos
        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoria1.Id))
            .ReturnsAsync(limite1);

        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoria2.Id))
            .ReturnsAsync(limite2);

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoria1.Id))
            .ReturnsAsync(categoria1);

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoria2.Id))
            .ReturnsAsync(categoria2);

        _mockLancamentoRepository
            .Setup(r => r.ListarPorCategoriasEPeriodo(It.IsAny<IEnumerable<Guid>>(), ano, mes))
            .ReturnsAsync((IEnumerable<Guid> ids, int a, int m) =>
                ids.Contains(categoria1.Id) ? lancamentos1 : lancamentos2);

        // Act
        var resultado = await _service.ObterGastoVsLimiteTodasCategorias(ano, mes);

        // Assert
        var resultadoList = resultado.ToList();
        Assert.Equal(2, resultadoList.Count);

        // Verifica resultado primeira categoria
        Assert.Equal(limite1.Id, resultadoList[0].LimiteGasto.Id);
        Assert.Equal(200m, resultadoList[0].Status.GastoRealizado);
        Assert.False(resultadoList[0].Status.Estourado);

        // Verifica resultado segunda categoria
        Assert.Equal(limite2.Id, resultadoList[1].LimiteGasto.Id);
        Assert.Equal(250m, resultadoList[1].Status.GastoRealizado);
        Assert.False(resultadoList[1].Status.Estourado);
    }

    #endregion

    #region Regra 11: Hierarquia - categoria-pai com subcategorias passa AMBOS IDs ao repository

    [Fact]
    public async Task ObterGastoVsLimite_CategoriaComSubcategorias_PassaBothIdsAoRepository()
    {
        // Arrange
        var ano = 2026;
        var mes = 7;

        var subcategoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Restaurantes",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false,
            ParentId = Guid.NewGuid(),
            Subcategorias = new List<Categoria>()
        };

        var categoriaPai = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Alimentacao",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false,
            Subcategorias = new List<Categoria> { subcategoria }
        };

        var limiteExistente = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = categoriaPai.Id,
            ValorLimite = 500m,
            Categoria = categoriaPai
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento { Tipo = TipoLancamento.Debit, Valor = 150m, Oculto = false },
            new Lancamento { Tipo = TipoLancamento.Debit, Valor = 100m, Oculto = false }
        };

        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoriaPai.Id))
            .ReturnsAsync(limiteExistente);

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaPai.Id))
            .ReturnsAsync(categoriaPai);

        _mockLancamentoRepository
            .Setup(r => r.ListarPorCategoriasEPeriodo(It.IsAny<IEnumerable<Guid>>(), ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var (limite, status) = await _service.ObterGastoVsLimite(categoriaPai.Id, ano, mes);

        // Assert
        // Verifica que ListarPorCategoriasEPeriodo foi chamado com UMA lista contendo AMBOS os IDs
        _mockLancamentoRepository.Verify(
            r => r.ListarPorCategoriasEPeriodo(
                It.Is<IEnumerable<Guid>>(ids =>
                    ids.Contains(categoriaPai.Id) &&
                    ids.Contains(subcategoria.Id) &&
                    ids.Count() == 2),
                ano,
                mes),
            Times.Once);

        // Verifica que o gasto foi calculado corretamente (soma dos dois lancamentos)
        Assert.Equal(250m, status.GastoRealizado);
        Assert.False(status.Estourado);
    }

    #endregion

    #region Regra 12: Categoria SEM subcategorias passa APENAS o seu ID ao repository

    [Fact]
    public async Task ObterGastoVsLimite_CategoriaSemSubcategorias_PassaApenasOProprioIdAoRepository()
    {
        // Arrange
        var ano = 2026;
        var mes = 7;

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Alimentacao",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false,
            Subcategorias = new List<Categoria>()
        };

        var limiteExistente = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = categoria.Id,
            ValorLimite = 500m,
            Categoria = categoria
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento { Tipo = TipoLancamento.Debit, Valor = 200m, Oculto = false }
        };

        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoria.Id))
            .ReturnsAsync(limiteExistente);

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoria.Id))
            .ReturnsAsync(categoria);

        _mockLancamentoRepository
            .Setup(r => r.ListarPorCategoriasEPeriodo(It.IsAny<IEnumerable<Guid>>(), ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var (limite, status) = await _service.ObterGastoVsLimite(categoria.Id, ano, mes);

        // Assert
        // Verifica que ListarPorCategoriasEPeriodo foi chamado com APENAS um ID
        _mockLancamentoRepository.Verify(
            r => r.ListarPorCategoriasEPeriodo(
                It.Is<IEnumerable<Guid>>(ids =>
                    ids.Count() == 1 &&
                    ids.First() == categoria.Id),
                ano,
                mes),
            Times.Once);

        Assert.Equal(200m, status.GastoRealizado);
        Assert.False(status.Estourado);
    }

    #endregion

    #region Regra 13: ObterGastoVsLimite com categoria inexistente lanca CategoriaNaoEncontradaException

    [Fact]
    public async Task ObterGastoVsLimite_CategoriaNaoExistente_LancaCategoriaNaoEncontradaExcecao()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var ano = 2026;
        var mes = 7;

        var limite = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = categoriaId,
            ValorLimite = 500m
        };

        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoriaId))
            .ReturnsAsync(limite);

        _mockCategoriaRepository
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync((Categoria?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaNaoEncontradaException>(
            () => _service.ObterGastoVsLimite(categoriaId, ano, mes));

        Assert.Equal(categoriaId, excecao.CategoriaId);
    }

    #endregion

    #region Regra 14: ObterGastoVsLimite com limite inexistente lanca LimiteGastoNaoEncontradoException

    [Fact]
    public async Task ObterGastoVsLimite_LimiteNaoExistente_LancaLimiteGastoNaoEncontradoExcecao()
    {
        // Arrange
        var categoriaId = Guid.NewGuid();
        var ano = 2026;
        var mes = 7;

        _mockLimiteGastoRepository
            .Setup(r => r.ObterPorCategoriaId(categoriaId))
            .ReturnsAsync((LimiteGasto?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<LimiteGastoNaoEncontradoException>(
            () => _service.ObterGastoVsLimite(categoriaId, ano, mes));

        Assert.Equal(categoriaId, excecao.CategoriaId);
    }

    #endregion
}
