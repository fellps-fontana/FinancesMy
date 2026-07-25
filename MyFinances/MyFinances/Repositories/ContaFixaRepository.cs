using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class ContaFixaRepository : IContaFixaRepository
{
    private readonly MyFinancesDbContext _context;

    public ContaFixaRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(ContaFixa contaFixa)
    {
        await _context.ContasFixas.AddAsync(contaFixa);
    }

    public async Task<ContaFixa?> ObterPorId(Guid id)
    {
        return await QueryComRelacionamentos()
            .FirstOrDefaultAsync(cf => cf.Id == id);
    }

    public async Task<IEnumerable<ContaFixa>> Listar(bool? ativaFiltro = null)
    {
        var query = QueryComRelacionamentos();

        if (ativaFiltro.HasValue)
        {
            query = query.Where(cf => cf.Ativa == ativaFiltro.Value);
        }

        return await query.ToListAsync();
    }

    public async Task Atualizar(ContaFixa contaFixa)
    {
        _context.ContasFixas.Update(contaFixa);
    }

    public async Task<bool> ExisteLancamentoGerado(Guid contaFixaId, int ano, int mes)
    {
        return await _context.Lancamentos
            .AnyAsync(l =>
                l.ContaFixaId == contaFixaId &&
                l.Data.Year == ano &&
                l.Data.Month == mes);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }

    private IQueryable<ContaFixa> QueryComRelacionamentos()
    {
        return _context.ContasFixas
            .Include(cf => cf.Conta)
            .Include(cf => cf.Categoria)
            .Include(cf => cf.Lancamentos);
    }
}
