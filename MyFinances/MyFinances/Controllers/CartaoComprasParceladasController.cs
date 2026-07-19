using MyFinances.DTOs;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/contas/{contaId}/compras-parceladas")]
public class CartaoComprasParceladasController : ControllerBase
{
    private readonly ComprasParceladasService _comprasParceladasService;

    public CartaoComprasParceladasController(ComprasParceladasService comprasParceladasService)
    {
        _comprasParceladasService = comprasParceladasService;
    }

    [HttpPost]
    public async Task<IActionResult> CriarCompraParcelada(Guid contaId, [FromBody] CriarCompraParceladaRequest request)
    {
        var (sucesso, compraParcelada, erro) = await _comprasParceladasService.CriarCompraParceladaAsync(contaId, request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var response = CompraParceladaResponse.FromDomain(compraParcelada!, contaId);
        return Created($"/api/contas/{contaId}/compras-parceladas/{response.Id}", response);
    }
}
