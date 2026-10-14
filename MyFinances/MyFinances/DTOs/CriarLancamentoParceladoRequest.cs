namespace MyFinances.DTOs;

public class CriarLancamentoParceladoRequest
{
    public required string Descricao { get; set; }

    public required decimal ValorTotal { get; set; }

    public required int QuantidadeParcelas { get; set; }

    public required DateOnly DataPrimeiraParcela { get; set; }

    public Guid? CategoriaId { get; set; }

    // Status da PARCELA 1: PENDENTE | PAGO. Parcelas 2..N sempre PENDENTE.
    public required string Status { get; set; }
}
