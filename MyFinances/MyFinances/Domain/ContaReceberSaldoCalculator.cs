namespace MyFinances.Domain;

public static class ContaReceberSaldoCalculator
{
    public static ContaReceberSaldo Calcular(ContaReceber contaReceber)
    {
        throw new NotImplementedException();
    }
}

public record ContaReceberSaldo(decimal ValorTotal, decimal ValorRecebido, decimal SaldoPendente, StatusContaReceber Status);
