using MyFinances.DTOs.LimiteGasto;
using MyFinances.Exceptions;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
public class LimitesGastoController : ControllerBase
{
    private readonly ILimiteGastoService _limiteGastoService;

    public LimitesGastoController(ILimiteGastoService limiteGastoService)
    {
        _limiteGastoService = limiteGastoService;
    }

    [HttpPost("api/limites-gasto")]
    public async Task<ActionResult<LimiteGastoResponse>> DefinirLimite(DefinirLimiteGastoRequest request)
    {
        try
        {
            var (limiteGasto, criado) = await _limiteGastoService.Definir(request.CategoriaId, request.ValorLimite);
            var response = LimiteGastoResponse.FromLimiteGasto(limiteGasto);

            if (criado)
                return Created($"/api/limites-gasto/{limiteGasto.CategoriaId}", response);

            return Ok(response);
        }
        catch (CategoriaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (CategoriaInvalidaParaLimiteGastoException ex)
        {
            return UnprocessableEntity(new { erro = ex.Message });
        }
        catch (ValorInvalidoException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpDelete("api/limites-gasto/{categoriaId}")]
    public async Task<ActionResult> RemoverLimite(Guid categoriaId)
    {
        try
        {
            await _limiteGastoService.Remover(categoriaId);
            return NoContent();
        }
        catch (LimiteGastoNaoEncontradoException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpGet("api/limites-gasto")]
    public async Task<ActionResult<IEnumerable<LimiteGastoResponse>>> ListarLimites()
    {
        var limitesGasto = await _limiteGastoService.Listar();
        var response = limitesGasto.Select(l => LimiteGastoResponse.FromLimiteGasto(l));
        return Ok(response);
    }

    [HttpGet("api/limites-gasto/gasto-vs-limite/{categoriaId}")]
    public async Task<ActionResult<GastoVsLimiteResponse>> ObterGastoVsLimitePorCategoria(Guid categoriaId, [FromQuery] int ano, [FromQuery] int mes)
    {
        try
        {
            var (limite, status) = await _limiteGastoService.ObterGastoVsLimite(categoriaId, ano, mes);
            var response = GastoVsLimiteResponse.FromLimiteEStatus(limite, status);
            return Ok(response);
        }
        catch (LimiteGastoNaoEncontradoException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpGet("api/limites-gasto/gasto-vs-limite")]
    public async Task<ActionResult<IEnumerable<GastoVsLimiteResponse>>> ObterGastoVsLimiteTodasCategorias([FromQuery] int ano, [FromQuery] int mes)
    {
        var resultados = await _limiteGastoService.ObterGastoVsLimiteTodasCategorias(ano, mes);
        var response = resultados.Select(r => GastoVsLimiteResponse.FromLimiteEStatus(r.LimiteGasto, r.Status));
        return Ok(response);
    }
}
