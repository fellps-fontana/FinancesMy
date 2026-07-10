using MyFinances.DTOs.Categoria;
using MyFinances.Exceptions;
using MyFinances.Models;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categoriaService;

    public CategoriasController(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaResponse>> Criar(CriarCategoriaRequest request)
    {
        try
        {
            var categoria = await _categoriaService.Criar(request.Nome, request.Tipo, request.ParentId);
            var response = CategoriaResponse.FromCategoria(categoria);
            return Created($"/api/categorias/{response.Id}", response);
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
    public async Task<ActionResult<IEnumerable<CategoriaResponse>>> Listar(
        [FromQuery] TipoCategoria? tipo = null,
        [FromQuery] bool? arquivada = null,
        [FromQuery] Guid? parentId = null)
    {
        var categorias = await _categoriaService.Listar(tipo, arquivada, parentId);
        var response = categorias.Select(c => CategoriaResponse.FromCategoria(c));
        return Ok(response);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CategoriaResponse>> Editar(Guid id, EditarCategoriaRequest request)
    {
        try
        {
            var categoria = await _categoriaService.Editar(id, request.Nome, request.ParentId);
            var response = CategoriaResponse.FromCategoria(categoria);
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
    }

    [HttpPatch("{id}/arquivar")]
    public async Task<IActionResult> Arquivar(Guid id)
    {
        try
        {
            await _categoriaService.Arquivar(id);
            return NoContent();
        }
        catch (CategoriaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }
}
