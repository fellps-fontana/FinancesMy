namespace MyFinances.Domain;

public class Transferencia
{
    public Guid Id { get; set; }

    public DateOnly Data { get; set; }

    public decimal Valor { get; set; }

    public Guid ContaOrigemId { get; set; }

    public Guid ContaDestinoId { get; set; }

    public Guid? FaturaId { get; set; }

    public string? Descricao { get; set; }

    // Relacionamentos
    public Conta? ContaOrigem { get; set; }

    public Conta? ContaDestino { get; set; }

    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();

    public Fatura? Fatura { get; set; }
}
