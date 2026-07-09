using MyFinances.Models;

namespace MyFinances.Domain;

public static class FaturaSaldoCalculator
{
    public static FaturaSaldo Calcular(Fatura fatura)
    {
        var valorTotal = fatura.Lancamentos.Sum(l =>
            l.Tipo == TipoLancamento.Debit ? l.Valor : -l.Valor);
        var valorPago = fatura.Transferencias.Sum(t => t.Valor);
        var valorPendente = valorTotal - valorPago;
        return new FaturaSaldo(valorTotal, valorPago, valorPendente);
    }
}

public record FaturaSaldo(decimal ValorTotal, decimal ValorPago, decimal ValorPendente);
