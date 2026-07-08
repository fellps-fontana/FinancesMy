using Microsoft.AspNetCore.Mvc;
using MyFinances.DTOs;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/relatorios")]
public class RelatoriosController : ControllerBase
{
    private readonly RelatorioCategoriaService _relatorioCategoriaService;

    public RelatoriosController(RelatorioCategoriaService relatorioCategoriaService)
    {
        _relatorioCategoriaService = relatorioCategoriaService;
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> ObterGastoPorCategoria(
        [FromQuery] string mes,
        [FromQuery] Guid? contaId = null)
    {
        try
        {
            var partes = mes.Split('-');
            if (partes.Length != 2
                || !int.TryParse(partes[0], out var ano)
                || !int.TryParse(partes[1], out var mesInt))
            {
                return BadRequest(new { erro = "Parametro mes deve estar no formato YYYY-MM." });
            }

            var resultado = await _relatorioCategoriaService.ObterGastoPorCategoriaAsync(ano, mesInt, contaId);
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { erro = "Erro ao processar relatorio de categorias.", detalhes = ex.Message });
        }
    }
}
