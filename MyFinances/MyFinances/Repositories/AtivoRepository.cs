using Microsoft.EntityFrameworkCore;
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
        return await _context.Ativos
            .Include(a => a.Conta)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Ativo>> ListarAtivas()
    {
        return await _context.Ativos
            .Include(a => a.Conta)
            .Where(a => a.Ativa)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ativo>> ListarAtivosPorConta(Guid contaId)
    {
        return await _context.Ativos
            .Where(a => a.Ativa && a.ContaId == contaId)
            .ToListAsync();
    }

    public Task<bool> ExisteAtivoAtivoVinculado(Guid contaId)
    {
        return _context.Ativos.AnyAsync(a => a.Ativa && a.ContaId == contaId);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
