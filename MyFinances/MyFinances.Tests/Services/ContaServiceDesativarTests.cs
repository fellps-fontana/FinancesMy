using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyFinances.Exceptions;
using MyFinances.Models;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class ContaServiceDesativarTests
{
    private readonly Mock<IContaRepository> _mockRepository;
    private readonly Mock<IAtivoRepository> _mockAtivoRepository;
    private readonly ContaService _service;

    public ContaServiceDesativarTests()
    {
        _mockRepository = new Mock<IContaRepository>();
        _mockAtivoRepository = new Mock<IAtivoRepository>();
        _mockAtivoRepository
            .Setup(r => r.VerificarContasComAtivos(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync((IEnumerable<Guid> contaIds) =>
                contaIds.ToDictionary(id => id, _ => false));
        _mockAtivoRepository
            .Setup(r => r.SomarValorAtivosPorConta(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, decimal>());
        _service = new ContaService(_mockRepository.Object, _mockAtivoRepository.Object, NullLogger<ContaService>.Instance);
    }

    [Fact]
    public async Task DesativarConta_InvestimentoComAtivos_LancaExcecao()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Carteira",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var ativos = new List<Ativo>
        {
            new() { Id = Guid.NewGuid(), ContaId = contaId, Ticker = "PETR4", Quantidade = 10, Ativa = true }
        };

        _mockRepository.Setup(r => r.ObterPorId(contaId)).ReturnsAsync(conta);
        _mockAtivoRepository.Setup(r => r.ListarAtivosAtivosPorConta(contaId)).ReturnsAsync(ativos);

        // Act & Assert
        await Assert.ThrowsAsync<ContaComAtivosNaoPodeSerDesativadaException>(
            () => _service.DesativarConta(contaId));

        Assert.True(conta.Ativa);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task DesativarConta_InvestimentoSemAtivos_Desativa()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cofrinho",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        _mockRepository.Setup(r => r.ObterPorId(contaId)).ReturnsAsync(conta);
        _mockAtivoRepository.Setup(r => r.ListarAtivosAtivosPorConta(contaId)).ReturnsAsync(new List<Ativo>());

        // Act
        await _service.DesativarConta(contaId);

        // Assert
        Assert.False(conta.Ativa);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task DesativarConta_OutroTipo_IgnoraAtivos()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var ativos = new List<Ativo>
        {
            new() { Id = Guid.NewGuid(), ContaId = contaId, Ticker = "VALE3", Quantidade = 5, Ativa = true }
        };

        _mockRepository.Setup(r => r.ObterPorId(contaId)).ReturnsAsync(conta);
        _mockAtivoRepository.Setup(r => r.ListarAtivosAtivosPorConta(contaId)).ReturnsAsync(ativos);

        // Act
        await _service.DesativarConta(contaId);

        // Assert
        Assert.False(conta.Ativa);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
        _mockAtivoRepository.Verify(r => r.ListarAtivosAtivosPorConta(It.IsAny<Guid>()), Times.Never);
    }
}
