using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class AssinaturaCartaoRepository : IAssinaturaCartaoRepository
{
    private readonly MyFinancesDbContext _context;

    public AssinaturaCartaoRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(AssinaturaCartao assinatura)
    {
        await _context.AssinaturasCartao.AddAsync(assinatura);
    }

    public async Task<AssinaturaCartao?> ObterPorId(Guid id)
    {
        return await QueryComRelacionamentos()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<AssinaturaCartao>> Listar(Guid? contaId = null, bool? ativaFiltro = null)
    {
        var query = QueryComRelacionamentos();

        if (contaId.HasValue)
        {
            query = query.Where(a => a.ContaId == contaId.Value);
        }

        if (ativaFiltro.HasValue)
        {
            query = query.Where(a => a.Ativa == ativaFiltro.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<AssinaturaCartao>> ListarAtivasPorConta(Guid contaId)
    {
        return await QueryComRelacionamentos()
            .Where(a => a.ContaId == contaId && a.Ativa)
            .ToListAsync();
    }

    public async Task Atualizar(AssinaturaCartao assinatura)
    {
        _context.AssinaturasCartao.Update(assinatura);
    }

    public async Task<bool> ExisteCompraGerada(Guid assinaturaCartaoId, int ano, int mes)
    {
        return await _context.Lancamentos
            .AnyAsync(l =>
                l.AssinaturaCartaoId == assinaturaCartaoId &&
                l.Data.Year == ano &&
                l.Data.Month == mes);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }

    private IQueryable<AssinaturaCartao> QueryComRelacionamentos()
    {
        return _context.AssinaturasCartao
            .Include(a => a.Conta)
            .Include(a => a.Categoria)
            .Include(a => a.Compras)
                .ThenInclude(c => c.Fatura);
    }
}
