using Microsoft.AspNetCore.Mvc;
using MyFinances.Dtos;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/contas")]
public class ContasController : ControllerBase
{
    private readonly ContaService _contaService;
    private readonly SaldoCartaoService _saldoCartaoService;

    public ContasController(ContaService contaService, SaldoCartaoService saldoCartaoService)
    {
        _contaService = contaService;
        _saldoCartaoService = saldoCartaoService;
    }

    [HttpPost]
    public async Task<IActionResult> CriarConta([FromBody] CriarContaRequest request)
    {
        var (sucesso, conta, erro) = await _contaService.CriarContaAsync(request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        return CreatedAtAction(nameof(CriarConta), new { id = conta!.Id }, conta);
    }

    [HttpGet("{id}/saldo")]
    public async Task<ActionResult<SaldoCartaoResponseDto>> ObterSaldo(Guid id)
    {
        var (sucesso, saldo, erro) = await _saldoCartaoService.CalcularSaldoAsync(id);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var dto = new SaldoCartaoResponseDto
        {
            ContaId = id,
            Saldo = saldo
        };

        return Ok(dto);
    }
}
