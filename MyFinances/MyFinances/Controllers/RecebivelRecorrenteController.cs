using Microsoft.AspNetCore.Mvc;
using MyFinances.DTOs.RecebivelRecorrente;
using MyFinances.Exceptions;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
public class RecebivelRecorrenteController : ControllerBase
{
    private readonly IRecebivelRecorrenteService _service;

    public RecebivelRecorrenteController(IRecebivelRecorrenteService service)
    {
        _service = service;
    }

    [HttpPost("api/recebiveis-recorrentes")]
    public async Task<ActionResult<RecebivelRecorrenteResponse>> Criar(CriarRecebivelRecorrenteRequest request)
    {
        try
        {
            var molde = await _service.CriarAsync(
                request.Descricao,
                request.Valor,
                request.Periodicidade,
                request.DiaVencimento,
                request.MesReferencia,
                request.DiaDaSemana,
                request.CategoriaId);

            var response = RecebivelRecorrenteResponse.FromRecebivelRecorrente(molde);
            return Created($"/api/recebiveis-recorrentes/{response.Id}", response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPut("api/recebiveis-recorrentes/{id}")]
    public async Task<ActionResult<RecebivelRecorrenteResponse>> Editar(Guid id, EditarRecebivelRecorrenteRequest request)
    {
        try
        {
            var molde = await _service.EditarAsync(
                id,
                request.Valor,
                request.Periodicidade,
                request.DiaVencimento,
                request.MesReferencia,
                request.DiaDaSemana,
                request.CategoriaId);

            var response = RecebivelRecorrenteResponse.FromRecebivelRecorrente(molde);
            return Ok(response);
        }
        catch (RecebivelRecorrenteNaoEncontradoException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("api/recebiveis-recorrentes/{id}/desativar")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        try
        {
            await _service.DesativarAsync(id);
            return NoContent();
        }
        catch (RecebivelRecorrenteNaoEncontradoException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpPost("api/recebiveis-recorrentes/{id}/reativar")]
    public async Task<IActionResult> Reativar(Guid id)
    {
        try
        {
            await _service.ReativarAsync(id);
            return NoContent();
        }
        catch (RecebivelRecorrenteNaoEncontradoException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpGet("api/recebiveis-recorrentes/{id}")]
    public async Task<ActionResult<RecebivelRecorrenteResponse>> ObterPorId(Guid id)
    {
        try
        {
            var molde = await _service.ObterPorId(id);
            var response = RecebivelRecorrenteResponse.FromRecebivelRecorrente(molde);
            return Ok(response);
        }
        catch (RecebivelRecorrenteNaoEncontradoException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpGet("api/recebiveis-recorrentes")]
    public async Task<ActionResult<IEnumerable<RecebivelRecorrenteResponse>>> Listar([FromQuery] bool? ativa)
    {
        var moldes = await _service.Listar(ativa);
        var response = moldes.Select(m => RecebivelRecorrenteResponse.FromRecebivelRecorrente(m));
        return Ok(response);
    }
}
