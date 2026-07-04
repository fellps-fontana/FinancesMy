namespace MyFinances.Models;

public class Transferencia
{
    public Guid Id { get; set; }
    public required DateOnly Data { get; set; }
    public required decimal Valor { get; set; }
    public Guid ContaOrigemId { get; set; }
    public Guid ContaDestinoId { get; set; }
    public string? Descricao { get; set; }

    // Relacionamentos
    public required Conta ContaOrigem { get; set; }
    public required Conta ContaDestino { get; set; }
    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
    public Fatura? Fatura { get; set; }
}
