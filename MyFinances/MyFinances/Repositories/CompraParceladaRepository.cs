using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class CompraParceladaRepository : ICompraParceladaRepository
{
    private readonly MyFinancesDbContext _context;

    public CompraParceladaRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(CompraParcelada compraParcelada)
    {
        await _context.ComprasParceladas.AddAsync(compraParcelada);
    }

    public async Task<CompraParcelada?> ObterPorId(Guid id)
    {
        return await _context.ComprasParceladas
            .Include(cp => cp.Lancamentos)
            .FirstOrDefaultAsync(cp => cp.Id == id);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    public async Task RollbackAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }
}
