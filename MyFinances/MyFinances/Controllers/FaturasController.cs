using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Repositories;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/contas/{contaId}/faturas")]
public class FaturasController : ControllerBase
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly PagamentoFaturaService _pagamentoFaturaService;
    private readonly EstornoCartaoService _estornoCartaoService;
    private readonly FaturaCreditoService _faturaCreditoService;
    private readonly IRecorrenciaGeradorService _recorrenciaGeradorService;
    private readonly IContaFixaRepository _contaFixaRepository;
    private readonly IAssinaturaCartaoService? _assinaturaCartaoService;
    private readonly FaturaCicloService _faturaCicloService;

    public FaturasController(
        IFaturaRepository faturaRepository,
        PagamentoFaturaService pagamentoFaturaService,
        EstornoCartaoService estornoCartaoService,
        FaturaCreditoService faturaCreditoService,
        IRecorrenciaGeradorService recorrenciaGeradorService,
        IContaFixaRepository contaFixaRepository,
        FaturaCicloService faturaCicloService,
        IAssinaturaCartaoService? assinaturaCartaoService = null)
    {
        _faturaRepository = faturaRepository;
        _pagamentoFaturaService = pagamentoFaturaService;
        _estornoCartaoService = estornoCartaoService;
        _faturaCreditoService = faturaCreditoService;
        _recorrenciaGeradorService = recorrenciaGeradorService;
        _contaFixaRepository = contaFixaRepository;
        _assinaturaCartaoService = assinaturaCartaoService;
        _faturaCicloService = faturaCicloService;
    }

    [HttpGet]
    public async Task<IActionResult> ListarFaturas(Guid contaId)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        var contasFixas = await _contaFixaRepository.ListarPorConta(contaId);
        foreach (var contaFixa in contasFixas.Where(cf => cf.Conta?.Tipo == TipoConta.Cartao && cf.Ativa))
        {
            await _recorrenciaGeradorService.GarantirOcorrenciaDoMesAsync(contaFixa.Id, hoje.Year, hoje.Month);
        }

        if (_assinaturaCartaoService != null)
        {
            await _assinaturaCartaoService.GarantirAssinaturasDoCicloAsync(contaId, hoje.Year, hoje.Month);
        }

        // Fechamento pelo calendario acontece na leitura (item 12, reclassificacao preguicosa).
        await _faturaCicloService.ReconciliarAsync(contaId);

        var faturas = await _faturaRepository.ListarPorConta(contaId);
        var cadeia = await _faturaCreditoService.CalcularCadeiaDaContaAsync(contaId);

        var response = faturas.Select(f =>
        {
            var ajuste = cadeia.FirstOrDefault(a => a.FaturaId == f.Id);
            return ajuste != null
                ? FaturaResponse.FromFaturaComAjuste(f, ajuste)
                : FaturaResponse.FromFatura(f);
        });

        return Ok(response);
    }

    [HttpGet("{faturaId}")]
    public async Task<IActionResult> ObterFatura(Guid contaId, Guid faturaId)
    {
        await _faturaCicloService.ReconciliarAsync(contaId);

        var fatura = await _faturaRepository.ObterPorId(faturaId);

        if (fatura == null || fatura.ContaId != contaId)
        {
            return NotFound(new { erro = "Fatura nao encontrada" });
        }

        var ajuste = await _faturaCreditoService.ObterSaldoAjustadoAsync(contaId, faturaId);
        var response = ajuste != null
            ? FaturaResponse.FromFaturaComAjuste(fatura, ajuste)
            : FaturaResponse.FromFatura(fatura);

        return Ok(response);
    }

    [HttpGet("{faturaId}/compras")]
    public async Task<IActionResult> ListarComprasDaFatura(
        Guid contaId,
        Guid faturaId,
        [FromServices] IFaturaComprasService faturaComprasService)
    {
        var compras = await faturaComprasService.ListarComprasAsync(contaId, faturaId);

        if (compras == null)
        {
            return NotFound(new { erro = "Fatura nao encontrada" });
        }

        return Ok(compras.Select(CompraResponse.FromLancamento));
    }

    [HttpPost("{faturaId}/pagamentos")]
    public async Task<IActionResult> PagarFatura(
        Guid contaId,
        Guid faturaId,
        [FromBody] PagarFaturaRequest request)
    {
        var (sucesso, pagamento, erro) = await _pagamentoFaturaService.PagarFaturaAsync(faturaId, request);

        if (!sucesso)
        {
            return BadRequest(new { erro });
        }

        var response = PagamentoResponse.FromTransferencia(pagamento!);
        return Created($"/api/contas/{contaId}/faturas/{faturaId}/pagamentos/{response.Id}", response);
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

        var response = EstornoResponse.FromLancamento(estorno!);
        return Created($"/api/contas/{contaId}/estornos/{response.Id}", response);
    }

}
