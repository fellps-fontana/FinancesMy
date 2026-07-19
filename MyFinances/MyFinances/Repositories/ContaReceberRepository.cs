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
        return await QueryComRelacionamentos()
            .FirstOrDefaultAsync(cr => cr.Id == id);
    }

    public async Task<IEnumerable<ContaReceber>> Listar(StatusContaReceber? statusFiltro = null)
    {
        var query = QueryComRelacionamentos();

        if (statusFiltro.HasValue)
        {
            query = query.Where(cr => cr.Status == statusFiltro.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<ContaReceber>> ListarParaProjecaoDoMes(int ano, int mes)
    {
        return await QueryComRelacionamentos()
            .Where(cr =>
                cr.Status == StatusContaReceber.Parcial ||
                (cr.Status == StatusContaReceber.Pendente &&
                 cr.DataPrevista != null &&
                 cr.DataPrevista.Value.Year == ano &&
                 cr.DataPrevista.Value.Month == mes))
            .ToListAsync();
    }

    private IQueryable<ContaReceber> QueryComRelacionamentos()
    {
        return _context.ContasReceber
            .Include(cr => cr.Categoria)
            .Include(cr => cr.Recebimentos);
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
