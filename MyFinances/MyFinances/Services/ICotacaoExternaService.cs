using MyFinances.DTOs.Cotacao;

namespace MyFinances.Services;

public interface ICotacaoExternaService
{
    Task<CotacaoHistoricoResponse> ObterHistoricoCotacao(string ticker, string range = "1mo");
}
