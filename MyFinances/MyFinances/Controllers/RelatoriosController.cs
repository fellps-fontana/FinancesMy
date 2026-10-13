using Microsoft.AspNetCore.Mvc;
using MyFinances.DTOs.Relatorio;
using MyFinances.Exceptions;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
public class RelatoriosController : ControllerBase
{
    private readonly IRelatorioCategoriaService _relatorioCategoriaService;

    public RelatoriosController(IRelatorioCategoriaService relatorioCategoriaService)
    {
        _relatorioCategoriaService = relatorioCategoriaService;
    }

    [HttpGet("api/relatorios/gasto-por-categoria")]
    public async Task<ActionResult<RelatorioGastoPorCategoriaResponse>> ObterGastoPorCategoria(
        [FromQuery] int? ano, [FromQuery] int? mes)
    {
        try
        {
            var response = await _relatorioCategoriaService.ObterResumoAsync(
                ano ?? DateTime.Today.Year, mes ?? DateTime.Today.Month);
            return Ok(response);
        }
        catch (ValorInvalidoException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpGet("api/relatorios/gasto-por-categoria/detalhe")]
    public async Task<ActionResult<RelatorioCategoriaDetalheResponse>> ObterDetalheCategoria(
        [FromQuery] int? ano, [FromQuery] int? mes, [FromQuery] Guid? categoriaId)
    {
        try
        {
            var response = await _relatorioCategoriaService.ObterDetalheAsync(
                ano ?? DateTime.Today.Year, mes ?? DateTime.Today.Month, categoriaId);
            return Ok(response);
        }
        catch (CategoriaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (CategoriaNaoEhPrimeiroNivelException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
        catch (ValorInvalidoException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
