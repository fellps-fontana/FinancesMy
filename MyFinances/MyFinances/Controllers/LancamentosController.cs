using Microsoft.AspNetCore.Mvc;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/lancamentos")]
public class LancamentosController : ControllerBase
{
    private readonly FluxoCaixaService _fluxoCaixaService;

    public LancamentosController(FluxoCaixaService fluxoCaixaService)
    {
        _fluxoCaixaService = fluxoCaixaService;
    }

    [HttpGet]
    public async Task<IActionResult> ObterLancamentosCaixa([FromQuery] Guid? contaId = null)
    {
        var lancamentos = await _fluxoCaixaService.ObterLancamentosCaixaAsync(contaId);
        return Ok(lancamentos);
    }
}
