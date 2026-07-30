using MyFinances.DTOs.ContaFixa;
using MyFinances.Domain;
using MyFinances.Exceptions;
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
        var periodicidade = ConverterPeriodicidade(request.Periodicidade);
        if (periodicidade == null && !string.IsNullOrEmpty(request.Periodicidade))
        {
            return BadRequest(new { erro = $"Periodicidade invalida: {request.Periodicidade}" });
        }

        var (sucesso, contaFixa, erro) = await _contaFixaService.CriarAsync(
            request.ContaId,
            request.Descricao,
            request.Valor,
            request.DiaVencimento,
            request.CategoriaId,
            periodicidade
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
        try
        {
            var periodicidade = ConverterPeriodicidade(request.Periodicidade);
            if (periodicidade == null && !string.IsNullOrEmpty(request.Periodicidade))
            {
                return BadRequest(new { erro = $"Periodicidade invalida: {request.Periodicidade}" });
            }

            var (sucesso, contaFixa, erro) = await _contaFixaService.EditarAsync(
                id,
                request.Valor,
                request.DiaVencimento,
                request.CategoriaId,
                periodicidade
            );

            if (!sucesso)
            {
                return BadRequest(new { erro });
            }

            var response = ContaFixaResponse.FromContaFixa(contaFixa!);
            return Ok(response);
        }
        catch (ContaFixaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
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
        var (sucesso, contasFixas, erro) = await _contaFixaService.Listar(ativa);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

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

    private static PeriodicidadeContaFixa? ConverterPeriodicidade(string? periodicidade)
    {
        if (string.IsNullOrEmpty(periodicidade))
        {
            return null;
        }

        try
        {
            return PeriodicidadeContaFixaExtensions.FromStorageValue(periodicidade.ToUpperInvariant());
        }
        catch
        {
            return null;
        }
    }
}
