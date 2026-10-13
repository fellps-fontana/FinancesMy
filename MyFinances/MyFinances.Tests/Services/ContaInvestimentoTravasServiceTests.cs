using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using MyFinances.Tests.Helpers;
using Xunit;

namespace MyFinances.Tests.Services;

public class ContaInvestimentoTravasServiceTests
{
    private readonly Mock<IContaRepository> _mockContaRepository;
    private readonly FakeAtivoRepository _ativoRepository;
    private readonly ContaService _service;

    public ContaInvestimentoTravasServiceTests()
    {
        _mockContaRepository = new Mock<IContaRepository>();
        _ativoRepository = new FakeAtivoRepository();
        _service = new ContaService(_mockContaRepository.Object, _ativoRepository, NullLogger<ContaService>.Instance);
    }

    #region Helpers

    private static Conta CriarContaInvestimento(Guid id, decimal? saldoManual, bool ativa = true)
    {
        return new Conta
        {
            Id = id,
            Nome = "Conta investimento teste",
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Investimento,
            SaldoManual = saldoManual,
            Ativa = ativa
        };
    }

    private static Conta CriarContaBanco(Guid id, decimal? saldoManual)
    {
        return new Conta
        {
            Id = id,
            Nome = "Conta banco teste",
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Banco,
            SaldoManual = saldoManual,
            Ativa = true
        };
    }

    private async Task<Ativo> AdicionarAtivo(Guid? contaId, decimal valorAtual, bool ativa = true)
    {
        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Ativo teste",
            Tipo = TipoAtivo.RendaFixa,
            ContaId = contaId,
            ValorAtual = valorAtual,
            Ativa = ativa
        };

        await _ativoRepository.Adicionar(ativo);
        return ativo;
    }

    private void ConfigurarConta(Conta conta)
    {
        _mockContaRepository.Setup(r => r.ObterPorId(conta.Id)).ReturnsAsync(conta);
    }

    #endregion

    #region ObterSaldo

    [Fact]
    public async Task ObterSaldo_InvestimentoComDoisAtivosAtivos_RetornaSomaECalculado()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 999m);
        await AdicionarAtivo(conta.Id, 100m);
        await AdicionarAtivo(conta.Id, 50m);

        // Act
        var (saldo, calculado) = await _service.ObterSaldo(conta);

        // Assert
        Assert.Equal(150m, saldo);
        Assert.True(calculado);
    }

    [Fact]
    public async Task ObterSaldo_InvestimentoSemAtivo_RetornaSaldoManualNaoCalculado()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 300m);

        // Act
        var (saldo, calculado) = await _service.ObterSaldo(conta);

        // Assert
        Assert.Equal(300m, saldo);
        Assert.False(calculado);
    }

    [Fact]
    public async Task ObterSaldo_InvestimentoSemAtivoESaldoManualNulo_RetornaZeroNaoCalculado()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: null);

        // Act
        var (saldo, calculado) = await _service.ObterSaldo(conta);

        // Assert
        Assert.Equal(0m, saldo);
        Assert.False(calculado);
    }

    [Fact]
    public async Task ObterSaldo_InvestimentoSoComAtivoInativo_VoltaAoSaldoManualNaoCalculado()
    {
        // Arrange (borda: apos desativar o ultimo ativo, volta o ultimo saldo_manual)
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 300m);
        await AdicionarAtivo(conta.Id, 500m, ativa: false);

        // Act
        var (saldo, calculado) = await _service.ObterSaldo(conta);

        // Assert
        Assert.Equal(300m, saldo);
        Assert.False(calculado);
    }

    [Fact]
    public async Task ObterSaldo_InvestimentoComAtivoDeOutraConta_NaoUsaAtivoAlheio()
    {
        // Arrange (borda: ativo de outra conta nao entra no saldo desta)
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 300m);
        await AdicionarAtivo(Guid.NewGuid(), 500m);

        // Act
        var (saldo, calculado) = await _service.ObterSaldo(conta);

        // Assert
        Assert.Equal(300m, saldo);
        Assert.False(calculado);
    }

    [Fact]
    public async Task ObterSaldo_BancoComSaldoNegativo_PreservaNegativoNaoCalculado()
    {
        // Arrange
        var conta = CriarContaBanco(Guid.NewGuid(), saldoManual: -50m);

        // Act
        var (saldo, calculado) = await _service.ObterSaldo(conta);

        // Assert
        Assert.Equal(-50m, saldo);
        Assert.False(calculado);
    }

    #endregion

    #region DesativarConta

    [Fact]
    public async Task DesativarConta_ComAtivoAtivoVinculado_LancaContaPossuiAtivoAtivoException()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 0m);
        ConfigurarConta(conta);
        await AdicionarAtivo(conta.Id, 100m);

        // Act & Assert
        await Assert.ThrowsAsync<ContaPossuiAtivoAtivoException>(() => _service.DesativarConta(conta.Id));
        Assert.True(conta.Ativa);
        _mockContaRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task DesativarConta_SoComAtivosInativos_DesativaConta()
    {
        // Arrange (borda: so ativos inativos nao bloqueiam)
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 0m);
        ConfigurarConta(conta);
        await AdicionarAtivo(conta.Id, 100m, ativa: false);

        // Act
        await _service.DesativarConta(conta.Id);

        // Assert
        Assert.False(conta.Ativa);
        _mockContaRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task DesativarConta_SemAtivos_DesativaConta()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 0m);
        ConfigurarConta(conta);

        // Act
        await _service.DesativarConta(conta.Id);

        // Assert
        Assert.False(conta.Ativa);
        _mockContaRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region AtualizarSaldoManual

    [Fact]
    public async Task AtualizarSaldoManual_ComAtivoAtivoVinculado_LancaSaldoCalculadoNaoEditavelException()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 300m);
        ConfigurarConta(conta);
        await AdicionarAtivo(conta.Id, 100m);

        // Act & Assert
        await Assert.ThrowsAsync<SaldoCalculadoNaoEditavelException>(() =>
            _service.AtualizarSaldoManual(conta.Id, 450m));
        Assert.Equal(300m, conta.SaldoManual);
        _mockContaRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task AtualizarSaldoManual_SemAtivo_AtualizaSaldoManual()
    {
        // Arrange
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 300m);
        ConfigurarConta(conta);

        // Act
        await _service.AtualizarSaldoManual(conta.Id, 450m);

        // Assert
        Assert.Equal(450m, conta.SaldoManual);
        _mockContaRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task AtualizarSaldoManual_AposDesativarUltimoAtivo_AtualizaSaldoManual()
    {
        // Arrange (borda: ativo desativado libera a edicao do saldo)
        var conta = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 300m);
        ConfigurarConta(conta);
        var ativo = await AdicionarAtivo(conta.Id, 100m);
        ativo.Ativa = false;

        // Act
        await _service.AtualizarSaldoManual(conta.Id, 450m);

        // Assert
        Assert.Equal(450m, conta.SaldoManual);
        _mockContaRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region CalcularTotalInvestido

    [Fact]
    public async Task CalcularTotalInvestido_CenarioMisto_SomaContasEAtivosSemConta()
    {
        // Arrange
        // Conta C inativa (saldo 999) e ativo inativo vinculado a A (500) ficam fora do total
        var contaA = CriarContaInvestimento(Guid.NewGuid(), saldoManual: null);
        var contaB = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 30m);
        var contaInativa = CriarContaInvestimento(Guid.NewGuid(), saldoManual: 999m, ativa: false);
        _mockContaRepository
            .Setup(r => r.ListarPorTipo(TipoConta.Investimento))
            .ReturnsAsync(new List<Conta> { contaA, contaB, contaInativa });
        await AdicionarAtivo(contaA.Id, 100m);
        await AdicionarAtivo(contaA.Id, 50m);
        await AdicionarAtivo(contaA.Id, 500m, ativa: false);
        await AdicionarAtivo(null, 20m);

        // Act
        var total = await _service.CalcularTotalInvestido();

        // Assert
        Assert.Equal(200m, total);
    }

    #endregion
}
