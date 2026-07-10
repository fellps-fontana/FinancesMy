using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Repositories;

public class FaturaRepository : IFaturaRepository
{
    private readonly MyFinancesDbContext _context;

    public FaturaRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(Fatura fatura)
    {
        await _context.Faturas.AddAsync(fatura);
    }

    public async Task<Fatura?> ObterPorId(Guid id)
    {
        return await _context.Faturas
            .Include(f => f.Conta)
            .Include(f => f.Transferencias)
            .Include(f => f.Lancamentos)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<Fatura>> ListarPorConta(Guid contaId)
    {
        return await _context.Faturas
            .Include(f => f.Conta)
            .Include(f => f.Transferencias)
            .Include(f => f.Lancamentos)
            .Where(f => f.ContaId == contaId)
            .ToListAsync();
    }

    public async Task<Fatura?> ObterFaturaAbertaPorConta(Guid contaId)
    {
        return await _context.Faturas
            .Include(f => f.Conta)
            .Include(f => f.Transferencias)
            .Include(f => f.Lancamentos)
            .FirstOrDefaultAsync(f => f.ContaId == contaId && f.Status == StatusFatura.Aberta);
    }

    public async Task Atualizar(Fatura fatura)
    {
        _context.Faturas.Update(fatura);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
