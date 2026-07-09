using MyFinances.DTOs;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/contas/{contaId}/compras")]
public class CartaoComprasController : ControllerBase
{
    private readonly CompraCartaoService _compraCartaoService;

    public CartaoComprasController(CompraCartaoService compraCartaoService)
    {
        _compraCartaoService = compraCartaoService;
    }

    [HttpPost]
    public async Task<IActionResult> CriarCompra(Guid contaId, [FromBody] CriarCompraRequest request)
    {
        var (sucesso, compra, erro) = await _compraCartaoService.CriarCompraAsync(contaId, request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var response = CompraResponse.FromLancamento(compra!);
        return Created($"/api/contas/{contaId}/compras/{response.Id}", response);
    }

    [HttpPut("{compraId}")]
    public async Task<IActionResult> EditarCompra(
        Guid contaId,
        Guid compraId,
        [FromBody] EditarCompraRequest request)
    {
        var (sucesso, compra, erro) = await _compraCartaoService.EditarCompraAsync(contaId, compraId, request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var response = CompraResponse.FromLancamento(compra!);
        return Ok(response);
    }
}
