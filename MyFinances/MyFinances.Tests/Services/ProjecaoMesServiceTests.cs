using Moq;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class ProjecaoMesServiceTests
{
    private readonly Mock<IFluxoCaixaService> _fluxoCaixaServiceMock;
    private readonly Mock<IContaReceberService> _contaReceberServiceMock;
    private readonly Mock<IFaturaProjecaoService> _faturaProjecaoServiceMock;
    private readonly ProjecaoMesService _service;

    public ProjecaoMesServiceTests()
    {
        _fluxoCaixaServiceMock = new Mock<IFluxoCaixaService>();
        _contaReceberServiceMock = new Mock<IContaReceberService>();
        _faturaProjecaoServiceMock = new Mock<IFaturaProjecaoService>();
        _service = new ProjecaoMesService(
            _fluxoCaixaServiceMock.Object,
            _contaReceberServiceMock.Object,
            _faturaProjecaoServiceMock.Object);
    }

    [Fact]
    public async Task CalcularProjecaoDoMes_CompoeSaldoCorretamente_RetornaAnoMesBatendoComParametros()
    {
        // Arrange
        int ano = 2026;
        int mes = 7;

        decimal totalRecebidoNoMes = 5000m;
        decimal totalAReceberEsperadoNoMes = 1000m;
        decimal totalPagoFluxoCaixa = 2000m;
        decimal totalAPagarFluxoCaixa = 1500m;
        decimal totalPagoFatura = 500m;
        decimal totalNaoPagoFatura = 300m;

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalRecebidoNoMes(ano, mes))
            .ReturnsAsync(totalRecebidoNoMes);

        _contaReceberServiceMock
            .Setup(s => s.CalcularTotalAReceberEsperadoNoMes(ano, mes))
            .ReturnsAsync(totalAReceberEsperadoNoMes);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalPagoNoMes(ano, mes))
            .ReturnsAsync(totalPagoFluxoCaixa);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalAPagarNoMes(ano, mes))
            .ReturnsAsync(totalAPagarFluxoCaixa);

        _faturaProjecaoServiceMock
            .Setup(s => s.CalcularProjecaoCartaoDoMes(ano, mes))
            .ReturnsAsync(new FaturaProjecaoMes(totalPagoFatura, totalNaoPagoFatura));

        // Act
        var resultado = await _service.CalcularProjecaoDoMes(ano, mes);

        // Assert
        Assert.Equal(ano, resultado.Ano);
        Assert.Equal(mes, resultado.Mes);
        Assert.Equal(totalRecebidoNoMes, resultado.TotalRecebidoNoMes);
        Assert.Equal(totalAReceberEsperadoNoMes, resultado.TotalAReceberEsperadoNoMes);
        Assert.Equal(totalPagoFluxoCaixa + totalPagoFatura, resultado.TotalPagoNoMes);
        Assert.Equal(totalAPagarFluxoCaixa + totalNaoPagoFatura, resultado.TotalAPagarNoMes);
    }

    [Fact]
    public async Task CalcularProjecaoDoMes_AplicaFormulaCorretamente()
    {
        // Arrange
        int ano = 2026;
        int mes = 7;

        decimal totalRecebidoNoMes = 10000m;
        decimal totalAReceberEsperadoNoMes = 2000m;
        decimal totalPagoFluxoCaixa = 3000m;
        decimal totalAPagarFluxoCaixa = 1500m;
        decimal totalPagoFatura = 1000m;
        decimal totalNaoPagoFatura = 500m;

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalRecebidoNoMes(ano, mes))
            .ReturnsAsync(totalRecebidoNoMes);

        _contaReceberServiceMock
            .Setup(s => s.CalcularTotalAReceberEsperadoNoMes(ano, mes))
            .ReturnsAsync(totalAReceberEsperadoNoMes);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalPagoNoMes(ano, mes))
            .ReturnsAsync(totalPagoFluxoCaixa);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalAPagarNoMes(ano, mes))
            .ReturnsAsync(totalAPagarFluxoCaixa);

        _faturaProjecaoServiceMock
            .Setup(s => s.CalcularProjecaoCartaoDoMes(ano, mes))
            .ReturnsAsync(new FaturaProjecaoMes(totalPagoFatura, totalNaoPagoFatura));

        // Act
        var resultado = await _service.CalcularProjecaoDoMes(ano, mes);

        // Assert - Formula: saldo_projetado = (recebido + a_receber) - (pago + a_pagar)
        decimal totalPagoEsperado = totalPagoFluxoCaixa + totalPagoFatura; // 4000m
        decimal totalAPagarEsperado = totalAPagarFluxoCaixa + totalNaoPagoFatura; // 2000m
        decimal saldoProjetadoEsperado = (totalRecebidoNoMes + totalAReceberEsperadoNoMes) - (totalPagoEsperado + totalAPagarEsperado);
        // (10000 + 2000) - (4000 + 2000) = 12000 - 6000 = 6000

        Assert.Equal(saldoProjetadoEsperado, resultado.SaldoProjetado);
        Assert.Equal(6000m, resultado.SaldoProjetado);
    }

    [Fact]
    public async Task CalcularProjecaoDoMes_ComSaldoPositivo_RetornaSaldoProjetadoPositivo()
    {
        // Arrange
        int ano = 2026;
        int mes = 7;

        decimal totalRecebidoNoMes = 15000m;
        decimal totalAReceberEsperadoNoMes = 3000m;
        decimal totalPagoFluxoCaixa = 5000m;
        decimal totalAPagarFluxoCaixa = 2000m;
        decimal totalPagoFatura = 1000m;
        decimal totalNaoPagoFatura = 500m;

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalRecebidoNoMes(ano, mes))
            .ReturnsAsync(totalRecebidoNoMes);

        _contaReceberServiceMock
            .Setup(s => s.CalcularTotalAReceberEsperadoNoMes(ano, mes))
            .ReturnsAsync(totalAReceberEsperadoNoMes);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalPagoNoMes(ano, mes))
            .ReturnsAsync(totalPagoFluxoCaixa);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalAPagarNoMes(ano, mes))
            .ReturnsAsync(totalAPagarFluxoCaixa);

        _faturaProjecaoServiceMock
            .Setup(s => s.CalcularProjecaoCartaoDoMes(ano, mes))
            .ReturnsAsync(new FaturaProjecaoMes(totalPagoFatura, totalNaoPagoFatura));

        // Act
        var resultado = await _service.CalcularProjecaoDoMes(ano, mes);

        // Assert - (15000 + 3000) - (5000 + 2000 + 1000 + 500) = 18000 - 8500 = 9500
        Assert.True(resultado.SaldoProjetado > 0, "Saldo projetado deve ser positivo");
        Assert.Equal(9500m, resultado.SaldoProjetado);
    }

    [Fact]
    public async Task CalcularProjecaoDoMes_ComSaldoNegativo_RetornaSaldoProjetadoNegativo()
    {
        // Arrange
        int ano = 2026;
        int mes = 7;

        decimal totalRecebidoNoMes = 3000m;
        decimal totalAReceberEsperadoNoMes = 500m;
        decimal totalPagoFluxoCaixa = 2000m;
        decimal totalAPagarFluxoCaixa = 1500m;
        decimal totalPagoFatura = 800m;
        decimal totalNaoPagoFatura = 200m;

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalRecebidoNoMes(ano, mes))
            .ReturnsAsync(totalRecebidoNoMes);

        _contaReceberServiceMock
            .Setup(s => s.CalcularTotalAReceberEsperadoNoMes(ano, mes))
            .ReturnsAsync(totalAReceberEsperadoNoMes);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalPagoNoMes(ano, mes))
            .ReturnsAsync(totalPagoFluxoCaixa);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalAPagarNoMes(ano, mes))
            .ReturnsAsync(totalAPagarFluxoCaixa);

        _faturaProjecaoServiceMock
            .Setup(s => s.CalcularProjecaoCartaoDoMes(ano, mes))
            .ReturnsAsync(new FaturaProjecaoMes(totalPagoFatura, totalNaoPagoFatura));

        // Act
        var resultado = await _service.CalcularProjecaoDoMes(ano, mes);

        // Assert - (3000 + 500) - (2000 + 1500 + 800 + 200) = 3500 - 4500 = -1000
        Assert.True(resultado.SaldoProjetado < 0, "Saldo projetado deve ser negativo");
        Assert.Equal(-1000m, resultado.SaldoProjetado);
    }

    [Fact]
    public async Task CalcularProjecaoDoMes_ComTodosOsValoresZerados_RetornaSaldoProjetadoZero()
    {
        // Arrange
        int ano = 2026;
        int mes = 7;

        decimal totalRecebidoNoMes = 0m;
        decimal totalAReceberEsperadoNoMes = 0m;
        decimal totalPagoFluxoCaixa = 0m;
        decimal totalAPagarFluxoCaixa = 0m;
        decimal totalPagoFatura = 0m;
        decimal totalNaoPagoFatura = 0m;

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalRecebidoNoMes(ano, mes))
            .ReturnsAsync(totalRecebidoNoMes);

        _contaReceberServiceMock
            .Setup(s => s.CalcularTotalAReceberEsperadoNoMes(ano, mes))
            .ReturnsAsync(totalAReceberEsperadoNoMes);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalPagoNoMes(ano, mes))
            .ReturnsAsync(totalPagoFluxoCaixa);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalAPagarNoMes(ano, mes))
            .ReturnsAsync(totalAPagarFluxoCaixa);

        _faturaProjecaoServiceMock
            .Setup(s => s.CalcularProjecaoCartaoDoMes(ano, mes))
            .ReturnsAsync(new FaturaProjecaoMes(totalPagoFatura, totalNaoPagoFatura));

        // Act
        var resultado = await _service.CalcularProjecaoDoMes(ano, mes);

        // Assert - (0 + 0) - (0 + 0) = 0
        Assert.Equal(0m, resultado.SaldoProjetado);
    }

    [Fact]
    public async Task CalcularProjecaoDoMes_ChamamasTodosDependenciasCom_AnoMesCorretos()
    {
        // Arrange
        int ano = 2025;
        int mes = 12;

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalRecebidoNoMes(ano, mes))
            .ReturnsAsync(1000m);

        _contaReceberServiceMock
            .Setup(s => s.CalcularTotalAReceberEsperadoNoMes(ano, mes))
            .ReturnsAsync(100m);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalPagoNoMes(ano, mes))
            .ReturnsAsync(500m);

        _fluxoCaixaServiceMock
            .Setup(s => s.CalcularTotalAPagarNoMes(ano, mes))
            .ReturnsAsync(200m);

        _faturaProjecaoServiceMock
            .Setup(s => s.CalcularProjecaoCartaoDoMes(ano, mes))
            .ReturnsAsync(new FaturaProjecaoMes(100m, 50m));

        // Act
        await _service.CalcularProjecaoDoMes(ano, mes);

        // Assert - Verifica que todas as dependencias foram chamadas com ano e mes corretos
        _fluxoCaixaServiceMock.Verify(s => s.CalcularTotalRecebidoNoMes(ano, mes), Times.Once);
        _contaReceberServiceMock.Verify(s => s.CalcularTotalAReceberEsperadoNoMes(ano, mes), Times.Once);
        _fluxoCaixaServiceMock.Verify(s => s.CalcularTotalPagoNoMes(ano, mes), Times.Once);
        _fluxoCaixaServiceMock.Verify(s => s.CalcularTotalAPagarNoMes(ano, mes), Times.Once);
        _faturaProjecaoServiceMock.Verify(s => s.CalcularProjecaoCartaoDoMes(ano, mes), Times.Once);
    }
}
