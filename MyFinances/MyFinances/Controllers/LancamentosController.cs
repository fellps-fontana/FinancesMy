using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Exceptions;
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
        try
        {
            var (sucesso, lancamento, erro) = await _lancamentoManualService.EditarAsync(contaId, lancamentoId, request);

            if (!sucesso)
            {
                return BadRequest(new { erro });
            }

            var response = lancamento!;
            return Ok(response);
        }
        catch (LancamentoNaoAvulsoException ex)
        {
            return RespostaNaoAvulso(ex);
        }
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
        try
        {
            var (sucesso, erro) = await _lancamentoManualService.RemoverAsync(contaId, lancamentoId);

            if (!sucesso)
            {
                return BadRequest(new { erro });
            }

            return Ok();
        }
        catch (LancamentoNaoAvulsoException ex)
        {
            return RespostaNaoAvulso(ex);
        }
    }

    // Regra-de-negocio item 18: lancamento com vinculo e editado/excluido na origem.
    private ObjectResult RespostaNaoAvulso(LancamentoNaoAvulsoException ex)
    {
        return Conflict(new { erro = ex.Message, origemVinculo = ex.Origem.ToStorageValue() });
    }

    [HttpGet("fluxo-caixa")]
    public async Task<IActionResult> ListarFluxoCaixa(Guid contaId)
    {
        var lancamentos = await _fluxoCaixaService.ListarFluxoCaixa(contaId);
        return Ok(lancamentos);
    }

    [HttpGet("/api/lancamentos/fluxo-caixa")]
    public async Task<IActionResult> ListarFluxoCaixaTodasContas()
    {
        var itens = await _fluxoCaixaService.ListarFluxoCaixaTodasContas();
        return Ok(itens);
    }
}
