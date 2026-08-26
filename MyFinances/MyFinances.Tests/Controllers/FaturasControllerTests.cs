using Moq;
using MyFinances.Controllers;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Controllers;

public class FaturasControllerTests
{
    private readonly Mock<IFaturaRepository> _mockFaturaRepository;
    private readonly Mock<PagamentoFaturaService> _mockPagamentoFaturaService;
    private readonly Mock<EstornoCartaoService> _mockEstornoCartaoService;
    private readonly Mock<FaturaCreditoService> _mockFaturaCreditoService;
    private readonly Mock<IRecorrenciaGeradorService> _mockRecorrenciaGeradorService;
    private readonly FaturasController _controller;

    public FaturasControllerTests()
    {
        _mockFaturaRepository = new Mock<IFaturaRepository>();
        _mockPagamentoFaturaService = new Mock<PagamentoFaturaService>(
            new Mock<IFaturaRepository>().Object,
            new Mock<ILancamentoRepository>().Object);
        _mockEstornoCartaoService = new Mock<EstornoCartaoService>(
            new Mock<ILancamentoRepository>().Object);
        _mockFaturaCreditoService = new Mock<FaturaCreditoService>(
            new Mock<IFaturaRepository>().Object,
            new Mock<ILancamentoRepository>().Object);
        _mockRecorrenciaGeradorService = new Mock<IRecorrenciaGeradorService>();
        _controller = new FaturasController(
            _mockFaturaRepository.Object,
            _mockPagamentoFaturaService.Object,
            _mockEstornoCartaoService.Object,
            _mockFaturaCreditoService.Object,
            _mockRecorrenciaGeradorService.Object);
    }

    #region Regra 17 (y): ListarFaturas deve chamar GarantirOcorrenciaDoMesAsync para ContaFixa-cartao

    [Fact]
    public async Task ListarFaturas_DeveChamarGarantirOcorrenciaParaContaFixaCartao()
    {
        // Arrange - item 12 (fatura): FaturasController ListarFaturas deve garantir ocorrencias antes de listar
        var contaId = Guid.NewGuid();
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var ano = hoje.Year;
        var mes = hoje.Month;

        // Setup mocks
        _mockFaturaRepository
            .Setup(r => r.ListarPorConta(contaId))
            .ReturnsAsync(new List<Fatura>());

        _mockFaturaCreditoService
            .Setup(s => s.CalcularCadeiaDaContaAsync(contaId))
            .ReturnsAsync((IReadOnlyList<FaturaSaldoAjustado>)new List<FaturaSaldoAjustado>());

        _mockRecorrenciaGeradorService
            .Setup(s => s.GarantirOcorrenciasAtivasDoMesAsync(ano, mes))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.ListarFaturas(contaId);

        // Assert - deve chamar GarantirOcorrenciasAtivasDoMesAsync com ano/mes corretos
        _mockRecorrenciaGeradorService.Verify(
            s => s.GarantirOcorrenciasAtivasDoMesAsync(ano, mes),
            Times.Once,
            "FaturasController.ListarFaturas deve chamar GarantirOcorrenciasAtivasDoMesAsync antes de listar faturas");
    }

    #endregion
}
