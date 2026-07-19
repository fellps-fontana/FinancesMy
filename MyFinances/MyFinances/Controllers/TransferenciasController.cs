using MyFinances.DTOs;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/transferencias")]
public class TransferenciasController : ControllerBase
{
    private readonly ITransferenciaService _transferenciaService;

    public TransferenciasController(ITransferenciaService transferenciaService)
    {
        _transferenciaService = transferenciaService;
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] CriarTransferenciaRequest request)
    {
        var (sucesso, transferencia, erro) = await _transferenciaService.RegistrarTransferenciaManualAsync(request);

        if (!sucesso)
        {
            if (erro?.Contains("nao encontrada", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new { erro });
            }
            return BadRequest(new { erro });
        }

        var response = TransferenciaResponse.FromTransferencia(transferencia!);
        return Created($"/api/transferencias/{response.Id}", response);
    }
}
