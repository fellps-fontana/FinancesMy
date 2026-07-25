using MyFinances.DTOs;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/contas/{contaId}/compras-parceladas")]
public class CartaoComprasParceladasController : ControllerBase
{
    private readonly ComprasParceladasService _comprasParceladasService;
    private readonly EstornoCompraParceladaService _estornoCompraParceladaService;

    public CartaoComprasParceladasController(
        ComprasParceladasService comprasParceladasService,
        EstornoCompraParceladaService estornoCompraParceladaService)
    {
        _comprasParceladasService = comprasParceladasService;
        _estornoCompraParceladaService = estornoCompraParceladaService;
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

    [HttpPost("{compraParceladaId}/estornos")]
    public async Task<IActionResult> EstornarCompraParcelada(
        Guid contaId,
        Guid compraParceladaId,
        [FromBody] EstornarCompraParceladaRequest request)
    {
        var (sucesso, canceladas, estornos, erro) = await _estornoCompraParceladaService.EstornarCompraParceladaAsync(
            contaId,
            compraParceladaId,
            request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var response = EstornoCompraParceladaResponse.FromDomain(canceladas!, estornos!);
        return Ok(response);
    }
}
