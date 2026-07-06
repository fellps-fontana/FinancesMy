using Microsoft.AspNetCore.Mvc;
using MyFinances.DTOs;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/contas")]
public class ContasController : ControllerBase
{
    private readonly ContaService _contaService;

    public ContasController(ContaService contaService)
    {
        _contaService = contaService;
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
}
