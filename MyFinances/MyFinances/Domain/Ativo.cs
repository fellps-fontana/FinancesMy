namespace MyFinances.Domain;

public class Ativo
{
    public Guid Id { get; set; }

    public Guid ContaId { get; set; }

    public string Ticker { get; set; } = string.Empty;

    public string? Nome { get; set; }

    public decimal Quantidade { get; set; }

    public decimal PrecoMedio { get; set; }

    public decimal PrecoAtual { get; set; }

    public bool Ativa { get; set; } = true;

    public DateTime CriadoEm { get; set; }
}
