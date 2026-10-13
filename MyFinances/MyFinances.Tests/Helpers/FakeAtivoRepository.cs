using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Tests.Helpers;

public class FakeAtivoRepository : IAtivoRepository
{
    private readonly List<Ativo> _ativos = [];

    public Task Adicionar(Ativo ativo)
    {
        _ativos.Add(ativo);
        return Task.CompletedTask;
    }

    public Task<Ativo?> ObterPorId(Guid id)
    {
        var ativo = _ativos.FirstOrDefault(a => a.Id == id);
        return Task.FromResult(ativo);
    }

    public Task<IEnumerable<Ativo>> ListarAtivas()
    {
        var ativas = _ativos.Where(a => a.Ativa).ToList();
        return Task.FromResult((IEnumerable<Ativo>)ativas);
    }

    public Task<IEnumerable<Ativo>> ListarAtivosPorConta(Guid contaId)
    {
        var daConta = _ativos.Where(a => a.Ativa && a.ContaId == contaId).ToList();
        return Task.FromResult((IEnumerable<Ativo>)daConta);
    }

    public Task<bool> ExisteAtivoAtivoVinculado(Guid contaId)
    {
        return Task.FromResult(_ativos.Any(a => a.Ativa && a.ContaId == contaId));
    }

    public Task Salvar()
    {
        return Task.CompletedTask;
    }
}
