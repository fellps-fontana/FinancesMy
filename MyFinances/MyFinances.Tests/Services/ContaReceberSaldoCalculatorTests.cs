using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Services;

public class ContaReceberSaldoCalculatorTests
{
    #region Regra 1: Status PENDENTE quando nenhum lancamento CREDIT vinculado

    [Fact]
    public void Calcular_SemNenhumLancamento_RetornaStatusPendente()
    {
        // Arrange
        var contaReceber = new ContaReceber
        {
            Id = Guid.NewGuid(),
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Consulta",
            ValorTotal = 1000m,
            DataRegistro = new DateOnly(2026, 7, 1),
            DataPrevista = new DateOnly(2026, 7, 31),
            Status = StatusContaReceber.Pendente
        };

        // Act
        var saldo = ContaReceberSaldoCalculator.Calcular(contaReceber);

        // Assert
        Assert.Equal(StatusContaReceber.Pendente, saldo.Status);
        Assert.Equal(1000m, saldo.SaldoPendente);
        Assert.Equal(0m, saldo.ValorRecebido);
    }

    #endregion

    #region Regra 2: Status PARCIAL quando recebimento > 0 e < valor_total

    [Fact]
    public void Calcular_ComRecebimentoParcial_RetornaStatusParcial()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Consulta",
            ValorTotal = 1000m,
            DataRegistro = new DateOnly(2026, 7, 1),
            Status = StatusContaReceber.Pendente
        };

        // Simula lancamento CREDIT de 300 reais
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Tipo = TipoLancamento.Credit,
            Status = StatusLancamento.Pago,
            Valor = 300m,
            ContaReceberId = contaReceberId
        };

        contaReceber.Recebimentos.Add(lancamento);

        // Act
        var saldo = ContaReceberSaldoCalculator.Calcular(contaReceber);

        // Assert
        Assert.Equal(StatusContaReceber.Parcial, saldo.Status);
        Assert.Equal(700m, saldo.SaldoPendente);
        Assert.Equal(300m, saldo.ValorRecebido);
    }

    #endregion

    #region Regra 3: Status RECEBIDO quando saldo_pendente == 0

    [Fact]
    public void Calcular_ComRecebimentoCompleto_RetornaStatusRecebido()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Consulta",
            ValorTotal = 1000m,
            DataRegistro = new DateOnly(2026, 7, 1),
            Status = StatusContaReceber.Recebido
        };

        // Simula lancamento CREDIT de valor total
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Tipo = TipoLancamento.Credit,
            Status = StatusLancamento.Pago,
            Valor = 1000m,
            ContaReceberId = contaReceberId
        };

        contaReceber.Recebimentos.Add(lancamento);

        // Act
        var saldo = ContaReceberSaldoCalculator.Calcular(contaReceber);

        // Assert
        Assert.Equal(StatusContaReceber.Recebido, saldo.Status);
        Assert.Equal(0m, saldo.SaldoPendente);
        Assert.Equal(1000m, saldo.ValorRecebido);
    }

    #endregion

    #region Regra 4: ValorTotal nunca muda, sempre retorna o da entidade

    [Fact]
    public void Calcular_ValorTotalPermaneceSemAlteracao()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var valorTotalOriginal = 5000m;
        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Venda",
            ValorTotal = valorTotalOriginal,
            DataRegistro = new DateOnly(2026, 7, 1),
            Status = StatusContaReceber.Parcial
        };

        // Simula multiplos recebimentos
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 1500m,
                ContaReceberId = contaReceberId
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 2000m,
                ContaReceberId = contaReceberId
            }
        };

        contaReceber.Recebimentos = lancamentos;

        // Act
        var saldo = ContaReceberSaldoCalculator.Calcular(contaReceber);

        // Assert
        Assert.Equal(valorTotalOriginal, saldo.ValorTotal);
    }

    #endregion

    #region Regra 5: Lancamentos Debit ou com Status != Pago sao ignorados

    [Fact]
    public void Calcular_IgnoraLancamentosNaoCreditOuNaoPago()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Consulta",
            ValorTotal = 1000m,
            DataRegistro = new DateOnly(2026, 7, 1),
            Status = StatusContaReceber.Pendente
        };

        // Simula lancamentos que NAO devem contar:
        // - Debit (saida)
        // - Credit Pendente (nao pago)
        // Apenas Credit Pago deve contar
        var lancamentosIgnorados = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 200m,
                ContaReceberId = contaReceberId
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pendente,
                Valor = 300m,
                ContaReceberId = contaReceberId
            }
        };

        var lancamentoPago = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Tipo = TipoLancamento.Credit,
            Status = StatusLancamento.Pago,
            Valor = 500m,
            ContaReceberId = contaReceberId
        };

        contaReceber.Recebimentos = lancamentosIgnorados;
        contaReceber.Recebimentos.Add(lancamentoPago);

        // Act
        var saldo = ContaReceberSaldoCalculator.Calcular(contaReceber);

        // Assert - apenas o lancamento CREDIT + PAGO de 500 deve contar
        Assert.Equal(500m, saldo.ValorRecebido);
        Assert.Equal(500m, saldo.SaldoPendente);
        Assert.Equal(StatusContaReceber.Parcial, saldo.Status);
    }

    #endregion

    #region Regra 6: Multiplos recebimentos CREDIT somam corretamente

    [Fact]
    public void Calcular_MultiplosRecebimentos_SomamValorRecebido()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Prestacao Servicos",
            ValorTotal = 2000m,
            DataRegistro = new DateOnly(2026, 7, 1),
            Status = StatusContaReceber.Parcial
        };

        // Simula 3 recebimentos parciais
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 500m,
                ContaReceberId = contaReceberId
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 700m,
                ContaReceberId = contaReceberId
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 600m,
                ContaReceberId = contaReceberId
            }
        };

        contaReceber.Recebimentos = lancamentos;

        // Act
        var saldo = ContaReceberSaldoCalculator.Calcular(contaReceber);

        // Assert
        Assert.Equal(1800m, saldo.ValorRecebido);
        Assert.Equal(200m, saldo.SaldoPendente);
        Assert.Equal(StatusContaReceber.Parcial, saldo.Status);
    }

    #endregion
}
