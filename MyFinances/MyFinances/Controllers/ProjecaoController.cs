using Microsoft.AspNetCore.Mvc;
using MyFinances.Dtos;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/cartoes/{contaId}/projecao")]
public class ProjecaoController : ControllerBase
{
    private readonly ProjecaoService _projecaoService;

    public ProjecaoController(ProjecaoService projecaoService)
    {
        _projecaoService = projecaoService;
    }

    [HttpGet]
    public async Task<ActionResult<ProjecaoCartaoResponseDto>> ObterProjecao(
        Guid contaId,
        [FromQuery] string mes)
    {
        if (string.IsNullOrWhiteSpace(mes))
        {
            return BadRequest(new { erro = "Parametro 'mes' e obrigatorio. Formato: YYYY-MM" });
        }

        if (!ParsearMes(mes, out var mesInt, out var ano))
        {
            return BadRequest(new { erro = "Formato de mes invalido. Use YYYY-MM" });
        }

        var (sucesso, projecao, erro) = await _projecaoService.ObterProjecaoCartaoAsync(contaId, mesInt, ano);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        return Ok(projecao);
    }

    private static bool ParsearMes(string mesString, out int mes, out int ano)
    {
        mes = 0;
        ano = 0;

        var partes = mesString.Split('-');
        if (partes.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(partes[0], out ano) || !int.TryParse(partes[1], out mes))
        {
            return false;
        }

        if (mes < 1 || mes > 12)
        {
            return false;
        }

        if (ano < 1900 || ano > 2100)
        {
            return false;
        }

        return true;
    }
}
