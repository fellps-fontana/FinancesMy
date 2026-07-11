using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly MyFinancesDbContext _context;

    public LancamentoRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(Lancamento lancamento)
    {
        await _context.Lancamentos.AddAsync(lancamento);
    }

    public async Task<Lancamento?> ObterPorId(Guid id)
    {
        return await _context.Lancamentos
            .Include(l => l.Conta)
            .Include(l => l.Categoria)
            .Include(l => l.Fatura)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<IEnumerable<Lancamento>> ListarPorConta(Guid contaId)
    {
        return await _context.Lancamentos
            .Include(l => l.Conta)
            .Include(l => l.Categoria)
            .Where(l => l.ContaId == contaId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Lancamento>> ListarPorFatura(Guid faturaId)
    {
        return await _context.Lancamentos
            .Include(l => l.Conta)
            .Include(l => l.Categoria)
            .Where(l => l.FaturaId == faturaId)
            .ToListAsync();
    }

    public async Task Atualizar(Lancamento lancamento)
    {
        _context.Lancamentos.Update(lancamento);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
