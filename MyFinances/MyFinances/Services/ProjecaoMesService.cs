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

    public Task<ProjecaoMesResultado> CalcularProjecaoDoMes(int ano, int mes)
    {
        throw new NotImplementedException();
    }
}
