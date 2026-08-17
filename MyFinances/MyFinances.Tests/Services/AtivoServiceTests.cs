using Moq;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;
using MyFinances.Tests.Helpers;

namespace MyFinances.Tests.Services;

public class AtivoServiceTests
{
    private readonly Mock<IAtivoRepository> _mockAtivoRepository;
    private readonly Mock<IRendimentoService> _mockRendimentoService;
    private readonly Mock<IAtivoAporteRepository> _mockAporteRepository;
    private readonly AtivoService _service;

    public AtivoServiceTests()
    {
        _mockAtivoRepository = new Mock<IAtivoRepository>();
        _mockRendimentoService = new Mock<IRendimentoService>();
        _mockAporteRepository = new Mock<IAtivoAporteRepository>();
        _service = new AtivoService(_mockAtivoRepository.Object, _mockRendimentoService.Object, _mockAporteRepository.Object);
    }

    #region Regra 1: Criacao de ativo - nasce com valor_atual == valor_investido

    [Fact]
    public async Task CriarAtivo_ComValoresValidos_NasceComValorAtualIgualAValorInvestido()
    {
        // Arrange
        var nome = "Tesouro Direto";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var quantidade = 10m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act
        var ativo = await _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra);

        // Assert
        Assert.NotEqual(Guid.Empty, ativo.Id);
        Assert.Equal(nome, ativo.Nome);
        Assert.Equal(tipo, ativo.Tipo);
        Assert.Equal(instituicao, ativo.Instituicao);
        Assert.Equal(quantidade, ativo.Quantidade);
        Assert.Equal(1000m, ativo.ValorInvestido); // quantidade * precoUnitario
        Assert.Equal(1000m, ativo.ValorAtual); // NO FIRST DAY, ALWAYS EQUAL
        Assert.Equal(dataCompra, ativo.DataCompra);
        Assert.True(ativo.Ativa);
    }

    [Fact]
    public async Task CriarAtivo_PersistenciaChamadoAoAdicionar()
    {
        // Arrange
        var nome = "ETF Acoes";
        var tipo = TipoAtivo.RendaVariavel;
        var instituicao = "XP";
        var quantidade = 50m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 2, 20);

        // Act
        await _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra);

        // Assert
        _mockAtivoRepository.Verify(r => r.Adicionar(It.IsAny<Ativo>()), Times.Once);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Once);
        _mockAporteRepository.Verify(r => r.Adicionar(It.IsAny<AtivoAporte>()), Times.Once);
        _mockAporteRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task CriarAtivo_ComTipoRendaFixa_CriaComSucesso()
    {
        // Arrange
        var nome = "LCI";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "Banco XYZ";
        var quantidade = 20m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 3, 10);

        // Act
        var ativo = await _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra);

        // Assert
        Assert.Equal(TipoAtivo.RendaFixa, ativo.Tipo);
        Assert.True(ativo.Ativa);
    }

    [Fact]
    public async Task CriarAtivo_ComTipoRendaVariavel_CriaComSucesso()
    {
        // Arrange
        var nome = "Fundo Imobiliario";
        var tipo = TipoAtivo.RendaVariavel;
        var instituicao = "Itau";
        var quantidade = 30m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 4, 5);

        // Act
        var ativo = await _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra);

        // Assert
        Assert.Equal(TipoAtivo.RendaVariavel, ativo.Tipo);
    }

    #endregion

    #region Regra 2: Validacao - quantidade ou preco <= 0 lanca ValorInvalidoException

    [Fact]
    public async Task CriarAtivo_ComQuantidadeNegativa_LancaExcecao()
    {
        // Arrange
        var nome = "Tesouro";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var quantidade = -10m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra));

        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAtivo_ComQuantidadeZero_LancaExcecao()
    {
        // Arrange
        var nome = "Tesouro";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var quantidade = 0m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra));

        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAtivo_ComPrecoUnitarioNegativo_LancaExcecao()
    {
        // Arrange
        var nome = "Tesouro";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var quantidade = 10m;
        var precoUnitario = -100m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra));

        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAtivo_ComPrecoUnitarioZero_LancaExcecao()
    {
        // Arrange
        var nome = "Tesouro";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var quantidade = 10m;
        var precoUnitario = 0m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra));

        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 3: Validacao de campos obrigatorios

    [Fact]
    public async Task CriarAtivo_ComNomeVazio_LancaCampoObrigatorioException()
    {
        // Arrange
        var nome = string.Empty;
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var quantidade = 10m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra));

        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAtivo_ComNomeApenasBranco_LancaCampoObrigatorioException()
    {
        // Arrange
        var nome = "   ";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var quantidade = 10m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra));

        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAtivo_ComInstituicaoVazia_LancaCampoObrigatorioException()
    {
        // Arrange
        var nome = "Tesouro Direto";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = string.Empty;
        var quantidade = 10m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra));

        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAtivo_ComInstituicaoApenasBranco_LancaCampoObrigatorioException()
    {
        // Arrange
        var nome = "Tesouro Direto";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "  \t\n  ";
        var quantidade = 10m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra));

        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 4: Calculo de evolucao percentual

    [Fact]
    public void CalcularEvolucaoPercentual_ComValorAtualIgualAoInvestido_Retorna0()
    {
        // Arrange
        var valorInvestido = 1000m;
        var valorAtual = 1000m; // NO FIRST DAY

        // Act
        var evolucao = _service.CalcularEvolucaoPercentual(valorInvestido, valorAtual);

        // Assert
        Assert.Equal(0m, evolucao);
    }

    [Fact]
    public void CalcularEvolucaoPercentual_ComValorAtualMaiorQueInvestido_RetornaPositivo()
    {
        // Arrange
        var valorInvestido = 1000m;
        var valorAtual = 1200m; // +20%

        // Act
        var evolucao = _service.CalcularEvolucaoPercentual(valorInvestido, valorAtual);

        // Assert
        Assert.Equal(20m, evolucao); // (1200 - 1000) / 1000 * 100 = 20
    }

    [Fact]
    public void CalcularEvolucaoPercentual_ComValorAtualMenorQueInvestido_RetornaNegativo()
    {
        // Arrange
        var valorInvestido = 1000m;
        var valorAtual = 800m; // -20%

        // Act
        var evolucao = _service.CalcularEvolucaoPercentual(valorInvestido, valorAtual);

        // Assert
        Assert.Equal(-20m, evolucao); // (800 - 1000) / 1000 * 100 = -20
    }

    [Fact]
    public void CalcularEvolucaoPercentual_ComPequenaEvoluacao_RetornaPreciso()
    {
        // Arrange
        var valorInvestido = 10000m;
        var valorAtual = 10050m; // +0.5%

        // Act
        var evolucao = _service.CalcularEvolucaoPercentual(valorInvestido, valorAtual);

        // Assert
        Assert.Equal(0.5m, evolucao);
    }

    #endregion

    #region Regra 4.1: Calculo de preco medio (regra-de-negocio.md item 8.1)

    [Fact]
    public void CalcularPrecoMedio_ComValoresValidos_RetornaDivisaoCorreta()
    {
        // Arrange
        var valorInvestido = 1000m;
        var quantidade = 10m;

        // Act
        var precoMedio = _service.CalcularPrecoMedio(valorInvestido, quantidade);

        // Assert
        Assert.Equal(100m, precoMedio); // 1000 / 10 = 100
    }

    [Fact]
    public void CalcularPrecoMedio_ComQuantidadeZero_RetornaZero()
    {
        // Arrange
        var valorInvestido = 1000m;
        var quantidade = 0m;

        // Act
        var precoMedio = _service.CalcularPrecoMedio(valorInvestido, quantidade);

        // Assert
        Assert.Equal(0m, precoMedio);
    }

    [Fact]
    public void CalcularPrecoMedio_ComValoresDecimais_RetornaValorPreciso()
    {
        // Arrange
        var valorInvestido = 255m; // 25.5 * 10
        var quantidade = 25.5m;

        // Act
        var precoMedio = _service.CalcularPrecoMedio(valorInvestido, quantidade);

        // Assert
        Assert.Equal(10m, precoMedio); // 255 / 25.5 = 10
    }

    [Fact]
    public void CalcularPrecoMedio_ComValoresQueBuscamDivisaoComResto_RetornaDivisaoExata()
    {
        // Arrange
        var valorInvestido = 451.875m;
        var quantidade = 38m;

        // Act
        var precoMedio = _service.CalcularPrecoMedio(valorInvestido, quantidade);

        // Assert
        var esperado = 451.875m / 38m; // Aproximadamente 11.89...
        Assert.Equal(esperado, precoMedio);
    }

    #endregion

    #region Regra 5: Atualizacao manual de valor_atual

    [Fact]
    public async Task AtualizarValorAtual_ComNovoValorValido_AtualizaComSucesso()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true
        };

        var novoValorAtual = 1200m;

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act
        await _service.AtualizarValorAtual(ativoId, novoValorAtual);

        // Assert
        Assert.Equal(novoValorAtual, ativoExistente.ValorAtual);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task AtualizarValorAtual_ComNovoValorNegativo_LancaExcecao()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "ETF",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "XP",
            ValorInvestido = 5000m,
            ValorAtual = 5000m,
            DataCompra = new DateOnly(2024, 2, 20),
            Ativa = true
        };

        var novoValorAtual = -100m;

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.AtualizarValorAtual(ativoId, novoValorAtual));

        Assert.Equal("valor_atual", excecao.NomeCampo);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task AtualizarValorAtual_ComNovoValorZero_LancaExcecao()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "LCI",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "Banco",
            ValorInvestido = 2000m,
            ValorAtual = 2000m,
            DataCompra = new DateOnly(2024, 3, 10),
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.AtualizarValorAtual(ativoId, 0m));

        Assert.Equal("valor_atual", excecao.NomeCampo);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 6: Desativacao (soft-delete)

    [Fact]
    public async Task DesativarAtivo_ComAtivoExistente_MarcaComoInativo()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act
        await _service.DesativarAtivo(ativoId);

        // Assert
        Assert.False(ativoExistente.Ativa);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task DesativarAtivo_ComAtivoJaInativo_NaoLancaExcecao()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Fundo",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "Itau",
            ValorInvestido = 3000m,
            ValorAtual = 3000m,
            DataCompra = new DateOnly(2024, 4, 5),
            Ativa = false
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act
        await _service.DesativarAtivo(ativoId);

        // Assert
        Assert.False(ativoExistente.Ativa);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 7: Tratamento de ativo inexistente

    [Fact]
    public async Task AtualizarValorAtual_ComAtivoInexistente_LancaAtivoNaoEncontradoException()
    {
        // Arrange
        var ativoIdInexistente = Guid.NewGuid();

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoIdInexistente))
            .ReturnsAsync((Ativo?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<AtivoNaoEncontradoException>(
            () => _service.AtualizarValorAtual(ativoIdInexistente, 1000m));

        Assert.Equal(ativoIdInexistente, excecao.AtivoId);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task DesativarAtivo_ComAtivoInexistente_LancaAtivoNaoEncontradoException()
    {
        // Arrange
        var ativoIdInexistente = Guid.NewGuid();

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoIdInexistente))
            .ReturnsAsync((Ativo?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<AtivoNaoEncontradoException>(
            () => _service.DesativarAtivo(ativoIdInexistente));

        Assert.Equal(ativoIdInexistente, excecao.AtivoId);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 8: Listar ativos ativos

    [Fact]
    public async Task ListarAtivos_RetornaApenasAtivosComAtivaTrue()
    {
        // Arrange
        var ativosNoRepositorio = new List<Ativo>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Tesouro 1",
                Tipo = TipoAtivo.RendaFixa,
                Instituicao = "B3",
                ValorInvestido = 1000m,
                ValorAtual = 1000m,
                DataCompra = new DateOnly(2024, 1, 15),
                Ativa = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Tesouro Desativado",
                Tipo = TipoAtivo.RendaFixa,
                Instituicao = "B3",
                ValorInvestido = 500m,
                ValorAtual = 500m,
                DataCompra = new DateOnly(2024, 1, 10),
                Ativa = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "ETF Ativo",
                Tipo = TipoAtivo.RendaVariavel,
                Instituicao = "XP",
                ValorInvestido = 5000m,
                ValorAtual = 5000m,
                DataCompra = new DateOnly(2024, 2, 20),
                Ativa = true
            }
        };

        _mockAtivoRepository
            .Setup(r => r.ListarAtivas())
            .ReturnsAsync(ativosNoRepositorio.Where(a => a.Ativa));

        // Act
        var resultado = await _service.ListarAtivos();

        // Assert
        Assert.Equal(2, resultado.Count());
        Assert.All(resultado, ativo => Assert.True(ativo.Ativa));
        Assert.Contains(resultado, a => a.Nome == "Tesouro 1");
        Assert.Contains(resultado, a => a.Nome == "ETF Ativo");
        Assert.DoesNotContain(resultado, a => a.Nome == "Tesouro Desativado");
    }

    [Fact]
    public async Task ListarAtivos_ComTodosInativos_RetornaVazio()
    {
        // Arrange
        var ativosNoRepositorio = new List<Ativo>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Ativo Desativado",
                Tipo = TipoAtivo.RendaFixa,
                Instituicao = "B3",
                ValorInvestido = 1000m,
                ValorAtual = 1000m,
                DataCompra = new DateOnly(2024, 1, 15),
                Ativa = false
            }
        };

        _mockAtivoRepository
            .Setup(r => r.ListarAtivas())
            .ReturnsAsync(ativosNoRepositorio.Where(a => a.Ativa));

        // Act
        var resultado = await _service.ListarAtivos();

        // Assert
        Assert.Empty(resultado);
    }

    #endregion

    #region Regra 9: Resumo por tipo

    [Fact]
    public async Task ObterResumo_ComMultiplosAtivos_CalculaTotaisEPercentuais()
    {
        // Arrange
        var ativosNoRepositorio = new List<Ativo>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Tesouro IPCA",
                Tipo = TipoAtivo.RendaFixa,
                Instituicao = "B3",
                ValorInvestido = 1000m,
                ValorAtual = 1100m, // +100
                DataCompra = new DateOnly(2024, 1, 15),
                Ativa = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "ETF IBOV",
                Tipo = TipoAtivo.RendaVariavel,
                Instituicao = "XP",
                ValorInvestido = 5000m,
                ValorAtual = 5400m, // +400
                DataCompra = new DateOnly(2024, 2, 20),
                Ativa = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "LCI",
                Tipo = TipoAtivo.RendaFixa,
                Instituicao = "Banco",
                ValorInvestido = 2000m,
                ValorAtual = 2000m,
                DataCompra = new DateOnly(2024, 3, 10),
                Ativa = true
            }
        };

        _mockAtivoRepository
            .Setup(r => r.ListarAtivas())
            .ReturnsAsync(ativosNoRepositorio);

        // Act
        var resumo = await _service.ObterResumo();

        // Assert
        // TotalInvestido = 1000 + 5000 + 2000 = 8000
        Assert.Equal(8000m, resumo.TotalInvestido);

        // TotalAtual = 1100 + 5400 + 2000 = 8500
        Assert.Equal(8500m, resumo.TotalAtual);

        // PorTipo debe ter 2 tipos: RendaFixa e RendaVariavel
        Assert.Equal(2, resumo.PorTipo.Count());

        // RendaFixa: ValorAtual = 1100 + 2000 = 3100, Percentual = 3100 / 8500 = 36.47%
        var rendaFixa = resumo.PorTipo.FirstOrDefault(t => t.Tipo == "RENDA_FIXA");
        Assert.NotNull(rendaFixa);
        Assert.Equal(3100m, rendaFixa.ValorAtual);
        Assert.True(rendaFixa.PercentualDaCarteira > 36m && rendaFixa.PercentualDaCarteira < 37m);

        // RendaVariavel: ValorAtual = 5400, Percentual = 5400 / 8500 = 63.53%
        var rendaVariavel = resumo.PorTipo.FirstOrDefault(t => t.Tipo == "RENDA_VARIAVEL");
        Assert.NotNull(rendaVariavel);
        Assert.Equal(5400m, rendaVariavel.ValorAtual);
        Assert.True(rendaVariavel.PercentualDaCarteira > 63m && rendaVariavel.PercentualDaCarteira < 64m);
    }

    [Fact]
    public async Task ObterResumo_SemAtivos_RetornaTotaisZero()
    {
        // Arrange
        _mockAtivoRepository
            .Setup(r => r.ListarAtivas())
            .ReturnsAsync(new List<Ativo>());

        // Act
        var resumo = await _service.ObterResumo();

        // Assert
        Assert.Equal(0m, resumo.TotalInvestido);
        Assert.Equal(0m, resumo.TotalAtual);
        Assert.Empty(resumo.PorTipo);
    }

    [Fact]
    public async Task ObterResumo_ApenasUmTipo_RetornaComPercentual100()
    {
        // Arrange
        var ativosNoRepositorio = new List<Ativo>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Tesouro 1",
                Tipo = TipoAtivo.RendaFixa,
                Instituicao = "B3",
                ValorInvestido = 1000m,
                ValorAtual = 1100m,
                DataCompra = new DateOnly(2024, 1, 15),
                Ativa = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Tesouro 2",
                Tipo = TipoAtivo.RendaFixa,
                Instituicao = "B3",
                ValorInvestido = 2000m,
                ValorAtual = 2200m,
                DataCompra = new DateOnly(2024, 2, 10),
                Ativa = true
            }
        };

        _mockAtivoRepository
            .Setup(r => r.ListarAtivas())
            .ReturnsAsync(ativosNoRepositorio);

        // Act
        var resumo = await _service.ObterResumo();

        // Assert
        Assert.Single(resumo.PorTipo);
        var rendaFixa = resumo.PorTipo.First();
        Assert.Equal("RENDA_FIXA", rendaFixa.Tipo);
        Assert.Equal(3300m, rendaFixa.ValorAtual); // 1100 + 2200
        Assert.Equal(100m, rendaFixa.PercentualDaCarteira); // 100% (unico tipo)
    }

    #endregion

    #region Regra 10: Primeiro aporte via CriarAtivo define quantidade/valor_investido/preco_medio iniciais

    [Fact]
    public async Task CriarAtivo_PrimeiroAporte_DefineQuantidadeValorInvestidoEPrecoMedio()
    {
        // Arrange
        var nome = "Tesouro SELIC";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var quantidade = 10m;
        var precoUnitario = 100m;
        var dataCompra = new DateOnly(2024, 5, 15);

        // Act
        var ativo = await _service.CriarAtivo(nome, tipo, instituicao, quantidade, precoUnitario, dataCompra);

        // Assert
        Assert.Equal(quantidade, ativo.Quantidade);
        Assert.Equal(1000m, ativo.ValorInvestido); // 10 * 100
        // preco_medio = valor_investido / quantidade = 1000 / 10 = 100
        Assert.Equal(100m, ativo.ValorInvestido / ativo.Quantidade);
    }

    #endregion

    #region Regra 11: RegistrarAporte recalcula preco_medio pela formula ponderada

    [Fact]
    public async Task RegistrarAporte_ComSegundoAporte_RecalculaPrecoMedioPonderado()
    {
        // Arrange - Caso didatico: 10 cotas a R$10 + 10 cotas a R$20 = preco medio R$15
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Fundo Imobiliario",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "XP",
            Quantidade = 10m,
            ValorInvestido = 100m, // 10 * 10
            ValorAtual = 100m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        var novaQuantidade = 10m;
        var novoPreco = 20m;
        var dataAporte = new DateOnly(2024, 6, 15);

        // Act
        var aporte = await _service.RegistrarAporte(ativoId, novaQuantidade, novoPreco, dataAporte);

        // Assert
        // preco_medio_novo = (100 * 10 + 20 * 10) / (10 + 10) = (1000 + 200) / 20 = 1200 / 20 = 60
        Assert.Equal(ativoId, aporte.AtivoId);
        Assert.Equal(novaQuantidade, aporte.Quantidade);
        Assert.Equal(novoPreco, aporte.PrecoUnitario);
        Assert.Equal(dataAporte, aporte.Data);

        // Ativo deve ter quantidade incrementada
        Assert.Equal(20m, ativoExistente.Quantidade); // 10 + 10
        // Ativo deve ter valor_investido incrementado
        Assert.Equal(300m, ativoExistente.ValorInvestido); // 100 + (10 * 20)
        // preco_medio_novo = 300 / 20 = 15
        Assert.Equal(15m, ativoExistente.ValorInvestido / ativoExistente.Quantidade);
    }

    [Fact]
    public async Task RegistrarAporte_ApertadaComDecimais_CalculaPrecoMedioCorreto()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Acao",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "Itau",
            Quantidade = 25.5m,
            ValorInvestido = 255m, // 25.5 * 10
            ValorAtual = 255m,
            DataCompra = new DateOnly(2024, 2, 10),
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        var novaQuantidade = 12.5m;
        var novoPreco = 15.75m;
        var dataAporte = new DateOnly(2024, 7, 20);

        // Act
        var aporte = await _service.RegistrarAporte(ativoId, novaQuantidade, novoPreco, dataAporte);

        // Assert
        Assert.NotNull(aporte);
        Assert.Equal(ativoId, aporte.AtivoId);
        // Quantidade incrementada
        Assert.Equal(38m, ativoExistente.Quantidade); // 25.5 + 12.5
        // Valor incrementado
        Assert.Equal(451.875m, ativoExistente.ValorInvestido); // 255 + (12.5 * 15.75)
    }

    #endregion

    #region Regra 12: Aporte com quantidade ou preco <= 0 e rejeitado

    [Fact]
    public async Task RegistrarAporte_ComQuantidadeNegativa_LancaExcecao()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 10m,
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarAporte(ativoId, -5m, 100m, new DateOnly(2024, 6, 15)));

        Assert.Equal("quantidade", excecao.NomeCampo);
        _mockAporteRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task RegistrarAporte_ComQuantidadeZero_LancaExcecao()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 10m,
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarAporte(ativoId, 0m, 100m, new DateOnly(2024, 6, 15)));

        Assert.Equal("quantidade", excecao.NomeCampo);
        _mockAporteRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task RegistrarAporte_ComPrecoNegativo_LancaExcecao()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 10m,
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarAporte(ativoId, 5m, -100m, new DateOnly(2024, 6, 15)));

        Assert.Equal("preco_unitario", excecao.NomeCampo);
        _mockAporteRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task RegistrarAporte_ComPrecoZero_LancaExcecao()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 10m,
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarAporte(ativoId, 5m, 0m, new DateOnly(2024, 6, 15)));

        Assert.Equal("preco_unitario", excecao.NomeCampo);
        _mockAporteRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 13: valor_atual NAO muda automaticamente ao registrar aporte

    [Fact]
    public async Task RegistrarAporte_NaoAlteraValorAtual()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Fundo",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "Banco",
            Quantidade = 20m,
            ValorInvestido = 2000m,
            ValorAtual = 2500m, // valor_atual > valor_investido
            DataCompra = new DateOnly(2024, 1, 10),
            Ativa = true
        };

        var valorAtualAntes = ativoExistente.ValorAtual;

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act
        await _service.RegistrarAporte(ativoId, 10m, 150m, new DateOnly(2024, 8, 10));

        // Assert
        Assert.Equal(valorAtualAntes, ativoExistente.ValorAtual); // Nao mudou
    }

    #endregion

    #region Regra 14: ListarAportes retorna historico em ordem cronologica

    [Fact]
    public async Task ListarAportes_RetornaAportesEmOrdenCronologica()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var aportes = new List<AtivoAporte>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AtivoId = ativoId,
                Data = new DateOnly(2024, 1, 10),
                Quantidade = 10m,
                PrecoUnitario = 100m,
                CriadoEm = DateTime.UtcNow.AddDays(-100)
            },
            new()
            {
                Id = Guid.NewGuid(),
                AtivoId = ativoId,
                Data = new DateOnly(2024, 3, 15),
                Quantidade = 5m,
                PrecoUnitario = 110m,
                CriadoEm = DateTime.UtcNow.AddDays(-50)
            },
            new()
            {
                Id = Guid.NewGuid(),
                AtivoId = ativoId,
                Data = new DateOnly(2024, 6, 20),
                Quantidade = 8m,
                PrecoUnitario = 120m,
                CriadoEm = DateTime.UtcNow.AddDays(-10)
            }
        };

        _mockAporteRepository
            .Setup(r => r.ListarPorAtivo(ativoId))
            .ReturnsAsync(aportes.OrderBy(a => a.Data));

        // Act
        var resultado = await _service.ListarAportes(ativoId);

        // Assert
        var aportesOrdenados = resultado.ToList();
        Assert.Equal(3, aportesOrdenados.Count);
        Assert.Equal(new DateOnly(2024, 1, 10), aportesOrdenados[0].Data);
        Assert.Equal(new DateOnly(2024, 3, 15), aportesOrdenados[1].Data);
        Assert.Equal(new DateOnly(2024, 6, 20), aportesOrdenados[2].Data);
    }

    [Fact]
    public async Task ListarAportes_AtivoSemAportes_RetornaVazio()
    {
        // Arrange
        var ativoId = Guid.NewGuid();

        _mockAporteRepository
            .Setup(r => r.ListarPorAtivo(ativoId))
            .ReturnsAsync(new List<AtivoAporte>());

        // Act
        var resultado = await _service.ListarAportes(ativoId);

        // Assert
        Assert.Empty(resultado);
    }

    #endregion

    #region Regra 15: AtivoNaoEncontradoException ao aportar em ativo inexistente ou desativado

    [Fact]
    public async Task RegistrarAporte_AtivoInexistente_LancaAtivoNaoEncontradoException()
    {
        // Arrange
        var ativoIdInexistente = Guid.NewGuid();

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoIdInexistente))
            .ReturnsAsync((Ativo?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<AtivoNaoEncontradoException>(
            () => _service.RegistrarAporte(ativoIdInexistente, 10m, 100m, new DateOnly(2024, 6, 15)));

        Assert.Equal(ativoIdInexistente, excecao.AtivoId);
        _mockAporteRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task RegistrarAporte_AtivoDesativado_LancaAtivoNaoEncontradoException()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var ativoDesativado = new Ativo
        {
            Id = ativoId,
            Nome = "Tesouro Desativado",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 10m,
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = false // Desativado
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoDesativado);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<AtivoNaoEncontradoException>(
            () => _service.RegistrarAporte(ativoId, 5m, 100m, new DateOnly(2024, 6, 15)));

        Assert.Equal(ativoId, excecao.AtivoId);
        _mockAporteRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 16: Integracao com RendimentoService - AtualizarValorAtual gera Rendimento automaticamente

    [Fact]
    public async Task AtualizarValorAtual_ComDeltaPositivo_GeraUmRendimentoValorizacaoComDeltaCorreto()
    {
        // Arrange - Fake repositories para integracao real
        var fakeRendimentoRepository = new FakeRendimentoRepository();
        var rendimentoService = new RendimentoService(fakeRendimentoRepository, _mockAtivoRepository.Object);
        var fakeAtivoRepository = new FakeAtivoRepository();
        var ativoService = new AtivoService(fakeAtivoRepository, rendimentoService, Mock.Of<IAtivoAporteRepository>());

        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true
        };
        await fakeAtivoRepository.Adicionar(ativoExistente);
        await fakeAtivoRepository.Salvar();

        var novoValorAtual = 1200m;
        var deltaEsperado = 200m;

        // Act
        await ativoService.AtualizarValorAtual(ativoId, novoValorAtual);

        // Assert
        var rendimentos = await fakeRendimentoRepository.ListarPorAtivo(ativoId);
        Assert.Single(rendimentos);

        var rendimento = rendimentos.First();
        Assert.Equal(ativoId, rendimento.AtivoId);
        Assert.Equal(TipoRendimento.Valorizacao, rendimento.Tipo);
        Assert.Equal(OrigemRendimento.Automatico, rendimento.Origem);
        Assert.Equal(deltaEsperado, rendimento.Valor);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), rendimento.Data);
    }

    [Fact]
    public async Task AtualizarValorAtual_ComDeltaNegativo_GeraRendimentoValorizacaoNegativo()
    {
        // Arrange
        var fakeRendimentoRepository = new FakeRendimentoRepository();
        var rendimentoService = new RendimentoService(fakeRendimentoRepository, _mockAtivoRepository.Object);
        var fakeAtivoRepository = new FakeAtivoRepository();
        var ativoService = new AtivoService(fakeAtivoRepository, rendimentoService, Mock.Of<IAtivoAporteRepository>());

        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "ETF",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "XP",
            ValorInvestido = 5000m,
            ValorAtual = 5000m,
            DataCompra = new DateOnly(2024, 2, 20),
            Ativa = true
        };
        await fakeAtivoRepository.Adicionar(ativoExistente);
        await fakeAtivoRepository.Salvar();

        var novoValorAtual = 4800m;
        var deltaEsperado = -200m;

        // Act
        await ativoService.AtualizarValorAtual(ativoId, novoValorAtual);

        // Assert
        var rendimentos = await fakeRendimentoRepository.ListarPorAtivo(ativoId);
        Assert.Single(rendimentos);

        var rendimento = rendimentos.First();
        Assert.Equal(deltaEsperado, rendimento.Valor);
        Assert.Equal(OrigemRendimento.Automatico, rendimento.Origem);
    }

    [Fact]
    public async Task AtualizarValorAtual_ComMesmoValor_NaoCriaRendimento()
    {
        // Arrange
        var fakeRendimentoRepository = new FakeRendimentoRepository();
        var rendimentoService = new RendimentoService(fakeRendimentoRepository, _mockAtivoRepository.Object);
        var fakeAtivoRepository = new FakeAtivoRepository();
        var ativoService = new AtivoService(fakeAtivoRepository, rendimentoService, Mock.Of<IAtivoAporteRepository>());

        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "LCI",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "Banco",
            ValorInvestido = 2000m,
            ValorAtual = 2000m,
            DataCompra = new DateOnly(2024, 3, 10),
            Ativa = true
        };
        await fakeAtivoRepository.Adicionar(ativoExistente);
        await fakeAtivoRepository.Salvar();

        // Act
        await ativoService.AtualizarValorAtual(ativoId, 2000m);

        // Assert
        var rendimentos = await fakeRendimentoRepository.ListarPorAtivo(ativoId);
        Assert.Empty(rendimentos);
    }

    [Fact]
    public async Task AtualizarValorAtual_DuasVezesComMesmoValor_NaoGeraRendimentoNaSegundaVez()
    {
        // Arrange
        var fakeRendimentoRepository = new FakeRendimentoRepository();
        var rendimentoService = new RendimentoService(fakeRendimentoRepository, _mockAtivoRepository.Object);
        var fakeAtivoRepository = new FakeAtivoRepository();
        var ativoService = new AtivoService(fakeAtivoRepository, rendimentoService, Mock.Of<IAtivoAporteRepository>());

        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Fundo",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "Itau",
            ValorInvestido = 3000m,
            ValorAtual = 3000m,
            DataCompra = new DateOnly(2024, 4, 5),
            Ativa = true
        };
        await fakeAtivoRepository.Adicionar(ativoExistente);
        await fakeAtivoRepository.Salvar();

        var novoValor1 = 3500m;
        var novoValor2 = 3500m;

        // Act
        await ativoService.AtualizarValorAtual(ativoId, novoValor1);
        await ativoService.AtualizarValorAtual(ativoId, novoValor2);

        // Assert
        var rendimentos = await fakeRendimentoRepository.ListarPorAtivo(ativoId);
        Assert.Single(rendimentos); // Apenas a primeira atualizacao gera rendimento

        var rendimento = rendimentos.First();
        Assert.Equal(500m, rendimento.Valor); // 3500 - 3000
    }

    [Fact]
    public async Task AtualizarValorAtual_DuasVezesComValoresDiferentes_GeraDoiseRegistrosDistintos()
    {
        // Arrange
        var fakeRendimentoRepository = new FakeRendimentoRepository();
        var rendimentoService = new RendimentoService(fakeRendimentoRepository, _mockAtivoRepository.Object);
        var fakeAtivoRepository = new FakeAtivoRepository();
        var ativoService = new AtivoService(fakeAtivoRepository, rendimentoService, Mock.Of<IAtivoAporteRepository>());

        var ativoId = Guid.NewGuid();
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            Nome = "Tesouro Prefixado",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 10000m,
            ValorAtual = 10000m,
            DataCompra = new DateOnly(2024, 1, 1),
            Ativa = true
        };
        await fakeAtivoRepository.Adicionar(ativoExistente);
        await fakeAtivoRepository.Salvar();

        var novoValor1 = 10500m;
        var novoValor2 = 11000m;

        // Act
        await ativoService.AtualizarValorAtual(ativoId, novoValor1);
        await ativoService.AtualizarValorAtual(ativoId, novoValor2);

        // Assert
        var rendimentos = await fakeRendimentoRepository.ListarPorAtivo(ativoId);
        Assert.Equal(2, rendimentos.Count());

        // Verificar primeiro rendimento
        var primeiro = rendimentos.OrderBy(r => r.Data).First();
        Assert.Equal(500m, primeiro.Valor); // 10500 - 10000

        // Verificar segundo rendimento
        var segundo = rendimentos.OrderBy(r => r.Data).Last();
        Assert.Equal(500m, segundo.Valor); // 11000 - 10500

        // Verificar que estao ordenados por Data
        Assert.True(primeiro.Data <= segundo.Data);
    }

    #endregion
}
