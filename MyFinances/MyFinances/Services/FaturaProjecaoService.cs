using MyFinances.Repositories;

namespace MyFinances.Services;

public class FaturaProjecaoService : IFaturaProjecaoService
{
    private readonly IFaturaRepository _faturaRepository;

    public FaturaProjecaoService(IFaturaRepository faturaRepository)
    {
        _faturaRepository = faturaRepository;
    }

    public Task<FaturaProjecaoMes> CalcularProjecaoCartaoDoMes(int ano, int mes)
    {
        throw new NotImplementedException();
    }
}
