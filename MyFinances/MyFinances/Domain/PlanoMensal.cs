namespace MyFinances.Domain;

// Item 20. Um plano por mes calendario: (Ano, Mes) unico.
public class PlanoMensal
{
    public Guid Id { get; set; }

    public int Ano { get; set; }

    public int Mes { get; set; }

    public decimal RendaPrevista { get; set; }

    public decimal PercentualInvestimento { get; set; }

    public ICollection<PlanoMensalSublimite> Sublimites { get; set; } = new List<PlanoMensalSublimite>();
}
