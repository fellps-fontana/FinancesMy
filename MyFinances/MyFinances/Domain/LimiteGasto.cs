namespace MyFinances.Domain;

public class LimiteGasto
{
    public Guid Id { get; set; }

    public Guid CategoriaId { get; set; }

    public decimal ValorLimite { get; set; }

    public PeriodoLimiteGasto Periodo { get; set; } = PeriodoLimiteGasto.Mensal;

    public Categoria? Categoria { get; set; }
}
