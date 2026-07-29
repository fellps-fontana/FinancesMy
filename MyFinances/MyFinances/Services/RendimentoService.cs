using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class RendimentoService : IRendimentoService
{
    private readonly IRendimentoRepository _rendimentoRepository;
    private readonly IAtivoRepository _ativoRepository;

    public RendimentoService(IRendimentoRepository rendimentoRepository, IAtivoRepository ativoRepository)
    {
        _rendimentoRepository = rendimentoRepository;
        _ativoRepository = ativoRepository;
    }

    public Task<Rendimento> RegistrarDividendo(Guid ativoId, decimal valor, DateOnly data)
    {
        throw new NotImplementedException();
    }

    public Task RegistrarValorizacaoAutomatica(Guid ativoId, decimal valorAtualAnterior, decimal valorAtualNovo, DateOnly data)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Rendimento>> ObterHistorico(Guid ativoId)
    {
        throw new NotImplementedException();
    }

    public Task<RendimentosResumo> ObterResumoGeral()
    {
        throw new NotImplementedException();
    }
}
