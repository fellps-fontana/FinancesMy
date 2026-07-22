using MyFinances.Domain;

namespace MyFinances.DTOs;

public class FaturaResponse
{
    public Guid Id { get; set; }

    public Guid ContaId { get; set; }

    public DateOnly DataFechamento { get; set; }

    public DateOnly DataVencimento { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal ValorTotal { get; set; }

    public decimal ValorPago { get; set; }

    public decimal ValorPendente { get; set; }

    // FromFatura sobrecarregado: sem saldo ajustado (compatibilidade)
    public static FaturaResponse FromFatura(Fatura fatura)
    {
        var saldo = FaturaSaldoCalculator.Calcular(fatura);

        return new()
        {
            Id = fatura.Id,
            ContaId = fatura.ContaId,
            DataFechamento = fatura.DataFechamento,
            DataVencimento = fatura.DataVencimento,
            Status = fatura.Status.ToStorageValue(),
            ValorTotal = saldo.ValorTotal,
            ValorPago = saldo.ValorPago,
            ValorPendente = saldo.ValorPendente
        };
    }

    // FromFatura com saldo ajustado (novo, com credito de estorno retroativo)
    public static FaturaResponse FromFaturaComAjuste(Fatura fatura, FaturaSaldoAjustado saldoAjustado)
    {
        return new()
        {
            Id = fatura.Id,
            ContaId = fatura.ContaId,
            DataFechamento = fatura.DataFechamento,
            DataVencimento = fatura.DataVencimento,
            Status = fatura.Status.ToStorageValue(),
            ValorTotal = saldoAjustado.ValorTotal,
            ValorPago = saldoAjustado.ValorPago,
            ValorPendente = saldoAjustado.ValorPendenteAjustado
        };
    }
}
