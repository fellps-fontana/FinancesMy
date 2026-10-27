namespace MyFinances.Domain;

// Item 14. Percentual 0-100. Primeiro nivel: sobre a sobra. Subcategoria: sobre o sublimite do pai.
public class PlanoMensalSublimite
{
    public Guid Id { get; set; }

    public Guid PlanoMensalId { get; set; }

    public Guid CategoriaId { get; set; }

    public decimal Percentual { get; set; }

    public PlanoMensal? PlanoMensal { get; set; }

    public Categoria? Categoria { get; set; }
}
