using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class AtivoRepository : IAtivoRepository
{
    private readonly MyFinancesDbContext _context;

    public AtivoRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public Task Adicionar(Ativo ativo)
    {
        _context.Ativos.Add(ativo);
        return Task.CompletedTask;
    }

    public async Task<Ativo?> ObterPorId(Guid id)
    {
        return await _context.Ativos.FindAsync(id);
    }

    public async Task<IEnumerable<Ativo>> ListarAtivas()
    {
        return await Task.FromResult(_context.Ativos.Where(a => a.Ativa).AsEnumerable());
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
