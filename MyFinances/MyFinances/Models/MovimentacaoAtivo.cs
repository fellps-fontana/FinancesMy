namespace MyFinances.Models;

public class MovimentacaoAtivo
{
    public Guid Id { get; set; }

    public Guid AtivoId { get; set; }

    public TipoMovimentacaoAtivo Tipo { get; set; }

    public decimal Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    public DateOnly Data { get; set; }

    public string? Observacao { get; set; }
}
