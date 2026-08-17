using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Tests.Helpers;

public class FakeRendimentoRepository : IRendimentoRepository
{
    private readonly List<Rendimento> _rendimentos = [];

    public Task Adicionar(Rendimento rendimento)
    {
        rendimento.Id = Guid.NewGuid();
        rendimento.CriadoEm = DateTime.UtcNow;
        _rendimentos.Add(rendimento);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Rendimento>> ListarPorAtivo(Guid ativoId)
    {
        var resultado = _rendimentos
            .Where(r => r.AtivoId == ativoId)
            .OrderBy(r => r.Data)
            .ToList();
        return Task.FromResult((IEnumerable<Rendimento>)resultado);
    }

    public Task<IEnumerable<Rendimento>> ListarTodos()
    {
        return Task.FromResult((IEnumerable<Rendimento>)_rendimentos.OrderBy(r => r.Data).ToList());
    }

    public Task Salvar()
    {
        return Task.CompletedTask;
    }
}
