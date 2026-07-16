using Moq;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class AtivoServiceTests
{
    private readonly Mock<IAtivoRepository> _mockRepository;
    private readonly AtivoService _service;

    public AtivoServiceTests()
    {
        _mockRepository = new Mock<IAtivoRepository>();
        _service = new AtivoService(_mockRepository.Object);
    }

    #region Regra 1: Criacao de ativo - nasce com valor_atual == valor_investido

    [Fact]
    public async Task CriarAtivo_ComValoresValidos_NasceComValorAtualIgualAValorInvestido()
    {
        // Arrange
        var nome = "Tesouro Direto";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var valorInvestido = 1000m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act
        var ativo = await _service.CriarAtivo(nome, tipo, instituicao, valorInvestido, dataCompra);

        // Assert
        Assert.NotEqual(Guid.Empty, ativo.Id);
        Assert.Equal(nome, ativo.Nome);
        Assert.Equal(tipo, ativo.Tipo);
        Assert.Equal(instituicao, ativo.Instituicao);
        Assert.Equal(valorInvestido, ativo.ValorInvestido);
        Assert.Equal(valorInvestido, ativo.ValorAtual); // NO FIRST DAY, ALWAYS EQUAL
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
        var valorInvestido = 5000m;
        var dataCompra = new DateOnly(2024, 2, 20);

        // Act
        await _service.CriarAtivo(nome, tipo, instituicao, valorInvestido, dataCompra);

        // Assert
        _mockRepository.Verify(r => r.Adicionar(It.IsAny<Ativo>()), Times.Once);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task CriarAtivo_ComTipoRendaFixa_CriaComSucesso()
    {
        // Arrange
        var nome = "LCI";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "Banco XYZ";
        var valorInvestido = 2000m;
        var dataCompra = new DateOnly(2024, 3, 10);

        // Act
        var ativo = await _service.CriarAtivo(nome, tipo, instituicao, valorInvestido, dataCompra);

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
        var valorInvestido = 3000m;
        var dataCompra = new DateOnly(2024, 4, 5);

        // Act
        var ativo = await _service.CriarAtivo(nome, tipo, instituicao, valorInvestido, dataCompra);

        // Assert
        Assert.Equal(TipoAtivo.RendaVariavel, ativo.Tipo);
    }

    #endregion

    #region Regra 2: Validacao - valor_investido <= 0 lanca ValorInvalidoException

    [Fact]
    public async Task CriarAtivo_ComValorInvestidoNegativo_LancaExcecao()
    {
        // Arrange
        var nome = "Tesouro";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var valorInvestido = -1000m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, valorInvestido, dataCompra));

        Assert.Equal("valor_investido", excecao.NomeCampo);
        Assert.Equal(valorInvestido, excecao.Valor);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAtivo_ComValorInvestidoZero_LancaExcecao()
    {
        // Arrange
        var nome = "Tesouro";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var valorInvestido = 0m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, valorInvestido, dataCompra));

        Assert.Equal("valor_investido", excecao.NomeCampo);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
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
        var valorInvestido = 1000m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, valorInvestido, dataCompra));

        Assert.Equal("nome", excecao.NomeCampo);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAtivo_ComNomeApenasBranco_LancaCampoObrigatorioException()
    {
        // Arrange
        var nome = "   ";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "B3";
        var valorInvestido = 1000m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, valorInvestido, dataCompra));

        Assert.Equal("nome", excecao.NomeCampo);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAtivo_ComInstituicaoVazia_LancaCampoObrigatorioException()
    {
        // Arrange
        var nome = "Tesouro Direto";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = string.Empty;
        var valorInvestido = 1000m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, valorInvestido, dataCompra));

        Assert.Equal("instituicao", excecao.NomeCampo);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAtivo_ComInstituicaoApenasBranco_LancaCampoObrigatorioException()
    {
        // Arrange
        var nome = "Tesouro Direto";
        var tipo = TipoAtivo.RendaFixa;
        var instituicao = "  \t\n  ";
        var valorInvestido = 1000m;
        var dataCompra = new DateOnly(2024, 1, 15);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.CriarAtivo(nome, tipo, instituicao, valorInvestido, dataCompra));

        Assert.Equal("instituicao", excecao.NomeCampo);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
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

        _mockRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act
        await _service.AtualizarValorAtual(ativoId, novoValorAtual);

        // Assert
        Assert.Equal(novoValorAtual, ativoExistente.ValorAtual);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
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

        _mockRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.AtualizarValorAtual(ativoId, novoValorAtual));

        Assert.Equal("valor_atual", excecao.NomeCampo);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
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

        _mockRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.AtualizarValorAtual(ativoId, 0m));

        Assert.Equal("valor_atual", excecao.NomeCampo);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
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

        _mockRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act
        await _service.DesativarAtivo(ativoId);

        // Assert
        Assert.False(ativoExistente.Ativa);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
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

        _mockRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoExistente);

        // Act
        await _service.DesativarAtivo(ativoId);

        // Assert
        Assert.False(ativoExistente.Ativa);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 7: Tratamento de ativo inexistente

    [Fact]
    public async Task AtualizarValorAtual_ComAtivoInexistente_LancaAtivoNaoEncontradoException()
    {
        // Arrange
        var ativoIdInexistente = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.ObterPorId(ativoIdInexistente))
            .ReturnsAsync((Ativo?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<AtivoNaoEncontradoException>(
            () => _service.AtualizarValorAtual(ativoIdInexistente, 1000m));

        Assert.Equal(ativoIdInexistente, excecao.AtivoId);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task DesativarAtivo_ComAtivoInexistente_LancaAtivoNaoEncontradoException()
    {
        // Arrange
        var ativoIdInexistente = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.ObterPorId(ativoIdInexistente))
            .ReturnsAsync((Ativo?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<AtivoNaoEncontradoException>(
            () => _service.DesativarAtivo(ativoIdInexistente));

        Assert.Equal(ativoIdInexistente, excecao.AtivoId);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
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

        _mockRepository
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

        _mockRepository
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

        _mockRepository
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
        _mockRepository
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

        _mockRepository
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
}
