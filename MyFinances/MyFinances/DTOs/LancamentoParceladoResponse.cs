namespace MyFinances.DTOs;

public class LancamentoParceladoResponse
{
    public Guid CompraParceladaId { get; set; }

    // Ordenadas por ParcelaNumero 1..N (exige lancamento.CompraParcelada setado
    // antes de FromLancamento para TotalParcelas).
    public IReadOnlyList<LancamentoResponseDto> Parcelas { get; set; } = Array.Empty<LancamentoResponseDto>();
}
