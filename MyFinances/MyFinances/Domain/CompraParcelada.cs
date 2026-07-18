namespace MyFinances.Domain;

public class CompraParcelada
{
    public Guid Id { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal ValorTotal { get; set; }

    public int QuantidadeParcelas { get; set; }

    public DateOnly DataCompra { get; set; }

    // Relacionamentos
    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
}
