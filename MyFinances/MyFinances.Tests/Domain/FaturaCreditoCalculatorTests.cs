using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

// Testes para FaturaCreditoCalculator.CalcularCadeia (regra critica de
// encadeamento de credito, item 12 subsecao "Estorno de compra parcelada").
// Calculadora pura -- sem repository, sem banco, sem estado externo.
public class FaturaCreditoCalculatorTests
{
    [Fact]
    public void CalcularCadeia_SemEstorno_CreditoZeroETodosOsAjustadosIguaisAoBruto()
    {
        // Arrange: duas faturas, nenhum estorno, nenhum credito pendente
        var contaId = Guid.NewGuid();
        var fatura1 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Fechada,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 100m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        var fatura2 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 2, 10),
            DataVencimento = new DateOnly(2025, 2, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 200m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        var faturas = new[] { fatura1, fatura2 }.AsReadOnly();

        // Act
        var resultado = FaturaCreditoCalculator.CalcularCadeia(faturas);

        // Assert: CreditoRecebido = 0 para ambas, ValorPendenteAjustado == ValorPendenteBruto
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Count);

        var ajustada1 = resultado[0];
        Assert.Equal(fatura1.Id, ajustada1.FaturaId);
        Assert.Equal(0m, ajustada1.CreditoRecebido);
        Assert.Equal(ajustada1.ValorPendenteBruto, ajustada1.ValorPendenteAjustado);

        var ajustada2 = resultado[1];
        Assert.Equal(fatura2.Id, ajustada2.FaturaId);
        Assert.Equal(0m, ajustada2.CreditoRecebido);
        Assert.Equal(ajustada2.ValorPendenteBruto, ajustada2.ValorPendenteAjustado);
    }

    [Fact]
    public void CalcularCadeia_ComEstornoEmPrimeiraFaturaCriandoCredito_CreditoFlui()
    {
        // Arrange: primeira fatura com valor pendente negativo (credito),
        // segunda fatura com saldo positivo que o credito vai ser abatido
        var contaId = Guid.NewGuid();

        // Fatura 1: R$100 em débito, sem pagamento, fica com -R$50 de crédito
        // (tipo: valor bruto 100, pago 0, restante 100, mas com lancamento de estorno
        // o ValorPendenteBruto ficaria -50)
        var fatura1 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Paga,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 100m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() },
                new Lancamento { Valor = 150m, Tipo = TipoLancamento.Credit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        // Fatura 2: R$200 em débito, sem pagamento, pendente 200
        var fatura2 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 2, 10),
            DataVencimento = new DateOnly(2025, 2, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 200m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        var faturas = new[] { fatura1, fatura2 }.AsReadOnly();

        // Act
        var resultado = FaturaCreditoCalculator.CalcularCadeia(faturas);

        // Assert
        Assert.Equal(2, resultado.Count);

        var ajustada1 = resultado[0];
        Assert.Equal(fatura1.Id, ajustada1.FaturaId);
        // ValorPendenteBruto = 100 - 150 = -50 (credito)
        Assert.Equal(-50m, ajustada1.ValorPendenteBruto);
        Assert.Equal(0m, ajustada1.CreditoRecebido);
        // ValorPendenteAjustado = -50 - 0 = -50
        Assert.Equal(-50m, ajustada1.ValorPendenteAjustado);

        var ajustada2 = resultado[1];
        Assert.Equal(fatura2.Id, ajustada2.FaturaId);
        // CreditoRecebido = 50 (vindo de fatura1)
        Assert.Equal(50m, ajustada2.CreditoRecebido);
        Assert.Equal(200m, ajustada2.ValorPendenteBruto);
        // ValorPendenteAjustado = 200 - 50 = 150
        Assert.Equal(150m, ajustada2.ValorPendenteAjustado);
    }

    [Fact]
    public void CalcularCadeia_ComCreditoQueSobra_PropagaParaProximaFatura()
    {
        // Arrange: primeira fatura com credito de 100, segunda fatura com
        // saldo de 50 (credito sobra 50 para proxima)
        var contaId = Guid.NewGuid();

        var fatura1 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Paga,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 50m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() },
                new Lancamento { Valor = 150m, Tipo = TipoLancamento.Credit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        var fatura2 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 2, 10),
            DataVencimento = new DateOnly(2025, 2, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 50m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        var fatura3 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 3, 10),
            DataVencimento = new DateOnly(2025, 3, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 100m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        var faturas = new[] { fatura1, fatura2, fatura3 }.AsReadOnly();

        // Act
        var resultado = FaturaCreditoCalculator.CalcularCadeia(faturas);

        // Assert
        Assert.Equal(3, resultado.Count);

        // Fatura 1: -100 de credito
        var ajustada1 = resultado[0];
        Assert.Equal(-100m, ajustada1.ValorPendenteAjustado);

        // Fatura 2: recebe 100 de credito, tem 50 de saldo, fica 0 de ajustado (50 - 100)
        var ajustada2 = resultado[1];
        Assert.Equal(100m, ajustada2.CreditoRecebido);
        Assert.Equal(50m, ajustada2.ValorPendenteBruto);
        Assert.Equal(-50m, ajustada2.ValorPendenteAjustado); // 50 - 100 = -50

        // Fatura 3: recebe 50 de credito (sobra de fatura 2), tem 100 de saldo
        var ajustada3 = resultado[2];
        Assert.Equal(50m, ajustada3.CreditoRecebido);
        Assert.Equal(100m, ajustada3.ValorPendenteBruto);
        Assert.Equal(50m, ajustada3.ValorPendenteAjustado); // 100 - 50 = 50
    }

    [Fact]
    public void CalcularCadeia_ComCredito_NaoPropagarQuandoAjustadoFicarPositivo()
    {
        // Arrange: primeira fatura com credito, segunda fatura com saldo
        // maior que o credito (ajustado fica positivo, nao propaga)
        var contaId = Guid.NewGuid();

        var fatura1 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Paga,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 100m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() },
                new Lancamento { Valor = 150m, Tipo = TipoLancamento.Credit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        var fatura2 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 2, 10),
            DataVencimento = new DateOnly(2025, 2, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 200m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        var fatura3 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 3, 10),
            DataVencimento = new DateOnly(2025, 3, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 100m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        var faturas = new[] { fatura1, fatura2, fatura3 }.AsReadOnly();

        // Act
        var resultado = FaturaCreditoCalculator.CalcularCadeia(faturas);

        // Assert
        Assert.Equal(3, resultado.Count);

        // Fatura 1: -50 de credito
        var ajustada1 = resultado[0];
        Assert.Equal(-50m, ajustada1.ValorPendenteAjustado);

        // Fatura 2: recebe 50, tem 200 de saldo, fica 150 positivo (NAO propaga)
        var ajustada2 = resultado[1];
        Assert.Equal(50m, ajustada2.CreditoRecebido);
        Assert.Equal(150m, ajustada2.ValorPendenteAjustado); // 200 - 50 = 150

        // Fatura 3: NAO recebe credito (porque fatura2 nao propagou)
        var ajustada3 = resultado[2];
        Assert.Equal(0m, ajustada3.CreditoRecebido);
        Assert.Equal(100m, ajustada3.ValorPendenteAjustado); // 100 - 0 = 100
    }

    [Fact]
    public void CalcularCadeia_ListaVazia_RetornaListaVazia()
    {
        // Arrange
        var faturas = new List<Fatura>().AsReadOnly();

        // Act
        var resultado = FaturaCreditoCalculator.CalcularCadeia(faturas);

        // Assert
        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }

    [Fact]
    public void CalcularCadeia_UmaFaturaPaga_MarcaCreditoRecebidoZero()
    {
        // Arrange: uma unica fatura, paga
        var contaId = Guid.NewGuid();
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Paga,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento { Valor = 100m, Tipo = TipoLancamento.Debit, Status = StatusLancamento.Pago, FaturaId = Guid.NewGuid() }
            }
        };

        var faturas = new[] { fatura }.AsReadOnly();

        // Act
        var resultado = FaturaCreditoCalculator.CalcularCadeia(faturas);

        // Assert
        Assert.Single(resultado);
        var ajustada = resultado[0];
        Assert.Equal(fatura.Id, ajustada.FaturaId);
        Assert.Equal(0m, ajustada.CreditoRecebido);
    }
}
