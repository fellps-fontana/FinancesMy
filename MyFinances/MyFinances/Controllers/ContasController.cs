using MyFinances.DTOs;
using MyFinances.DTOs.Conta;
using MyFinances.Exceptions;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContasController : ControllerBase
{
    private readonly IContaService _contaService;
    private readonly SaldoCartaoService _saldoCartaoService;

    public ContasController(IContaService contaService, SaldoCartaoService saldoCartaoService)
    {
        _contaService = contaService;
        _saldoCartaoService = saldoCartaoService;
    }

    [HttpPost]
    public async Task<ActionResult<ContaResponse>> CriarConta([FromBody] CriarContaRequest request)
    {
        var (sucesso, conta, erro) = await _contaService.CriarContaAsync(request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var response = ContaResponse.FromConta(conta!);
        return Created($"/api/contas/{response.Id}", response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContaResponse>>> ListarContas([FromQuery] string? tipo)
    {
        IEnumerable<Models.Conta> contas;

        if (string.IsNullOrWhiteSpace(tipo))
        {
            contas = await _contaService.ListarContasInvestimento();
        }
        else if (tipo.Equals("investimento", StringComparison.OrdinalIgnoreCase))
        {
            contas = await _contaService.ListarContasInvestimento();
        }
        else
        {
            return BadRequest(new { erro = $"Tipo '{tipo}' nao e suportado. Use 'investimento'." });
        }

        var response = contas.Select(c => ContaResponse.FromConta(c));
        return Ok(response);
    }

    [HttpGet("{id}/saldo")]
    public async Task<ActionResult<SaldoCartaoResponse>> ObterSaldo(Guid id)
    {
        var (sucesso, saldo, erro) = await _saldoCartaoService.CalcularSaldoAsync(id);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var response = new SaldoCartaoResponse
        {
            ContaId = id,
            Saldo = saldo
        };

        return Ok(response);
    }

    [HttpPatch("{id}/saldo")]
    public async Task<IActionResult> AtualizarSaldo(Guid id, AtualizarSaldoRequest request)
    {
        try
        {
            await _contaService.AtualizarSaldoManual(id, request.NovoSaldo);
            return NoContent();
        }
        catch (ContaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (SaldoManualNaoPermitidoException ex)
        {
            return UnprocessableEntity(new { erro = ex.Message });
        }
    }

    [HttpPatch("{id}/desativar")]
    public async Task<IActionResult> DesativarConta(Guid id)
    {
        try
        {
            await _contaService.DesativarConta(id);
            return NoContent();
        }
        catch (ContaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpGet("investimentos/total")]
    public async Task<ActionResult<TotalInvestidoResponse>> ObterTotalInvestido()
    {
        var total = await _contaService.CalcularTotalInvestido();
        var response = new TotalInvestidoResponse { TotalInvestido = total };
        return Ok(response);
    }
}
