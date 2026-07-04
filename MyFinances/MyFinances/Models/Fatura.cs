namespace MyFinances.Models;

public class Fatura
{
    public Guid Id { get; set; }
    public Guid ContaId { get; set; }
    public required DateOnly DataFechamento { get; set; }
    public required DateOnly DataVencimento { get; set; }
    public required string Status { get; set; } // ABERTA | FECHADA | PAGA
    public Guid? TransferenciaId { get; set; } // pagamento vinculado; null enquanto nao paga

    // Relacionamentos
    public required Conta Conta { get; set; }
    public Transferencia? Transferencia { get; set; }
    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
}
