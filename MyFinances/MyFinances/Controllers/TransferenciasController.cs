using Microsoft.AspNetCore.Mvc;
using MyFinances.DTOs;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/transferencias")]
public class TransferenciasController : ControllerBase
{
    private readonly TransferenciaService _transferenciaService;

    public TransferenciasController(TransferenciaService transferenciaService)
    {
        _transferenciaService = transferenciaService;
    }

    [HttpPost]
    public async Task<IActionResult> CriarTransferencia(
        [FromBody] CriarTransferenciaRequest request)
    {
        var (sucesso, transferencia, erro) = await _transferenciaService.CriarAsync(request);

        if (!sucesso)
        {
            return BadRequest(erro);
        }

        return CreatedAtAction(nameof(CriarTransferencia), new { id = transferencia!.Id }, transferencia);
    }
}
