using Microsoft.AspNetCore.Mvc;
using MyFinances.DTOs;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/cartoes/{contaId}")]
public class CartaoComprasController : ControllerBase
{
    private readonly CompraCartaoService _compraCartaoService;
    private readonly EstornoCartaoService _estornoCartaoService;

    public CartaoComprasController(CompraCartaoService compraCartaoService, EstornoCartaoService estornoCartaoService)
    {
        _compraCartaoService = compraCartaoService;
        _estornoCartaoService = estornoCartaoService;
    }

    [HttpPost("compras")]
    public async Task<IActionResult> CriarCompra(
        Guid contaId,
        [FromBody] CriarCompraRequest request)
    {
        var (sucesso, compra, erro) = await _compraCartaoService.CriarCompraAsync(contaId, request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        return CreatedAtAction(nameof(CriarCompra), new { contaId, id = compra!.Id }, compra);
    }

    [HttpPut("compras/{id}")]
    public async Task<IActionResult> EditarCompra(
        Guid contaId,
        Guid id,
        [FromBody] EditarCompraRequest request)
    {
        var (sucesso, compra, erro) = await _compraCartaoService.EditarCompraAsync(contaId, id, request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        return Ok(compra);
    }

    [HttpPost("estornos")]
    public async Task<IActionResult> CriarEstorno(
        Guid contaId,
        [FromBody] CriarEstornoRequest request)
    {
        var (sucesso, estorno, erro) = await _estornoCartaoService.CriarEstornoAsync(contaId, request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        return CreatedAtAction(nameof(CriarEstorno), new { contaId, id = estorno!.Id }, estorno);
    }
}
