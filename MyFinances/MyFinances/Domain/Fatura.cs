namespace MyFinances.Domain;

public class Fatura
{
    public Guid Id { get; set; }

    public Guid ContaId { get; set; }

    public DateOnly DataFechamento { get; set; }

    public DateOnly DataVencimento { get; set; }

    public StatusFatura Status { get; set; }

    // Relacionamentos
    public Conta? Conta { get; set; }

    public ICollection<Transferencia> Transferencias { get; set; } = new List<Transferencia>();

    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
}
