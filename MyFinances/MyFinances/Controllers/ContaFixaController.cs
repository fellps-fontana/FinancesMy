using MyFinances.DTOs.ContaFixa;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
public class ContaFixaController : ControllerBase
{
    private readonly IContaFixaService _contaFixaService;

    public ContaFixaController(IContaFixaService contaFixaService)
    {
        _contaFixaService = contaFixaService;
    }

    [HttpPost("api/contas-fixas")]
    public async Task<ActionResult<ContaFixaResponse>> Criar(CriarContaFixaRequest request)
    {
        var (sucesso, contaFixa, erro) = await _contaFixaService.CriarAsync(
            request.ContaId,
            request.Descricao,
            request.Valor,
            request.DiaVencimento,
            request.CategoriaId
        );

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var response = ContaFixaResponse.FromContaFixa(contaFixa!);
        return Created($"/api/contas-fixas/{response.Id}", response);
    }

    [HttpPut("api/contas-fixas/{id}")]
    public async Task<ActionResult<ContaFixaResponse>> Editar(Guid id, EditarContaFixaRequest request)
    {
        var (sucesso, contaFixa, erro) = await _contaFixaService.EditarAsync(
            id,
            request.Valor,
            request.DiaVencimento,
            request.CategoriaId
        );

        if (!sucesso)
        {
            if (erro?.Contains("nao encontrada") == true)
            {
                return NotFound(new { erro });
            }
            return BadRequest(new { erro });
        }

        var response = ContaFixaResponse.FromContaFixa(contaFixa!);
        return Ok(response);
    }

    [HttpPost("api/contas-fixas/{id}/desativar")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var (sucesso, erro) = await _contaFixaService.DesativarAsync(id);

        if (!sucesso)
        {
            return NotFound(new { erro });
        }

        return NoContent();
    }

    [HttpPost("api/contas-fixas/{id}/reativar")]
    public async Task<IActionResult> Reativar(Guid id)
    {
        var (sucesso, erro) = await _contaFixaService.ReativarAsync(id);

        if (!sucesso)
        {
            return NotFound(new { erro });
        }

        return NoContent();
    }

    [HttpGet("api/contas-fixas")]
    public async Task<ActionResult<IEnumerable<ContaFixaResponse>>> Listar([FromQuery] bool? ativa)
    {
        var (sucesso, contasFixas, _) = await _contaFixaService.Listar(ativa);

        var response = contasFixas!.Select(cf => ContaFixaResponse.FromContaFixa(cf));
        return Ok(response);
    }

    [HttpGet("api/contas-fixas/{id}")]
    public async Task<ActionResult<ContaFixaResponse>> ObterPorId(Guid id)
    {
        var (sucesso, contaFixa, erro) = await _contaFixaService.ObterPorId(id);

        if (!sucesso)
        {
            return NotFound(new { erro });
        }

        var response = ContaFixaResponse.FromContaFixa(contaFixa!);
        return Ok(response);
    }
}
