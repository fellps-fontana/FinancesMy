using MyFinances.DTOs.Ativo;
using MyFinances.Exceptions;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
public class AtivosController : ControllerBase
{
    private readonly IAtivoService _ativoService;

    public AtivosController(IAtivoService ativoService)
    {
        _ativoService = ativoService;
    }

    [HttpGet("api/contas/{contaId}/ativos")]
    public async Task<ActionResult<IEnumerable<AtivoResponse>>> ListarAtivosPorConta(Guid contaId)
    {
        try
        {
            var ativos = await _ativoService.ListarAtivosPorConta(contaId);
            var response = ativos.Select(a => AtivoResponse.FromAtivo(a));
            return Ok(response);
        }
        catch (ContaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (ContaNaoEhInvestimentoException ex)
        {
            return UnprocessableEntity(new { erro = ex.Message });
        }
    }

    [HttpPost("api/contas/{contaId}/ativos/compras")]
    public async Task<ActionResult<AtivoResponse>> RegistrarCompra(Guid contaId, RegistrarCompraRequest request)
    {
        try
        {
            var ativo = await _ativoService.RegistrarCompra(
                contaId,
                request.Ticker,
                request.Quantidade,
                request.PrecoUnitario,
                request.Data,
                request.Nome
            );
            var response = AtivoResponse.FromAtivo(ativo);
            return Created($"/api/contas/{contaId}/ativos/{response.Id}", response);
        }
        catch (ContaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (ContaNaoEhInvestimentoException ex)
        {
            return UnprocessableEntity(new { erro = ex.Message });
        }
        catch (ValorInvalidoException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("api/contas/{contaId}/ativos/{ativoId}/vendas")]
    public async Task<ActionResult<AtivoResponse>> RegistrarVenda(Guid contaId, Guid ativoId, RegistrarVendaRequest request)
    {
        try
        {
            var ativo = await _ativoService.RegistrarVenda(
                contaId,
                ativoId,
                request.Quantidade,
                request.PrecoUnitario,
                request.Data,
                request.Observacao
            );
            var response = AtivoResponse.FromAtivo(ativo);
            return Ok(response);
        }
        catch (AtivoNaoEncontradoException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (QuantidadeVendaInvalidaException ex)
        {
            return UnprocessableEntity(new { erro = ex.Message });
        }
        catch (ValorInvalidoException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
