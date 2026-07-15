using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class ContaReceberRepository : IContaReceberRepository
{
    private readonly MyFinancesDbContext _context;

    public ContaReceberRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(ContaReceber contaReceber)
    {
        await _context.ContasReceber.AddAsync(contaReceber);
    }

    public async Task<ContaReceber?> ObterPorId(Guid id)
    {
        return await _context.ContasReceber
            .Include(cr => cr.Categoria)
            .Include(cr => cr.Recebimentos)
            .FirstOrDefaultAsync(cr => cr.Id == id);
    }

    public async Task<IEnumerable<ContaReceber>> Listar(StatusContaReceber? statusFiltro = null)
    {
        var query = _context.ContasReceber
            .Include(cr => cr.Categoria)
            .Include(cr => cr.Recebimentos)
            .AsQueryable();

        if (statusFiltro.HasValue)
        {
            query = query.Where(cr => cr.Status == statusFiltro.Value);
        }

        return await query.ToListAsync();
    }

    public async Task Atualizar(ContaReceber contaReceber)
    {
        _context.ContasReceber.Update(contaReceber);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
