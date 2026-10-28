using Moq;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class PlanejamentoMensalServiceTests
{
    // Hoje fixo em 2026-10-14: mes corrente = outubro, mes passado = setembro, mes futuro = novembro.
    private static readonly DateTimeOffset HojeFixo = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IPlanoMensalRepository> _mockPlanoMensalRepository;
    private readonly Mock<ICategoriaRepository> _mockCategoriaRepository;
    private readonly Mock<ILancamentoRepository> _mockLancamentoRepository;
    private readonly Mock<IRecebivelRecorrenteRepository> _mockRecebivelRecorrenteRepository;
    private readonly PlanejamentoMensalService _service;

    public PlanejamentoMensalServiceTests()
    {
        _mockPlanoMensalRepository = new Mock<IPlanoMensalRepository>();
        _mockCategoriaRepository = new Mock<ICategoriaRepository>();
        _mockLancamentoRepository = new Mock<ILancamentoRepository>();
        _mockRecebivelRecorrenteRepository = new Mock<IRecebivelRecorrenteRepository>();

        _mockCategoriaRepository
            .Setup(r => r.ListarTodas())
            .ReturnsAsync(new List<Categoria>());

        _mockLancamentoRepository
            .Setup(r => r.ListarDebitosParaRelatorioAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Lancamento>());

        _mockRecebivelRecorrenteRepository
            .Setup(r => r.ListarAtivos())
            .ReturnsAsync(new List<RecebivelRecorrente>());

        _mockPlanoMensalRepository
            .Setup(r => r.ObterMaisRecenteAnteriorA(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((PlanoMensal?)null);

        _mockPlanoMensalRepository
            .Setup(r => r.AdicionarOuObterExistente(It.IsAny<PlanoMensal>()))
            .ReturnsAsync((PlanoMensal plano) => plano);

        _service = CriarServico(HojeFixo);
    }

    #region Obter: geracao sob demanda (mes corrente ou futuro)

    [Theory]
    [InlineData(2026, 10)]
    [InlineData(2026, 11)]
    public async Task Obter_MesCorrenteOuFuturoSemPlano_GeraPlanoUmaVezComRendaDosMoldesAtivos(int ano, int mes)
    {
        // Arrange
        ConfigurarMoldesAtivos(CriarMolde(1000m));
        _mockPlanoMensalRepository
            .Setup(r => r.ObterPorAnoMes(ano, mes))
            .ReturnsAsync((PlanoMensal?)null);

        // Act
        var resultado = await _service.ObterAsync(ano, mes);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(ano, resultado.Plano.Ano);
        Assert.Equal(mes, resultado.Plano.Mes);
        Assert.False(resultado.SomenteLeitura);

        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.Is<PlanoMensal>(p =>
                p.Ano == ano &&
                p.Mes == mes &&
                p.RendaPrevista == 1000m)),
            Times.Once);
    }

    [Fact]
    public async Task Obter_MesSemPlanoComPlanoAnteriorRecente_CopiaPercentualEBuscaSublimitesNaoArquivados()
    {
        // Arrange
        var alimentacao = CriarCategoria("Alimentacao");
        var antiga = CriarCategoria("Antiga", arquivada: true);
        ConfigurarCategorias(alimentacao, antiga);
        ConfigurarMoldesAtivos(CriarMolde(1000m));

        var anterior = CriarPlano(2026, 9, 3000m, 15m,
            CriarSublimite(alimentacao, 20m),
            CriarSublimite(antiga, 10m));
        ConfigurarSemPlano(2026, 10);
        _mockPlanoMensalRepository
            .Setup(r => r.ObterMaisRecenteAnteriorA(2026, 10))
            .ReturnsAsync(anterior);

        // Act
        await _service.ObterAsync(2026, 10);

        // Assert
        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.Is<PlanoMensal>(p =>
                p.PercentualInvestimento == 15m &&
                p.Sublimites.Count == 1 &&
                p.Sublimites.Any(s => s.CategoriaId == alimentacao.Id && s.Percentual == 20m) &&
                p.Sublimites.All(s => s.CategoriaId != antiga.Id))),
            Times.Once);
    }

    [Fact]
    public async Task Obter_MesSemPlanoComPlanoAnterior_NaoCopiaRendaPrevistaDoAnterior()
    {
        // Arrange
        ConfigurarMoldesAtivos(CriarMolde(1000m));
        var anterior = CriarPlano(2026, 9, 9999m, 10m);
        ConfigurarSemPlano(2026, 10);
        _mockPlanoMensalRepository
            .Setup(r => r.ObterMaisRecenteAnteriorA(2026, 10))
            .ReturnsAsync(anterior);

        // Act
        await _service.ObterAsync(2026, 10);

        // Assert
        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.Is<PlanoMensal>(p => p.RendaPrevista == 1000m)),
            Times.Once);
    }

    [Fact]
    public async Task Obter_MesSemPlanoESemPlanoAnterior_NasceComInvestimentoZeroESemSublimites()
    {
        // Arrange
        ConfigurarMoldesAtivos(CriarMolde(1000m));
        ConfigurarSemPlano(2026, 10);

        // Act
        await _service.ObterAsync(2026, 10);

        // Assert
        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.Is<PlanoMensal>(p =>
                p.RendaPrevista == 1000m &&
                p.PercentualInvestimento == 0m &&
                p.Sublimites.Count == 0)),
            Times.Once);
    }

    [Fact]
    public async Task Obter_PlanoJaExistente_NaoRegeraNemPersiste()
    {
        // Arrange
        var existente = CriarPlano(2026, 11, 2500m, 10m);
        ConfigurarPlanoExistente(existente);

        // Act
        var resultado = await _service.ObterAsync(2026, 11);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(2026, resultado.Plano.Ano);
        Assert.Equal(11, resultado.Plano.Mes);

        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.IsAny<PlanoMensal>()),
            Times.Never);
        _mockPlanoMensalRepository.Verify(
            r => r.ObterMaisRecenteAnteriorA(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
        _mockRecebivelRecorrenteRepository.Verify(r => r.ListarAtivos(), Times.Never);
    }

    #endregion

    #region Obter: ciclo do mes (somente leitura e mes passado)

    [Fact]
    public async Task Obter_MesPassadoSemPlano_RetornaNullENaoPersiste()
    {
        // Arrange
        ConfigurarSemPlano(2026, 9);

        // Act
        var resultado = await _service.ObterAsync(2026, 9);

        // Assert
        Assert.Null(resultado);
        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.IsAny<PlanoMensal>()),
            Times.Never);
    }

    [Fact]
    public async Task Obter_MesPassadoComPlano_RetornaSomenteLeitura()
    {
        // Arrange
        ConfigurarPlanoExistente(CriarPlano(2026, 9, 1000m, 10m));

        // Act
        var resultado = await _service.ObterAsync(2026, 9);

        // Assert
        Assert.NotNull(resultado);
        Assert.True(resultado.SomenteLeitura);
        Assert.Equal(9, resultado.Plano.Mes);
    }

    [Fact]
    public async Task Obter_MesCorrenteComPlano_NaoEhSomenteLeitura()
    {
        // Arrange
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act
        var resultado = await _service.ObterAsync(2026, 10);

        // Assert
        Assert.NotNull(resultado);
        Assert.False(resultado.SomenteLeitura);
    }

    [Fact]
    public async Task Obter_PrimeiroDiaDoMesSeguinte_MesAnteriorViraSomenteLeitura()
    {
        // Arrange: hoje e 2026-11-01, entao outubro ja e mes passado
        var servico = CriarServico(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act
        var resultado = await servico.ObterAsync(2026, 10);

        // Assert
        Assert.NotNull(resultado);
        Assert.True(resultado.SomenteLeitura);
    }

    [Fact]
    public async Task Obter_UltimoDiaDoMes_MesCorrenteContinuaEditavel()
    {
        // Arrange: hoje e 2026-10-31 (23:59 UTC), outubro ainda e mes corrente
        var servico = CriarServico(new DateTimeOffset(2026, 10, 31, 23, 59, 0, TimeSpan.Zero));
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act
        var resultado = await servico.ObterAsync(2026, 10);

        // Assert
        Assert.NotNull(resultado);
        Assert.False(resultado.SomenteLeitura);
    }

    #endregion

    #region Obter: validacao de ano e mes

    [Theory]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    [InlineData(0, 10)]
    [InlineData(10000, 10)]
    public async Task Obter_AnoOuMesInvalido_LancaValorInvalidoSemConsultarPlano(int ano, int mes)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(() => _service.ObterAsync(ano, mes));

        _mockPlanoMensalRepository.Verify(
            r => r.ObterPorAnoMes(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    #endregion

    #region Salvar: mes passado e validacao de ano/mes

    [Fact]
    public async Task Salvar_MesPassadoComDadosInvalidos_LancaSomenteLeituraAntesDeValidar()
    {
        // Arrange: renda negativa seria invalida, mas a trava de mes passado vem antes
        var passado = CriarPlano(2026, 9, 1000m, 10m);
        ConfigurarPlanoExistente(passado);

        // Act & Assert
        await Assert.ThrowsAsync<PlanoMensalPassadoSomenteLeituraException>(
            () => _service.SalvarAsync(2026, 9, -1m, 150m, new List<SublimiteEntrada>()));

        Assert.Equal(1000m, passado.RendaPrevista);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_MesPassadoComDadosValidos_LancaSomenteLeitura()
    {
        // Arrange
        ConfigurarPlanoExistente(CriarPlano(2026, 9, 1000m, 10m));

        // Act & Assert
        await Assert.ThrowsAsync<PlanoMensalPassadoSomenteLeituraException>(
            () => _service.SalvarAsync(2026, 9, 2000m, 10m, new List<SublimiteEntrada>()));

        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_MesInvalido_LancaValorInvalidoSemConsultarPlano()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.SalvarAsync(2026, 13, 1000m, 10m, new List<SublimiteEntrada>()));

        _mockPlanoMensalRepository.Verify(
            r => r.ObterPorAnoMes(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Salvar: violacoes de validacao (nada e salvo)

    [Fact]
    public async Task Salvar_RendaNegativa_LancaRendaPrevistaNegativaENaoSalva()
    {
        // Arrange
        var plano = CriarPlano(2026, 10, 1000m, 10m);
        ConfigurarPlanoExistente(plano);

        // Act & Assert
        await Assert.ThrowsAsync<RendaPrevistaNegativaException>(
            () => _service.SalvarAsync(2026, 10, -0.01m, 10m, new List<SublimiteEntrada>()));

        Assert.Equal(1000m, plano.RendaPrevista);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_PercentualInvestimentoAbaixoDeZero_LancaValorInvalidoENaoSalva()
    {
        // Arrange
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.SalvarAsync(2026, 10, 1000m, -0.01m, new List<SublimiteEntrada>()));

        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_PercentualInvestimentoAcimaDeCem_LancaValorInvalidoENaoSalva()
    {
        // Arrange
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.SalvarAsync(2026, 10, 1000m, 100.01m, new List<SublimiteEntrada>()));

        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_PercentualDeSublimiteAcimaDeCem_LancaValorInvalidoENaoSalva()
    {
        // Arrange
        var alimentacao = CriarCategoria("Alimentacao");
        ConfigurarCategorias(alimentacao);
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.SalvarAsync(2026, 10, 1000m, 10m, new List<SublimiteEntrada>
            {
                new(alimentacao.Id, 101m)
            }));

        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_SublimiteEmCategoriaReceita_LancaCategoriaInvalidaENaoSalva()
    {
        // Arrange
        var salario = CriarCategoria("Salario", TipoCategoria.Receita);
        ConfigurarCategorias(salario);
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaInvalidaParaSublimiteException>(
            () => _service.SalvarAsync(2026, 10, 1000m, 10m, new List<SublimiteEntrada>
            {
                new(salario.Id, 10m)
            }));

        Assert.Equal(salario.Id, excecao.CategoriaId);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_CategoriaArquivadaQueJaTinhaSublimite_ReenviadaLancaCategoriaInvalidaENaoSalva()
    {
        // Arrange: decisao do usuario em 2026-10-14, categoria arquivada nao volta a receber sublimite
        var arquivada = CriarCategoria("Antiga", arquivada: true);
        ConfigurarCategorias(arquivada);
        var plano = CriarPlano(2026, 10, 1000m, 10m, CriarSublimite(arquivada, 10m));
        ConfigurarPlanoExistente(plano);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CategoriaInvalidaParaSublimiteException>(
            () => _service.SalvarAsync(2026, 10, 1000m, 10m, new List<SublimiteEntrada>
            {
                new(arquivada.Id, 10m)
            }));

        Assert.Equal(arquivada.Id, excecao.CategoriaId);
        _mockPlanoMensalRepository.Verify(r => r.RemoverSublimite(It.IsAny<PlanoMensalSublimite>()), Times.Never);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_MesmaCategoriaDuasVezes_LancaSublimiteDuplicadoENaoSalva()
    {
        // Arrange
        var alimentacao = CriarCategoria("Alimentacao");
        ConfigurarCategorias(alimentacao);
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act & Assert
        await Assert.ThrowsAsync<SublimiteDuplicadoException>(
            () => _service.SalvarAsync(2026, 10, 1000m, 10m, new List<SublimiteEntrada>
            {
                new(alimentacao.Id, 10m),
                new(alimentacao.Id, 20m)
            }));

        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_SomaDoPrimeiroNivelAcimaDeCem_LancaSomaExcedeENaoSalva()
    {
        // Arrange
        var alimentacao = CriarCategoria("Alimentacao");
        var transporte = CriarCategoria("Transporte");
        ConfigurarCategorias(alimentacao, transporte);
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<SomaPercentuaisExcedeCemException>(
            () => _service.SalvarAsync(2026, 10, 1000m, 10m, new List<SublimiteEntrada>
            {
                new(alimentacao.Id, 60m),
                new(transporte.Id, 50m)
            }));

        Assert.Null(excecao.CategoriaPaiId);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_SomaDoPrimeiroNivelExatamenteCem_EValidaESalva()
    {
        // Arrange
        var alimentacao = CriarCategoria("Alimentacao");
        var transporte = CriarCategoria("Transporte");
        ConfigurarCategorias(alimentacao, transporte);
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act
        await _service.SalvarAsync(2026, 10, 1000m, 10m, new List<SublimiteEntrada>
        {
            new(alimentacao.Id, 60m),
            new(transporte.Id, 40m)
        });

        // Assert
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task Salvar_SubcategoriaSemPaiNoConjunto_LancaSubcategoriaSemSublimiteNoPaiENaoRemoveNada()
    {
        // Arrange: plano tinha pai e filho; o usuario remove o pai e mantem o filho
        var pai = CriarCategoria("Alimentacao");
        var filho = CriarCategoria("Restaurantes", pai: pai);
        ConfigurarCategorias(pai, filho);
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m,
            CriarSublimite(pai, 30m),
            CriarSublimite(filho, 10m)));

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<SubcategoriaSemSublimiteNoPaiException>(
            () => _service.SalvarAsync(2026, 10, 1000m, 10m, new List<SublimiteEntrada>
            {
                new(filho.Id, 10m)
            }));

        Assert.Equal(filho.Id, excecao.SubcategoriaId);
        Assert.Equal(pai.Id, excecao.PaiId);
        _mockPlanoMensalRepository.Verify(r => r.RemoverSublimite(It.IsAny<PlanoMensalSublimite>()), Times.Never);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_SubcategoriaComPaiNoConjunto_EValidaESalva()
    {
        // Arrange
        var pai = CriarCategoria("Alimentacao");
        var filho = CriarCategoria("Restaurantes", pai: pai);
        ConfigurarCategorias(pai, filho);
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act
        await _service.SalvarAsync(2026, 10, 1000m, 10m, new List<SublimiteEntrada>
        {
            new(pai.Id, 30m),
            new(filho.Id, 10m)
        });

        // Assert
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task Salvar_SomaDasSubcategoriasDoMesmoPaiAcimaDeCem_LancaSomaExcedeComIdDoPai()
    {
        // Arrange
        var pai = CriarCategoria("Alimentacao");
        var filho1 = CriarCategoria("Restaurantes", pai: pai);
        var filho2 = CriarCategoria("Mercado", pai: pai);
        ConfigurarCategorias(pai, filho1, filho2);
        ConfigurarPlanoExistente(CriarPlano(2026, 10, 1000m, 10m));

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<SomaPercentuaisExcedeCemException>(
            () => _service.SalvarAsync(2026, 10, 1000m, 10m, new List<SublimiteEntrada>
            {
                new(pai.Id, 100m),
                new(filho1.Id, 60m),
                new(filho2.Id, 50m)
            }));

        Assert.Equal(pai.Id, excecao.CategoriaPaiId);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Salvar: plano valido (diff in-place)

    [Fact]
    public async Task Salvar_PlanoValido_AtualizaExistenteRemoveAusenteEAdicionaNovo()
    {
        // Arrange
        var alimentacao = CriarCategoria("Alimentacao");
        var transporte = CriarCategoria("Transporte");
        var lazer = CriarCategoria("Lazer");
        ConfigurarCategorias(alimentacao, transporte, lazer);

        var sublimiteAlimentacao = CriarSublimite(alimentacao, 10m);
        var plano = CriarPlano(2026, 10, 1000m, 10m,
            sublimiteAlimentacao,
            CriarSublimite(transporte, 20m));
        ConfigurarPlanoExistente(plano);

        // Act
        var resultado = await _service.SalvarAsync(2026, 10, 2000m, 15m, new List<SublimiteEntrada>
        {
            new(alimentacao.Id, 30m),
            new(lazer.Id, 5m)
        });

        // Assert
        Assert.NotNull(resultado);
        Assert.False(resultado.SomenteLeitura);

        Assert.Equal(2000m, plano.RendaPrevista);
        Assert.Equal(15m, plano.PercentualInvestimento);

        // Existente: mesma instancia, percentual atualizado
        Assert.Same(sublimiteAlimentacao, plano.Sublimites.Single(s => s.CategoriaId == alimentacao.Id));
        Assert.Equal(30m, sublimiteAlimentacao.Percentual);

        // Novo: adicionado ao plano
        Assert.Contains(plano.Sublimites, s => s.CategoriaId == lazer.Id && s.Percentual == 5m);

        // Ausente: removido via repositorio
        _mockPlanoMensalRepository.Verify(
            r => r.RemoverSublimite(It.Is<PlanoMensalSublimite>(s => s.CategoriaId == transporte.Id)),
            Times.Once);
        _mockPlanoMensalRepository.Verify(
            r => r.RemoverSublimite(It.Is<PlanoMensalSublimite>(s => s.CategoriaId == alimentacao.Id)),
            Times.Never);

        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Once);
        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.IsAny<PlanoMensal>()),
            Times.Never);
    }

    #endregion

    #region Teto tecnico do ano (1..9999, limite do DateOnly) e horizonte (mes corrente + 12)

    [Theory]
    [InlineData(10000, 10)]
    [InlineData(10000, 11)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task Salvar_AnoOuMesInvalidoNoTeto_LancaValorInvalidoSemAcessarRepositorio(int ano, int mes)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.SalvarAsync(ano, mes, 1000m, 10m, new List<SublimiteEntrada>()));

        _mockPlanoMensalRepository.Verify(
            r => r.ObterPorAnoMes(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.IsAny<PlanoMensal>()),
            Times.Never);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Obter_Ano10000_LancaValorInvalidoSemAcessarRepositorio()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(() => _service.ObterAsync(10000, 10));

        _mockPlanoMensalRepository.Verify(
            r => r.ObterPorAnoMes(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.IsAny<PlanoMensal>()),
            Times.Never);
    }

    // Horizonte (decisao de 2026-10-14): ate mes corrente + 12. Hoje 2026-10-14 => limite 2027-10.
    // 9999-11 cai fora do horizonte: continua 400, agora pelo horizonte (nao mais pelo teto do ano).
    [Theory]
    [InlineData(2027, 11)]
    [InlineData(9999, 11)]
    public async Task Obter_MesAlemDoHorizonte_LancaForaDoHorizonteSemGravar(int ano, int mes)
    {
        await Assert.ThrowsAsync<PlanoMensalForaDoHorizonteException>(() => _service.ObterAsync(ano, mes));

        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.IsAny<PlanoMensal>()),
            Times.Never);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Theory]
    [InlineData(2027, 11)]
    [InlineData(9999, 11)]
    public async Task Salvar_MesAlemDoHorizonte_LancaForaDoHorizonteSemGravar(int ano, int mes)
    {
        await Assert.ThrowsAsync<PlanoMensalForaDoHorizonteException>(
            () => _service.SalvarAsync(ano, mes, 1000m, 10m, new List<SublimiteEntrada>()));

        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.IsAny<PlanoMensal>()),
            Times.Never);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Obter_MesCorrentePlus12_EAceitoEGera()
    {
        // 2027-10 = mes corrente (2026-10) + 12: limite inclusive do horizonte
        ConfigurarSemPlano(2027, 10);

        var resultado = await _service.ObterAsync(2027, 10);

        Assert.NotNull(resultado);
        Assert.Equal(2027, resultado.Plano.Ano);
        Assert.Equal(10, resultado.Plano.Mes);
        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.Is<PlanoMensal>(p => p.Ano == 2027 && p.Mes == 10)),
            Times.Once);
    }

    [Fact]
    public async Task Salvar_MesCorrentePlus12_EAceitoECriaPlano()
    {
        ConfigurarSemPlano(2027, 10);

        var resultado = await _service.SalvarAsync(2027, 10, 1000m, 10m, new List<SublimiteEntrada>());

        Assert.NotNull(resultado);
        Assert.Equal(2027, resultado.Plano.Ano);
        Assert.Equal(10, resultado.Plano.Mes);
        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.Is<PlanoMensal>(p => p.Ano == 2027 && p.Mes == 10)),
            Times.Once);
    }

    #endregion

    #region Salvar: mes sem plano gravado (decisao de 2026-10-14)

    [Fact]
    public async Task Salvar_MesCorrenteSemPlano_CriaPlanoComValoresEnviados()
    {
        // Arrange: sem plano gravado, os valores vem do request; nada de copia nem de moldes
        var alimentacao = CriarCategoria("Alimentacao");
        ConfigurarCategorias(alimentacao);
        ConfigurarMoldesAtivos(CriarMolde(1000m));
        ConfigurarSemPlano(2026, 10);

        // Act
        var resultado = await _service.SalvarAsync(2026, 10, 2500m, 12m, new List<SublimiteEntrada>
        {
            new(alimentacao.Id, 40m)
        });

        // Assert
        Assert.NotNull(resultado);
        Assert.False(resultado.SomenteLeitura);

        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.Is<PlanoMensal>(p =>
                p.Ano == 2026 &&
                p.Mes == 10 &&
                p.RendaPrevista == 2500m &&
                p.PercentualInvestimento == 12m &&
                p.Sublimites.Count == 1 &&
                p.Sublimites.Any(s => s.CategoriaId == alimentacao.Id && s.Percentual == 40m) &&
                p.Sublimites.All(s => s.Id != Guid.Empty))),
            Times.Once);

        _mockPlanoMensalRepository.Verify(
            r => r.ObterMaisRecenteAnteriorA(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
        _mockRecebivelRecorrenteRepository.Verify(r => r.ListarAtivos(), Times.Never);
    }

    [Fact]
    public async Task Salvar_MesFuturoSemPlanoComDadosInvalidos_NaoCriaNada()
    {
        // Arrange
        ConfigurarSemPlano(2026, 11);

        // Act & Assert
        await Assert.ThrowsAsync<RendaPrevistaNegativaException>(
            () => _service.SalvarAsync(2026, 11, -1m, 10m, new List<SublimiteEntrada>()));

        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.IsAny<PlanoMensal>()),
            Times.Never);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task Salvar_MesPassadoSemPlano_LancaSomenteLeitura()
    {
        // Arrange: mes passado sem plano continua sem plano, e nao pode ser criado por Salvar
        ConfigurarSemPlano(2026, 9);

        // Act & Assert
        await Assert.ThrowsAsync<PlanoMensalPassadoSomenteLeituraException>(
            () => _service.SalvarAsync(2026, 9, 1000m, 10m, new List<SublimiteEntrada>()));

        _mockPlanoMensalRepository.Verify(
            r => r.AdicionarOuObterExistente(It.IsAny<PlanoMensal>()),
            Times.Never);
        _mockPlanoMensalRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Helpers

    private PlanejamentoMensalService CriarServico(DateTimeOffset agora) => new(
        _mockPlanoMensalRepository.Object,
        _mockCategoriaRepository.Object,
        _mockLancamentoRepository.Object,
        _mockRecebivelRecorrenteRepository.Object,
        new RelogioFixo(agora));

    private void ConfigurarMoldesAtivos(params RecebivelRecorrente[] moldes)
    {
        _mockRecebivelRecorrenteRepository
            .Setup(r => r.ListarAtivos())
            .ReturnsAsync(moldes.ToList());
    }

    private void ConfigurarCategorias(params Categoria[] categorias)
    {
        _mockCategoriaRepository
            .Setup(r => r.ListarTodas())
            .ReturnsAsync(categorias.ToList());

        foreach (var categoria in categorias)
        {
            _mockCategoriaRepository
                .Setup(r => r.ObterPorId(categoria.Id))
                .ReturnsAsync(categoria);
        }
    }

    private void ConfigurarPlanoExistente(PlanoMensal plano)
    {
        _mockPlanoMensalRepository
            .Setup(r => r.ObterPorAnoMes(plano.Ano, plano.Mes))
            .ReturnsAsync(plano);
    }

    private void ConfigurarSemPlano(int ano, int mes)
    {
        _mockPlanoMensalRepository
            .Setup(r => r.ObterPorAnoMes(ano, mes))
            .ReturnsAsync((PlanoMensal?)null);
    }

    private static Categoria CriarCategoria(
        string nome,
        TipoCategoria tipo = TipoCategoria.Despesa,
        bool arquivada = false,
        Categoria? pai = null) => new()
    {
        Id = Guid.NewGuid(),
        Nome = nome,
        Tipo = tipo,
        Arquivada = arquivada,
        ParentId = pai?.Id
    };

    private static RecebivelRecorrente CriarMolde(decimal valor) => new()
    {
        Id = Guid.NewGuid(),
        Descricao = "Salario",
        Valor = valor,
        Periodicidade = PeriodicidadeRecebivel.Mensal,
        DiaVencimento = 5,
        Ativa = true
    };

    private static PlanoMensal CriarPlano(
        int ano,
        int mes,
        decimal renda,
        decimal percentualInvestimento,
        params PlanoMensalSublimite[] sublimites)
    {
        var plano = new PlanoMensal
        {
            Id = Guid.NewGuid(),
            Ano = ano,
            Mes = mes,
            RendaPrevista = renda,
            PercentualInvestimento = percentualInvestimento
        };

        foreach (var sublimite in sublimites)
        {
            sublimite.PlanoMensalId = plano.Id;
            plano.Sublimites.Add(sublimite);
        }

        return plano;
    }

    private static PlanoMensalSublimite CriarSublimite(Categoria categoria, decimal percentual) => new()
    {
        Id = Guid.NewGuid(),
        CategoriaId = categoria.Id,
        Categoria = categoria,
        Percentual = percentual
    };

    // Relogio controlado: hoje e o instante informado, fuso UTC para nao depender da maquina.
    private sealed class RelogioFixo : TimeProvider
    {
        private readonly DateTimeOffset _agora;

        public RelogioFixo(DateTimeOffset agora) => _agora = agora;

        public override DateTimeOffset GetUtcNow() => _agora;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    #endregion
}
