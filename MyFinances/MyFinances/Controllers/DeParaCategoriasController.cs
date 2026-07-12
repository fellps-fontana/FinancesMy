using MyFinances.DTOs.DeParaCategoria;
using MyFinances.Exceptions;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeParaCategoriasController : ControllerBase
{
    private readonly IDeParaCategoriaService _deParaCategoriaService;

    public DeParaCategoriasController(IDeParaCategoriaService deParaCategoriaService)
    {
        _deParaCategoriaService = deParaCategoriaService;
    }

    [HttpPost]
    public async Task<ActionResult<DeParaCategoriaResponse>> Criar(CriarDeParaCategoriaRequest request)
    {
        try
        {
            var deParaCategoria = await _deParaCategoriaService.Criar(request.CategoriaPierre, request.CategoriaId);
            var response = DeParaCategoriaResponse.FromDeParaCategoria(deParaCategoria);
            return Created($"/api/de-para-categorias/{response.Id}", response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
        catch (CategoriaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeParaCategoriaResponse>>> Listar(
        [FromQuery] string? categoriaPierre = null)
    {
        var deParaCategorias = await _deParaCategoriaService.Listar(categoriaPierre);
        var response = deParaCategorias.Select(d => DeParaCategoriaResponse.FromDeParaCategoria(d));
        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DeParaCategoriaResponse>> Editar(Guid id, EditarDeParaCategoriaRequest request)
    {
        try
        {
            var deParaCategoria = await _deParaCategoriaService.Editar(id, request.CategoriaId);
            var response = DeParaCategoriaResponse.FromDeParaCategoria(deParaCategoria);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
        catch (CategoriaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (DeParaCategoriaNaoEncontradoException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        try
        {
            await _deParaCategoriaService.Excluir(id);
            return NoContent();
        }
        catch (DeParaCategoriaNaoEncontradoException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }
}
