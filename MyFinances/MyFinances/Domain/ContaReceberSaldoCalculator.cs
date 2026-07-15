namespace MyFinances.Domain;

public static class ContaReceberSaldoCalculator
{
    public static ContaReceberSaldo Calcular(ContaReceber contaReceber)
    {
        var valorRecebido = CalcularValorRecebido(contaReceber);
        var saldoPendente = contaReceber.ValorTotal - valorRecebido;
        var status = DeterminarStatus(valorRecebido, saldoPendente);

        return new ContaReceberSaldo(contaReceber.ValorTotal, valorRecebido, saldoPendente, status);
    }

    private static decimal CalcularValorRecebido(ContaReceber contaReceber)
    {
        return contaReceber.Recebimentos
            .Where(l => l.Tipo == TipoLancamento.Credit && l.Status == StatusLancamento.Pago)
            .Sum(l => l.Valor);
    }

    private static StatusContaReceber DeterminarStatus(decimal valorRecebido, decimal saldoPendente)
    {
        if (valorRecebido == 0)
            return StatusContaReceber.Pendente;

        if (saldoPendente <= 0)
            return StatusContaReceber.Recebido;

        return StatusContaReceber.Parcial;
    }
}

public record ContaReceberSaldo(decimal ValorTotal, decimal ValorRecebido, decimal SaldoPendente, StatusContaReceber Status);
