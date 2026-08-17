namespace MyFinances.DTOs;

public class FluxoCaixaItemDto
{
    public string TipoItem { get; set; } = string.Empty; // "LANCAMENTO" | "TRANSFERENCIA"

    public DateOnly Data { get; set; }

    public LancamentoResponseDto? Lancamento { get; set; }

    public TransferenciaResponse? Transferencia { get; set; }
}
