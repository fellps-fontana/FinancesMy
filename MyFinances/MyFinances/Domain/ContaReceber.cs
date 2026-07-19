namespace MyFinances.Domain;

public class ContaReceber
{
    public Guid Id { get; set; }

    public TipoContaReceber Tipo { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public string? Pessoa { get; set; }

    public decimal ValorTotal { get; set; }

    public DateOnly DataRegistro { get; set; }

    public DateOnly? DataPrevista { get; set; }

    public Guid? CategoriaId { get; set; }

    public StatusContaReceber Status { get; set; }

    // Relacionamentos
    public Categoria? Categoria { get; set; }

    public ICollection<Lancamento> Recebimentos { get; set; } = new List<Lancamento>();
}
