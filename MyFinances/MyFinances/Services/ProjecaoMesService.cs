namespace MyFinances.Services;

public class ProjecaoMesService : IProjecaoMesService
{
    private readonly IFluxoCaixaService _fluxoCaixaService;
    private readonly IContaReceberService _contaReceberService;
    private readonly IFaturaProjecaoService _faturaProjecaoService;
    private readonly IRecorrenciaGeradorService _recorrenciaGeradorService;
    private readonly IRecebivelRecorrenteGeradorService _recebivelRecorrenteGeradorService;

    public ProjecaoMesService(
        IFluxoCaixaService fluxoCaixaService,
        IContaReceberService contaReceberService,
        IFaturaProjecaoService faturaProjecaoService,
        IRecorrenciaGeradorService recorrenciaGeradorService,
        IRecebivelRecorrenteGeradorService recebivelRecorrenteGeradorService)
    {
        _fluxoCaixaService = fluxoCaixaService;
        _contaReceberService = contaReceberService;
        _faturaProjecaoService = faturaProjecaoService;
        _recorrenciaGeradorService = recorrenciaGeradorService;
        _recebivelRecorrenteGeradorService = recebivelRecorrenteGeradorService;
    }

    public async Task<ProjecaoMesResultado> CalcularProjecaoDoMes(int ano, int mes)
    {
        // Rede de seguranca (item 15): materializa recebivel recorrente na janela
        // padrao antes do calculo -- sem estender ate a proxima ocorrencia.
        await _recebivelRecorrenteGeradorService.MaterializarTodosAtivosNaJanelaPadraoAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.Date));

        await _recorrenciaGeradorService.GarantirOcorrenciasAtivasDoMesAsync(ano, mes);

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
