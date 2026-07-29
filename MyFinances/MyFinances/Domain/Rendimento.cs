namespace MyFinances.Domain;

public class Rendimento
{
    public Guid Id { get; set; }

    public Guid AtivoId { get; set; }

    public TipoRendimento Tipo { get; set; }

    public OrigemRendimento Origem { get; set; }

    public decimal Valor { get; set; }

    public DateOnly Data { get; set; }

    public DateTime CriadoEm { get; set; }

    // Navegacao
    public Ativo? Ativo { get; set; }
}
