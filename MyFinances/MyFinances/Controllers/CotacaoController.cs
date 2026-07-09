using MyFinances.DTOs.Cotacao;
using MyFinances.Exceptions;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/ativos/cotacao")]
public class CotacaoController : ControllerBase
{
    private readonly ICotacaoExternaService _cotacaoExternaService;

    public CotacaoController(ICotacaoExternaService cotacaoExternaService)
    {
        _cotacaoExternaService = cotacaoExternaService;
    }

    [HttpGet("{ticker}/historico")]
    public async Task<ActionResult<CotacaoHistoricoResponse>> ObterHistoricoCotacao(
        string ticker,
        [FromQuery] string? range = "1mo")
    {
        try
        {
            var historico = await _cotacaoExternaService.ObterHistoricoCotacao(ticker, range ?? "1mo");
            return Ok(historico);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
        catch (TickerNaoEncontradoException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (CotacaoExternaIndisponibilException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { erro = ex.Message });
        }
    }
}
