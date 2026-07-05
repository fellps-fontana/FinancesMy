namespace MyFinances.Models;

public class Fatura
{
    public Guid Id { get; set; }
    public Guid ContaId { get; set; }
    public required DateOnly DataFechamento { get; set; }
    public required DateOnly DataVencimento { get; set; }
    public required string Status { get; set; } // ABERTA | FECHADA | PAGA

    // Relacionamentos
    public required Conta Conta { get; set; }
    public ICollection<Transferencia> Transferencias { get; set; } = new List<Transferencia>();
    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
}
