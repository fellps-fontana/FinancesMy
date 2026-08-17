using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class RendimentoRepository : IRendimentoRepository
{
    private readonly MyFinancesDbContext _context;

    public RendimentoRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public Task Adicionar(Rendimento rendimento)
    {
        _context.Rendimentos.Add(rendimento);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Rendimento>> ListarPorAtivo(Guid ativoId)
    {
        return await Task.FromResult(
            _context.Rendimentos
                .Where(r => r.AtivoId == ativoId)
                .OrderBy(r => r.Data)
                .AsEnumerable());
    }

    public async Task<IEnumerable<Rendimento>> ListarTodos()
    {
        return await Task.FromResult(_context.Rendimentos.AsEnumerable());
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
