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
    private readonly IAntecipacaoParcelasService _antecipacaoParcelasService;

    public CartaoComprasParceladasController(
        ComprasParceladasService comprasParceladasService,
        EstornoCompraParceladaService estornoCompraParceladaService,
        IAntecipacaoParcelasService antecipacaoParcelasService)
    {
        _antecipacaoParcelasService = antecipacaoParcelasService;
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

    [HttpPost("{compraParceladaId}/antecipacoes")]
    public async Task<IActionResult> AnteciparParcelas(
        Guid contaId,
        Guid compraParceladaId,
        [FromBody] AnteciparParcelasRequest request)
    {
        var (sucesso, faturaDestino, parcelas, erro) = await _antecipacaoParcelasService.AnteciparAsync(
            contaId,
            compraParceladaId,
            request.Quantidade,
            DateOnly.FromDateTime(DateTime.Today));

        if (!sucesso)
        {
            return ErroAntecipacao(erro);
        }

        var response = AnteciparParcelasResponse.FromDomain(compraParceladaId, faturaDestino!, parcelas!);
        return Ok(response);
    }

    [HttpGet("{compraParceladaId}/antecipacoes/elegiveis")]
    public async Task<IActionResult> ObterParcelasElegiveis(
        Guid contaId,
        Guid compraParceladaId)
    {
        var (sucesso, compra, parcelas, erro) = await _antecipacaoParcelasService.ObterElegiveisAsync(contaId, compraParceladaId);

        if (!sucesso)
        {
            return ErroAntecipacao(erro);
        }

        var totalParcelas = compra!.QuantidadeParcelas;
        var response = parcelas!
            .Select(parcela => ParcelaElegivelResponse.FromLancamento(parcela, totalParcelas))
            .ToList();
        return Ok(response);
    }

    private IActionResult ErroAntecipacao(string? erro)
    {
        return erro == AntecipacaoParcelasService.ErroCompraNaoEncontrada ? NotFound(new { erro }) : BadRequest(new { erro });
    }
}
