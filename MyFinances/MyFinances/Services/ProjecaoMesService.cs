namespace MyFinances.Services;

public class ProjecaoMesService : IProjecaoMesService
{
    private readonly IFluxoCaixaService _fluxoCaixaService;
    private readonly IContaReceberService _contaReceberService;
    private readonly IFaturaProjecaoService _faturaProjecaoService;

    public ProjecaoMesService(
        IFluxoCaixaService fluxoCaixaService,
        IContaReceberService contaReceberService,
        IFaturaProjecaoService faturaProjecaoService)
    {
        _fluxoCaixaService = fluxoCaixaService;
        _contaReceberService = contaReceberService;
        _faturaProjecaoService = faturaProjecaoService;
    }

    public async Task<ProjecaoMesResultado> CalcularProjecaoDoMes(int ano, int mes)
    {
        var totalRecebidoNoMes = await _fluxoCaixaService.CalcularTotalRecebidoNoMes(ano, mes);
        var totalAReceberEsperadoNoMes = await _contaReceberService.CalcularTotalAReceberEsperadoNoMes(ano, mes);
        var totalPagoFluxoCaixa = await _fluxoCaixaService.CalcularTotalPagoNoMes(ano, mes);
        var totalAPagarFluxoCaixa = await _fluxoCaixaService.CalcularTotalAPagarNoMes(ano, mes);
        var projecaoCartao = await _faturaProjecaoService.CalcularProjecaoCartaoDoMes(ano, mes);

        var totalPagoNoMes = totalPagoFluxoCaixa + projecaoCartao.TotalPago;
        var totalAPagarNoMes = totalAPagarFluxoCaixa + projecaoCartao.TotalNaoPago;

        var saldoProjetado = (totalRecebidoNoMes + totalAReceberEsperadoNoMes) - (totalPagoNoMes + totalAPagarNoMes);

        return new ProjecaoMesResultado(
            ano,
            mes,
            totalRecebidoNoMes,
            totalAReceberEsperadoNoMes,
            totalPagoNoMes,
            totalAPagarNoMes,
            saldoProjetado);
    }
}
