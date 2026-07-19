using MyFinances.DTOs;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/contas/{contaId}/lancamentos")]
public class LancamentosController : ControllerBase
{
    private readonly ILancamentoManualService _lancamentoManualService;
    private readonly IFluxoCaixaService _fluxoCaixaService;

    public LancamentosController(
        ILancamentoManualService lancamentoManualService,
        IFluxoCaixaService fluxoCaixaService)
    {
        _lancamentoManualService = lancamentoManualService;
        _fluxoCaixaService = fluxoCaixaService;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(Guid contaId, [FromBody] CriarLancamentoRequest request)
    {
        var (sucesso, lancamento, erro) = await _lancamentoManualService.CriarAsync(contaId, request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var response = lancamento!;
        return Created($"/api/contas/{contaId}/lancamentos/{response.Id}", response);
    }

    [HttpPut("{lancamentoId}")]
    public async Task<IActionResult> Editar(
        Guid contaId,
        Guid lancamentoId,
        [FromBody] EditarLancamentoRequest request)
    {
        var (sucesso, lancamento, erro) = await _lancamentoManualService.EditarAsync(contaId, lancamentoId, request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var response = lancamento!;
        return Ok(response);
    }

    [HttpPost("{lancamentoId}/pagamentos")]
    public async Task<IActionResult> MarcarComoPago(Guid contaId, Guid lancamentoId)
    {
        var (sucesso, erro) = await _lancamentoManualService.MarcarComoPagoAsync(contaId, lancamentoId);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        return Ok();
    }

    [HttpDelete("{lancamentoId}")]
    public async Task<IActionResult> Remover(Guid contaId, Guid lancamentoId)
    {
        var (sucesso, erro) = await _lancamentoManualService.RemoverAsync(contaId, lancamentoId);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        return Ok();
    }

    [HttpGet("fluxo-caixa")]
    public async Task<IActionResult> ListarFluxoCaixa(Guid contaId)
    {
        var lancamentos = await _fluxoCaixaService.ListarFluxoCaixa(contaId);
        return Ok(lancamentos);
    }
}
