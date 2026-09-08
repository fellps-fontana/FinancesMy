using Microsoft.AspNetCore.Mvc;
using MyFinances.DTOs.AssinaturaCartao;
using MyFinances.Exceptions;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
public class AssinaturaCartaoController : ControllerBase
{
    private readonly IAssinaturaCartaoService _service;

    public AssinaturaCartaoController(IAssinaturaCartaoService service)
    {
        _service = service;
    }

    [HttpPost("api/assinaturas-cartao")]
    public async Task<ActionResult<AssinaturaCartaoResponse>> Criar(CriarAssinaturaCartaoRequest request)
    {
        try
        {
            var assinatura = await _service.CriarAsync(
                request.ContaId,
                request.Descricao,
                request.Valor,
                request.DiaReferencia,
                request.CategoriaId);

            var response = AssinaturaCartaoResponse.FromDomain(assinatura);
            return Created($"/api/assinaturas-cartao/{response.Id}", response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPut("api/assinaturas-cartao/{id}")]
    public async Task<ActionResult<AssinaturaCartaoResponse>> Editar(Guid id, EditarAssinaturaCartaoRequest request)
    {
        try
        {
            var assinatura = await _service.EditarAsync(
                id,
                request.Descricao,
                request.Valor,
                request.DiaReferencia,
                request.CategoriaId);

            var response = AssinaturaCartaoResponse.FromDomain(assinatura);
            return Ok(response);
        }
        catch (AssinaturaCartaoNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("api/assinaturas-cartao/{id}/desativar")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        try
        {
            await _service.DesativarAsync(id);
            return NoContent();
        }
        catch (AssinaturaCartaoNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpPost("api/assinaturas-cartao/{id}/reativar")]
    public async Task<IActionResult> Reativar(Guid id)
    {
        try
        {
            await _service.ReativarAsync(id);
            return NoContent();
        }
        catch (AssinaturaCartaoNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpGet("api/assinaturas-cartao/{id}")]
    public async Task<ActionResult<AssinaturaCartaoResponse>> ObterPorId(Guid id)
    {
        try
        {
            var assinatura = await _service.ObterPorIdAsync(id);
            var response = AssinaturaCartaoResponse.FromDomain(assinatura);
            return Ok(response);
        }
        catch (AssinaturaCartaoNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpGet("api/assinaturas-cartao")]
    public async Task<ActionResult<IEnumerable<AssinaturaCartaoResponse>>> Listar(
        [FromQuery] Guid? contaId,
        [FromQuery] bool? ativa)
    {
        var assinaturas = await _service.ListarAsync(contaId, ativa);
        var response = assinaturas.Select(AssinaturaCartaoResponse.FromDomain);
        return Ok(response);
    }
}
