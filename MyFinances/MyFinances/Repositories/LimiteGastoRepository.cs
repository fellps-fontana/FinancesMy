using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class LimiteGastoRepository : ILimiteGastoRepository
{
    private readonly MyFinancesDbContext _context;

    public LimiteGastoRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(LimiteGasto limiteGasto)
    {
        await _context.LimitesGasto.AddAsync(limiteGasto);
    }

    public async Task<LimiteGasto?> ObterPorCategoriaId(Guid categoriaId)
    {
        return await _context.LimitesGasto
            .Include(l => l.Categoria)
            .FirstOrDefaultAsync(l => l.CategoriaId == categoriaId);
    }

    public async Task<IEnumerable<LimiteGasto>> Listar()
    {
        return await _context.LimitesGasto
            .Include(l => l.Categoria)
            .ToListAsync();
    }

    public async Task Remover(LimiteGasto limiteGasto)
    {
        _context.LimitesGasto.Remove(limiteGasto);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
