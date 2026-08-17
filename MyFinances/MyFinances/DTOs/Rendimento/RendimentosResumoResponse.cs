namespace MyFinances.DTOs.Rendimento;

public class RendimentosResumoResponse
{
    public decimal TotalDividendos { get; set; }

    public decimal TotalValorizacao { get; set; }

    public IEnumerable<RendimentoResponse> Historico { get; set; } = Enumerable.Empty<RendimentoResponse>();
}
