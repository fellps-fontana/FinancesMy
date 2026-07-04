using Microsoft.AspNetCore.Mvc;
using MyFinances.Dtos;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/cartoes/{contaId}/compras")]
public class CartaoComprasController : ControllerBase
{
    private readonly CompraCartaoService _compraCartaoService;

    public CartaoComprasController(CompraCartaoService compraCartaoService)
    {
        _compraCartaoService = compraCartaoService;
    }

    [HttpPost]
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

    [HttpPut("{id}")]
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
}
