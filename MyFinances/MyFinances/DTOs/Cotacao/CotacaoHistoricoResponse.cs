namespace MyFinances.DTOs.Cotacao;

public class CotacaoHistoricoResponse
{
    public string Ticker { get; set; } = "";
    public List<PontoCotacaoResponse> Pontos { get; set; } = [];
}
