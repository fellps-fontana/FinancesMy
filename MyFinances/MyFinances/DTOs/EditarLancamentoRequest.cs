namespace MyFinances.DTOs;

public class EditarLancamentoRequest
{
    public required string Descricao { get; set; }

    public required decimal Valor { get; set; }

    // Item 18: ao trocar Tipo a UI envia null. Categoria de tipo incompativel
    // (DEBIT so DESPESA, CREDIT so RECEITA) -> 400.
    public Guid? CategoriaId { get; set; }

    public required string Tipo { get; set; }

    public required DateOnly Data { get; set; }

    public string? Status { get; set; }

    // Item 18: opcional. Null/omitido = mantem a conta. Diferente da atual =
    // move o lancamento (so conta MANUAL, ATIVA e TipoConta != Cartao).
    public Guid? ContaId { get; set; }
}
