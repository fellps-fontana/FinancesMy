using MyFinances.Domain;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Services;

public class FaturaSaldoCalculatorTests
{
    [Fact]
    public void Calcular_ComApenasComprasDebit_RetornaSaldoCorreto()
    {
        // Arrange
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    Tipo = TipoLancamento.Debit,
                    Valor = 100m,
                    Descricao = "Compra 1"
                },
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    Tipo = TipoLancamento.Debit,
                    Valor = 50m,
                    Descricao = "Compra 2"
                }
            }
        };

        // Act
        var saldo = FaturaSaldoCalculator.Calcular(fatura);

        // Assert
        Assert.Equal(150m, saldo.ValorTotal);
        Assert.Equal(0m, saldo.ValorPago);
        Assert.Equal(150m, saldo.ValorPendente);
    }

    [Fact]
    public void Calcular_ComCompraDebitEEstornoCredit_RetornaSaldoZero()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    ContaId = contaId,
                    Tipo = TipoLancamento.Debit,
                    Valor = 100m,
                    Descricao = "Compra original"
                },
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    ContaId = contaId,
                    Tipo = TipoLancamento.Credit,
                    Valor = 100m,
                    Descricao = "Estorno"
                }
            }
        };

        // Act
        var saldo = FaturaSaldoCalculator.Calcular(fatura);

        // Assert
        Assert.Equal(0m, saldo.ValorTotal);
        Assert.Equal(0m, saldo.ValorPago);
        Assert.Equal(0m, saldo.ValorPendente);
    }

    [Fact]
    public void Calcular_ComComprasEEstornoParcial_RetornaSaldoCorreto()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    ContaId = contaId,
                    Tipo = TipoLancamento.Debit,
                    Valor = 300m,
                    Descricao = "Compra 1"
                },
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    ContaId = contaId,
                    Tipo = TipoLancamento.Debit,
                    Valor = 200m,
                    Descricao = "Compra 2"
                },
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    ContaId = contaId,
                    Tipo = TipoLancamento.Credit,
                    Valor = 100m,
                    Descricao = "Estorno parcial"
                }
            }
        };

        // Act
        var saldo = FaturaSaldoCalculator.Calcular(fatura);

        // Assert
        Assert.Equal(400m, saldo.ValorTotal);
        Assert.Equal(0m, saldo.ValorPago);
        Assert.Equal(400m, saldo.ValorPendente);
    }

    [Fact]
    public void Calcular_ComPagamentoTotal_RetornaSaldoPendentZero()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var faturaId = Guid.NewGuid();
        var fatura = new Fatura
        {
            Id = faturaId,
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    ContaId = contaId,
                    Tipo = TipoLancamento.Debit,
                    Valor = 500m,
                    Descricao = "Compra"
                }
            },
            Transferencias = new List<Transferencia>
            {
                new Transferencia
                {
                    Id = Guid.NewGuid(),
                    Valor = 500m,
                    FaturaId = faturaId,
                    Descricao = "Pagamento"
                }
            }
        };

        // Act
        var saldo = FaturaSaldoCalculator.Calcular(fatura);

        // Assert
        Assert.Equal(500m, saldo.ValorTotal);
        Assert.Equal(500m, saldo.ValorPago);
        Assert.Equal(0m, saldo.ValorPendente);
    }

    [Fact]
    public void Calcular_ComPagamentoParcial_RetornaSaldoPendenteMenor()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var faturaId = Guid.NewGuid();
        var fatura = new Fatura
        {
            Id = faturaId,
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    ContaId = contaId,
                    Tipo = TipoLancamento.Debit,
                    Valor = 1000m,
                    Descricao = "Compra"
                }
            },
            Transferencias = new List<Transferencia>
            {
                new Transferencia
                {
                    Id = Guid.NewGuid(),
                    Valor = 300m,
                    FaturaId = faturaId,
                    Descricao = "Pagamento parcial"
                }
            }
        };

        // Act
        var saldo = FaturaSaldoCalculator.Calcular(fatura);

        // Assert
        Assert.Equal(1000m, saldo.ValorTotal);
        Assert.Equal(300m, saldo.ValorPago);
        Assert.Equal(700m, saldo.ValorPendente);
    }

    [Fact]
    public void Calcular_ComMultiplosDebitsECredits_RegradeSinalAplicadaCorretamente()
    {
        // Arrange - 5 Debits (compras) + 2 Credits (estornos)
        var contaId = Guid.NewGuid();
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Id = Guid.NewGuid(), Tipo = TipoLancamento.Debit, Valor = 100m },
                new Lancamento { Id = Guid.NewGuid(), Tipo = TipoLancamento.Debit, Valor = 200m },
                new Lancamento { Id = Guid.NewGuid(), Tipo = TipoLancamento.Debit, Valor = 150m },
                new Lancamento { Id = Guid.NewGuid(), Tipo = TipoLancamento.Debit, Valor = 250m },
                new Lancamento { Id = Guid.NewGuid(), Tipo = TipoLancamento.Debit, Valor = 300m },
                new Lancamento { Id = Guid.NewGuid(), Tipo = TipoLancamento.Credit, Valor = 50m },
                new Lancamento { Id = Guid.NewGuid(), Tipo = TipoLancamento.Credit, Valor = 75m }
            }
        };

        // Act
        var saldo = FaturaSaldoCalculator.Calcular(fatura);

        // Assert - 100+200+150+250+300 = 1000, 1000 - (50+75) = 875
        Assert.Equal(875m, saldo.ValorTotal);
        Assert.Equal(0m, saldo.ValorPago);
        Assert.Equal(875m, saldo.ValorPendente);
    }
}
