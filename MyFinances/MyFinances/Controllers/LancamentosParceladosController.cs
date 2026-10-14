using Microsoft.AspNetCore.Mvc;
using MyFinances.DTOs;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/contas/{contaId}/lancamentos-parcelados")]
public class LancamentosParceladosController : ControllerBase
{
    private readonly ILancamentoParceladoService _lancamentoParceladoService;

    public LancamentosParceladosController(ILancamentoParceladoService lancamentoParceladoService)
    {
        _lancamentoParceladoService = lancamentoParceladoService;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(Guid contaId, [FromBody] CriarLancamentoParceladoRequest request)
    {
        var (sucesso, resposta, erro) = await _lancamentoParceladoService.CriarAsync(contaId, request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        return Created($"/api/contas/{contaId}/lancamentos-parcelados/{resposta!.CompraParceladaId}", resposta);
    }

    [HttpDelete("{compraParceladaId}")]
    public async Task<IActionResult> Cancelar(Guid contaId, Guid compraParceladaId)
    {
        var (sucesso, excluidas, mantidas, erro) = await _lancamentoParceladoService.CancelarAsync(contaId, compraParceladaId);

        if (!sucesso)
        {
            return ErroCancelamento(erro);
        }

        return Ok(new { excluidas, mantidas });
    }

    private IActionResult ErroCancelamento(string? erro)
    {
        return erro == LancamentoParceladoService.ErroCompraNaoEncontrada ? NotFound(new { erro }) : BadRequest(new { erro });
    }
}
