using Microsoft.AspNetCore.Mvc;
using MyFinances.DTOs;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/lancamentos")]
public class LancamentosController : ControllerBase
{
    private readonly FluxoCaixaService _fluxoCaixaService;
    private readonly LancamentoManualService _lancamentoManualService;

    public LancamentosController(
        FluxoCaixaService fluxoCaixaService,
        LancamentoManualService lancamentoManualService)
    {
        _fluxoCaixaService = fluxoCaixaService;
        _lancamentoManualService = lancamentoManualService;
    }

    [HttpGet]
    public async Task<IActionResult> ObterLancamentosCaixa(
        [FromQuery] Guid? contaId = null,
        [FromQuery] string? visao = null)
    {
        if (visao == "caixa")
        {
            var lancamentos = await _fluxoCaixaService.ObterLancamentosCaixaAsync(contaId);
            return Ok(lancamentos);
        }

        // Comportamento default: listagem crua filtrada por contaId e status opcionales
        if (!contaId.HasValue)
        {
            return BadRequest("contaId é obrigatório para listagem padrão");
        }

        var status = HttpContext.Request.Query["status"].ToString();
        var lancamentosManual = await _lancamentoManualService.ListarLancamentosAsync(
            contaId.Value,
            status);

        return Ok(lancamentosManual);
    }

    [HttpPost]
    public async Task<IActionResult> CriarLancamento(
        [FromQuery] Guid contaId,
        [FromBody] CriarLancamentoRequest request)
    {
        var (sucesso, lancamento, erro) = await _lancamentoManualService.CriarLancamentoAsync(
            contaId,
            request);

        if (!sucesso)
        {
            return BadRequest(erro);
        }

        return CreatedAtAction(nameof(ObterLancamentosCaixa), lancamento);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditarLancamento(
        [FromQuery] Guid contaId,
        [FromRoute] Guid id,
        [FromBody] EditarLancamentoRequest request)
    {
        var (sucesso, lancamento, erro) = await _lancamentoManualService.EditarLancamentoAsync(
            contaId,
            id,
            request);

        if (!sucesso)
        {
            return BadRequest(erro);
        }

        return Ok(lancamento);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> ExcluirLancamento(
        [FromQuery] Guid contaId,
        [FromRoute] Guid id)
    {
        var (sucesso, erro) = await _lancamentoManualService.ExcluirLancamentoAsync(
            contaId,
            id);

        if (!sucesso)
        {
            return BadRequest(erro);
        }

        return NoContent();
    }
}
