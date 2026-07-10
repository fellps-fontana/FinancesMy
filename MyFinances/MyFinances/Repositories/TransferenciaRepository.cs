using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Repositories;

public class TransferenciaRepository : ITransferenciaRepository
{
    private readonly MyFinancesDbContext _context;

    public TransferenciaRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(Transferencia transferencia)
    {
        await _context.Transferencias.AddAsync(transferencia);
    }

    public async Task<Transferencia?> ObterPorId(Guid id)
    {
        return await _context.Transferencias
            .Include(t => t.ContaOrigem)
            .Include(t => t.ContaDestino)
            .Include(t => t.Fatura)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<Transferencia>> ListarPorConta(Guid contaId)
    {
        return await _context.Transferencias
            .Include(t => t.ContaOrigem)
            .Include(t => t.ContaDestino)
            .Where(t => t.ContaOrigemId == contaId || t.ContaDestinoId == contaId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transferencia>> ListarPorFatura(Guid faturaId)
    {
        return await _context.Transferencias
            .Include(t => t.ContaOrigem)
            .Include(t => t.ContaDestino)
            .Where(t => t.FaturaId == faturaId)
            .ToListAsync();
    }

    public async Task Atualizar(Transferencia transferencia)
    {
        _context.Transferencias.Update(transferencia);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
