using MyFinances.Dtos.Conta;
using MyFinances.Exceptions;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContasController : ControllerBase
{
    private readonly IContaService _contaService;

    public ContasController(IContaService contaService)
    {
        _contaService = contaService;
    }

    [HttpPost]
    public async Task<ActionResult<ContaResponse>> CriarContaInvestimento(CriarContaInvestimentoRequest request)
    {
        var conta = await _contaService.CriarContaInvestimento(request.Nome, request.SaldoInicial);
        var response = ContaResponse.FromConta(conta);
        return Created($"/api/contas/{response.Id}", response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContaResponse>>> ListarContas([FromQuery] string? tipo)
    {
        if (tipo != null && tipo != "investimento")
        {
            return BadRequest(new { erro = "Tipo de conta invalido. Apenas 'investimento' e suportado nesta versao." });
        }

        var contas = await _contaService.ListarContasInvestimento();
        var response = contas.Select(c => ContaResponse.FromConta(c));
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
