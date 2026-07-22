using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Moq;
using Xunit;

namespace MyFinances.Tests.Services;

public class FaturaProjecaoServiceTests
{
    private readonly Mock<IFaturaRepository> _mockFaturaRepository;
    private readonly FaturaProjecaoService _service;

    public FaturaProjecaoServiceTests()
    {
        _mockFaturaRepository = new Mock<IFaturaRepository>();
        _service = new FaturaProjecaoService(_mockFaturaRepository.Object);
    }

    [Fact]
    public async Task CalcularProjecaoCartaoDoMes_ComFaturaPaga_SomaValorTotalEmTotalPago()
    {
        // Arrange
        int ano = 2026;
        int mes = 7;

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            DataFechamento = new DateOnly(2026, 7, 1),
            DataVencimento = new DateOnly(2026, 7, 15),
            Status = StatusFatura.Paga,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 100m, Tipo = TipoLancamento.Debit }
            },
            Transferencias = new List<Transferencia>
            {
                new Transferencia { Valor = 100m }
            }
        };

        _mockFaturaRepository
            .Setup(r => r.ListarFaturasCartaoPorVencimentoNoMes(ano, mes))
            .ReturnsAsync(new[] { fatura });

        // Act
        var resultado = await _service.CalcularProjecaoCartaoDoMes(ano, mes);

        // Assert
        Assert.Equal(100m, resultado.TotalPago);
        Assert.Equal(0m, resultado.TotalNaoPago);
    }

    [Fact]
    public async Task CalcularProjecaoCartaoDoMes_ComFaturaAbertaSemPagamento_SomaValorTotalEmTotalNaoPago()
    {
        // Arrange
        int ano = 2026;
        int mes = 7;

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            DataFechamento = new DateOnly(2026, 7, 1),
            DataVencimento = new DateOnly(2026, 7, 15),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 200m, Tipo = TipoLancamento.Debit }
            },
            Transferencias = new List<Transferencia>()
        };

        _mockFaturaRepository
            .Setup(r => r.ListarFaturasCartaoPorVencimentoNoMes(ano, mes))
            .ReturnsAsync(new[] { fatura });

        // Act
        var resultado = await _service.CalcularProjecaoCartaoDoMes(ano, mes);

        // Assert
        Assert.Equal(0m, resultado.TotalPago);
        Assert.Equal(200m, resultado.TotalNaoPago);
    }

    [Fact]
    public async Task CalcularProjecaoCartaoDoMes_ComFaturaFechadaSemPagamento_SomaValorTotalEmTotalNaoPago()
    {
        // Arrange
        int ano = 2026;
        int mes = 7;

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            DataFechamento = new DateOnly(2026, 7, 1),
            DataVencimento = new DateOnly(2026, 7, 15),
            Status = StatusFatura.Fechada,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 150m, Tipo = TipoLancamento.Debit }
            },
            Transferencias = new List<Transferencia>()
        };

        _mockFaturaRepository
            .Setup(r => r.ListarFaturasCartaoPorVencimentoNoMes(ano, mes))
            .ReturnsAsync(new[] { fatura });

        // Act
        var resultado = await _service.CalcularProjecaoCartaoDoMes(ano, mes);

        // Assert
        Assert.Equal(0m, resultado.TotalPago);
        Assert.Equal(150m, resultado.TotalNaoPago);
    }

    [Fact]
    public async Task CalcularProjecaoCartaoDoMes_ComFaturaAbertaComPagamentoParcial_FracionaEmTotalPagoETotalNaoPago()
    {
        // Arrange - Comportamento fracionado: fatura com R$1000, pagamento de R$300, saldo pendente de R$700
        int ano = 2026;
        int mes = 7;

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            DataFechamento = new DateOnly(2026, 7, 1),
            DataVencimento = new DateOnly(2026, 7, 15),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 1000m, Tipo = TipoLancamento.Debit }
            },
            Transferencias = new List<Transferencia>
            {
                new Transferencia { Valor = 300m }
            }
        };

        _mockFaturaRepository
            .Setup(r => r.ListarFaturasCartaoPorVencimentoNoMes(ano, mes))
            .ReturnsAsync(new[] { fatura });

        // Act
        var resultado = await _service.CalcularProjecaoCartaoDoMes(ano, mes);

        // Assert
        // ValorPago = 300, SaldoPendente = 1000 - 300 = 700
        Assert.Equal(300m, resultado.TotalPago);
        Assert.Equal(700m, resultado.TotalNaoPago);
    }

    [Fact]
    public async Task CalcularProjecaoCartaoDoMes_ComFaturaFechadaComPagamentoParcial_FracionaEmTotalPagoETotalNaoPago()
    {
        // Arrange - Mesmo comportamento fracionado para fatura Fechada
        int ano = 2026;
        int mes = 7;

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            DataFechamento = new DateOnly(2026, 7, 1),
            DataVencimento = new DateOnly(2026, 7, 15),
            Status = StatusFatura.Fechada,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 500m, Tipo = TipoLancamento.Debit }
            },
            Transferencias = new List<Transferencia>
            {
                new Transferencia { Valor = 200m }
            }
        };

        _mockFaturaRepository
            .Setup(r => r.ListarFaturasCartaoPorVencimentoNoMes(ano, mes))
            .ReturnsAsync(new[] { fatura });

        // Act
        var resultado = await _service.CalcularProjecaoCartaoDoMes(ano, mes);

        // Assert
        // ValorPago = 200, SaldoPendente = 500 - 200 = 300
        Assert.Equal(200m, resultado.TotalPago);
        Assert.Equal(300m, resultado.TotalNaoPago);
    }

    [Fact]
    public async Task CalcularProjecaoCartaoDoMes_ComMultiplasFaturasDeMultiplosCartoes_SomaTudoNosDoistotais()
    {
        // Arrange - 3 faturas de cartoes diferentes, todas no mesmo mes
        int ano = 2026;
        int mes = 7;

        var fatura1 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            DataVencimento = new DateOnly(2026, 7, 15),
            Status = StatusFatura.Paga,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 100m, Tipo = TipoLancamento.Debit }
            },
            Transferencias = new List<Transferencia>
            {
                new Transferencia { Valor = 100m }
            }
        };

        var fatura2 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            DataVencimento = new DateOnly(2026, 7, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 300m, Tipo = TipoLancamento.Debit }
            },
            Transferencias = new List<Transferencia>()
        };

        var fatura3 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            DataVencimento = new DateOnly(2026, 7, 10),
            Status = StatusFatura.Fechada,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 500m, Tipo = TipoLancamento.Debit }
            },
            Transferencias = new List<Transferencia>
            {
                new Transferencia { Valor = 150m }
            }
        };

        _mockFaturaRepository
            .Setup(r => r.ListarFaturasCartaoPorVencimentoNoMes(ano, mes))
            .ReturnsAsync(new[] { fatura1, fatura2, fatura3 });

        // Act
        var resultado = await _service.CalcularProjecaoCartaoDoMes(ano, mes);

        // Assert
        // TotalPago = 100 (fatura1 paga) + 150 (fatura3 parcial) = 250
        // TotalNaoPago = 300 (fatura2 aberta sem pag) + 350 (fatura3 saldo = 500-150) = 650
        Assert.Equal(250m, resultado.TotalPago);
        Assert.Equal(650m, resultado.TotalNaoPago);
    }

    [Fact]
    public async Task CalcularProjecaoCartaoDoMes_SemNenhumaFatura_RetornaZeroEmAmbostotais()
    {
        // Arrange
        int ano = 2026;
        int mes = 7;

        _mockFaturaRepository
            .Setup(r => r.ListarFaturasCartaoPorVencimentoNoMes(ano, mes))
            .ReturnsAsync(Enumerable.Empty<Fatura>());

        // Act
        var resultado = await _service.CalcularProjecaoCartaoDoMes(ano, mes);

        // Assert
        Assert.Equal(0m, resultado.TotalPago);
        Assert.Equal(0m, resultado.TotalNaoPago);
    }
}
