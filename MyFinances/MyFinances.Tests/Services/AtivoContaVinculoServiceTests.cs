using Moq;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class AtivoContaVinculoServiceTests
{
    private readonly Mock<IAtivoRepository> _mockAtivoRepository;
    private readonly Mock<IRendimentoService> _mockRendimentoService;
    private readonly Mock<IAtivoAporteRepository> _mockAporteRepository;
    private readonly Mock<IContaRepository> _mockContaRepository;
    private readonly AtivoService _service;

    public AtivoContaVinculoServiceTests()
    {
        _mockAtivoRepository = new Mock<IAtivoRepository>();
        _mockRendimentoService = new Mock<IRendimentoService>();
        _mockAporteRepository = new Mock<IAtivoAporteRepository>();
        _mockContaRepository = new Mock<IContaRepository>();
        _service = new AtivoService(
            _mockAtivoRepository.Object,
            _mockRendimentoService.Object,
            _mockAporteRepository.Object,
            _mockContaRepository.Object);
    }

    #region Helpers

    private static Conta CriarContaInvestimento(Guid id, bool ativa = true)
    {
        return new Conta
        {
            Id = id,
            Nome = "Conta investimento teste",
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Investimento,
            SaldoManual = 0m,
            Ativa = ativa
        };
    }

    private static Conta CriarContaBanco(Guid id)
    {
        return new Conta
        {
            Id = id,
            Nome = "Conta banco teste",
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Banco,
            SaldoManual = 100m,
            Ativa = true
        };
    }

    private static Ativo CriarAtivoExistente(Guid? contaId, bool ativa = true)
    {
        return new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Ativo existente",
            Tipo = TipoAtivo.RendaVariavel,
            ContaId = contaId,
            Quantidade = 10m,
            ValorInvestido = 1000m,
            ValorAtual = 1200m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = ativa,
            CriadoEm = new DateTime(2024, 1, 15),
            Rendimentos = new List<Rendimento>
            {
                new() { Id = Guid.NewGuid(), Valor = 10m },
                new() { Id = Guid.NewGuid(), Valor = 20m }
            }
        };
    }

    private void ConfigurarAtivo(Ativo ativo)
    {
        _mockAtivoRepository.Setup(r => r.ObterPorId(ativo.Id)).ReturnsAsync(ativo);
    }

    private void ConfigurarConta(Conta conta)
    {
        _mockContaRepository.Setup(r => r.ObterPorId(conta.Id)).ReturnsAsync(conta);
    }

    private void VerificarNadaPersistido()
    {
        _mockAtivoRepository.Verify(r => r.Adicionar(It.IsAny<Ativo>()), Times.Never);
        _mockAporteRepository.Verify(r => r.Adicionar(It.IsAny<AtivoAporte>()), Times.Never);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
        _mockAporteRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region CriarAtivo com conta - validacoes (nada persistido nos erros)

    [Fact]
    public async Task CriarAtivo_ContaIdVazio_LancaCampoObrigatorioContaId()
    {
        // Act
        var ex = await Assert.ThrowsAsync<CampoObrigatorioException>(() =>
            _service.CriarAtivo("ETF", TipoAtivo.RendaVariavel, Guid.Empty, 10m, 100m, new DateOnly(2024, 2, 1)));

        // Assert
        Assert.Equal("conta_id", ex.NomeCampo);
        VerificarNadaPersistido();
    }

    [Fact]
    public async Task CriarAtivo_ContaInexistente_LancaContaNaoEncontradaException()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        _mockContaRepository.Setup(r => r.ObterPorId(contaId)).ReturnsAsync((Conta?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ContaNaoEncontradaException>(() =>
            _service.CriarAtivo("ETF", TipoAtivo.RendaVariavel, contaId, 10m, 100m, new DateOnly(2024, 2, 1)));
        VerificarNadaPersistido();
    }

    [Fact]
    public async Task CriarAtivo_ContaDoTipoBanco_LancaContaInvestimentoInvalidaParaAtivo()
    {
        // Arrange
        var conta = CriarContaBanco(Guid.NewGuid());
        ConfigurarConta(conta);

        // Act & Assert
        await Assert.ThrowsAsync<ContaInvestimentoInvalidaParaAtivoException>(() =>
            _service.CriarAtivo("ETF", TipoAtivo.RendaVariavel, conta.Id, 10m, 100m, new DateOnly(2024, 2, 1)));
        VerificarNadaPersistido();
    }

    [Fact]
    public async Task CriarAtivo_ContaComTipoNulo_LancaContaInvestimentoInvalidaParaAtivo()
    {
        // Arrange (borda: tipo nao definido nao e INVESTIMENTO)
        var conta = CriarContaInvestimento(Guid.NewGuid());
        conta.Tipo = null;
        ConfigurarConta(conta);

        // Act & Assert
        await Assert.ThrowsAsync<ContaInvestimentoInvalidaParaAtivoException>(() =>
            _service.CriarAtivo("ETF", TipoAtivo.RendaVariavel, conta.Id, 10m, 100m, new DateOnly(2024, 2, 1)));
        VerificarNadaPersistido();
    }

    [Fact]
    public async Task CriarAtivo_ContaInvestimentoInativa_LancaContaInvestimentoInvalidaParaAtivo()
    {
        // Arrange (borda: conta de investimento desativada)
        var conta = CriarContaInvestimento(Guid.NewGuid(), ativa: false);
        ConfigurarConta(conta);

        // Act & Assert
        await Assert.ThrowsAsync<ContaInvestimentoInvalidaParaAtivoException>(() =>
            _service.CriarAtivo("ETF", TipoAtivo.RendaVariavel, conta.Id, 10m, 100m, new DateOnly(2024, 2, 1)));
        VerificarNadaPersistido();
    }

    #endregion

    #region CriarAtivo com conta - caso feliz

    [Fact]
    public async Task CriarAtivo_ContaInvestimentoAtiva_VinculaContaIdESemInstituicao()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid());
        ConfigurarConta(conta);

        // Act
        var ativo = await _service.CriarAtivo("ETF", TipoAtivo.RendaVariavel, conta.Id, 10m, 100m, new DateOnly(2024, 2, 1));

        // Assert
        Assert.Equal(conta.Id, ativo.ContaId);
        Assert.Null(ativo.Instituicao);
    }

    [Fact]
    public async Task CriarAtivo_ContaInvestimentoAtiva_NasceComValorAtualIgualAoInvestidoEAporteInicial()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid());
        ConfigurarConta(conta);
        var dataCompra = new DateOnly(2024, 2, 1);

        // Act
        var ativo = await _service.CriarAtivo("ETF", TipoAtivo.RendaVariavel, conta.Id, 10m, 100m, dataCompra);

        // Assert
        Assert.Equal(1000m, ativo.ValorInvestido);
        Assert.Equal(1000m, ativo.ValorAtual);
        Assert.Equal(10m, ativo.Quantidade);
        _mockAporteRepository.Verify(r => r.Adicionar(It.Is<AtivoAporte>(a =>
            a.AtivoId == ativo.Id &&
            a.Quantidade == 10m &&
            a.PrecoUnitario == 100m &&
            a.Data == dataCompra)), Times.Once);
    }

    [Fact]
    public async Task CriarAtivo_ContaInvestimentoAtiva_PersisteAtivoEAporte()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid());
        ConfigurarConta(conta);

        // Act
        await _service.CriarAtivo("ETF", TipoAtivo.RendaVariavel, conta.Id, 10m, 100m, new DateOnly(2024, 2, 1));

        // Assert
        _mockAtivoRepository.Verify(r => r.Adicionar(It.IsAny<Ativo>()), Times.Once);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Once);
        _mockAporteRepository.Verify(r => r.Adicionar(It.IsAny<AtivoAporte>()), Times.Once);
        _mockAporteRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region VincularConta

    [Fact]
    public async Task VincularConta_AtivoSemConta_DefineContaEPreencheAtualizadoEm()
    {
        // Arrange
        var ativo = CriarAtivoExistente(contaId: null);
        var conta = CriarContaInvestimento(Guid.NewGuid());
        ConfigurarAtivo(ativo);
        ConfigurarConta(conta);

        // Act
        var resultado = await _service.VincularConta(ativo.Id, conta.Id);

        // Assert
        Assert.Equal(conta.Id, resultado.ContaId);
        Assert.NotNull(resultado.AtualizadoEm);
    }

    [Fact]
    public async Task VincularConta_TrocaDeContaDeInvestimento_SubstituiContaId()
    {
        // Arrange
        var contaAntiga = Guid.NewGuid();
        var ativo = CriarAtivoExistente(contaId: contaAntiga);
        var contaNova = CriarContaInvestimento(Guid.NewGuid());
        ConfigurarAtivo(ativo);
        ConfigurarConta(contaNova);

        // Act
        var resultado = await _service.VincularConta(ativo.Id, contaNova.Id);

        // Assert
        Assert.Equal(contaNova.Id, resultado.ContaId);
    }

    [Fact]
    public async Task VincularConta_MesmaConta_EIdempotenteEMantemValores()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid());
        var ativo = CriarAtivoExistente(contaId: conta.Id);
        ConfigurarAtivo(ativo);
        ConfigurarConta(conta);

        // Act
        var resultado = await _service.VincularConta(ativo.Id, conta.Id);

        // Assert
        Assert.Equal(conta.Id, resultado.ContaId);
        Assert.Equal(10m, resultado.Quantidade);
        Assert.Equal(1000m, resultado.ValorInvestido);
    }

    [Fact]
    public async Task VincularConta_NaoAlteraQuantidadeValorInvestidoNemRendimentosNemAportes()
    {
        // Arrange
        var ativo = CriarAtivoExistente(contaId: null);
        var conta = CriarContaInvestimento(Guid.NewGuid());
        ConfigurarAtivo(ativo);
        ConfigurarConta(conta);

        // Act
        var resultado = await _service.VincularConta(ativo.Id, conta.Id);

        // Assert
        Assert.Equal(10m, resultado.Quantidade);
        Assert.Equal(1000m, resultado.ValorInvestido);
        Assert.Equal(2, resultado.Rendimentos.Count);
        _mockAporteRepository.Verify(r => r.Adicionar(It.IsAny<AtivoAporte>()), Times.Never);
    }

    [Fact]
    public async Task VincularConta_AtivoInexistente_LancaAtivoNaoEncontradoException()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        _mockAtivoRepository.Setup(r => r.ObterPorId(ativoId)).ReturnsAsync((Ativo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<AtivoNaoEncontradoException>(() =>
            _service.VincularConta(ativoId, Guid.NewGuid()));
    }

    [Fact]
    public async Task VincularConta_AtivoInativo_LancaAtivoNaoEncontradoException()
    {
        // Arrange (borda: ativo desativado nao pode ser vinculado)
        var ativo = CriarAtivoExistente(contaId: null, ativa: false);
        var conta = CriarContaInvestimento(Guid.NewGuid());
        ConfigurarAtivo(ativo);
        ConfigurarConta(conta);

        // Act & Assert
        await Assert.ThrowsAsync<AtivoNaoEncontradoException>(() =>
            _service.VincularConta(ativo.Id, conta.Id));
        Assert.Null(ativo.ContaId);
    }

    [Fact]
    public async Task VincularConta_ContaInexistente_LancaContaNaoEncontradaEMantemContaAnterior()
    {
        // Arrange
        var contaAntiga = Guid.NewGuid();
        var ativo = CriarAtivoExistente(contaId: contaAntiga);
        var contaId = Guid.NewGuid();
        ConfigurarAtivo(ativo);
        _mockContaRepository.Setup(r => r.ObterPorId(contaId)).ReturnsAsync((Conta?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ContaNaoEncontradaException>(() =>
            _service.VincularConta(ativo.Id, contaId));
        Assert.Equal(contaAntiga, ativo.ContaId);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task VincularConta_ContaDoTipoBanco_LancaContaInvestimentoInvalidaParaAtivo()
    {
        // Arrange
        var ativo = CriarAtivoExistente(contaId: null);
        var conta = CriarContaBanco(Guid.NewGuid());
        ConfigurarAtivo(ativo);
        ConfigurarConta(conta);

        // Act & Assert
        await Assert.ThrowsAsync<ContaInvestimentoInvalidaParaAtivoException>(() =>
            _service.VincularConta(ativo.Id, conta.Id));
        Assert.Null(ativo.ContaId);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task VincularConta_ContaInvestimentoInativa_LancaContaInvestimentoInvalidaParaAtivo()
    {
        // Arrange
        var ativo = CriarAtivoExistente(contaId: null);
        var conta = CriarContaInvestimento(Guid.NewGuid(), ativa: false);
        ConfigurarAtivo(ativo);
        ConfigurarConta(conta);

        // Act & Assert
        await Assert.ThrowsAsync<ContaInvestimentoInvalidaParaAtivoException>(() =>
            _service.VincularConta(ativo.Id, conta.Id));
        Assert.Null(ativo.ContaId);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion
}
