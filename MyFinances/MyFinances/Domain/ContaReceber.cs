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

    // regra-de-negocio.md item 15: null se conta_receber avulsa (item 13);
    // preenchido quando a linha foi materializada por um molde recorrente.
    public Guid? RecebivelRecorrenteId { get; set; }

    // Relacionamentos
    public Categoria? Categoria { get; set; }

    public RecebivelRecorrente? RecebivelRecorrente { get; set; }

    public ICollection<Lancamento> Recebimentos { get; set; } = new List<Lancamento>();
}
