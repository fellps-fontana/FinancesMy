using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class ContaRepository : IContaRepository
{
    private readonly MyFinancesDbContext _context;

    public ContaRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(Conta conta)
    {
        await _context.Contas.AddAsync(conta);
    }

    public async Task<Conta?> ObterPorId(Guid id)
    {
        return await _context.Contas.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Conta>> ListarPorTipo(TipoConta tipo)
    {
        return await _context.Contas
            .Where(c => c.Tipo == tipo)
            .ToListAsync();
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
