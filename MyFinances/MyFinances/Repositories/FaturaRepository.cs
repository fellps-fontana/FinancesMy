using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;

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

    public void Descartar(Fatura fatura)
    {
        _context.Entry(fatura).State = EntityState.Detached;
    }

    public async Task<Fatura?> ObterPorContaEDataFechamento(Guid contaId, DateOnly dataFechamento)
    {
        return await _context.Faturas
            .Include(f => f.Conta)
            .Include(f => f.Transferencias)
            .Include(f => f.Lancamentos)
            .FirstOrDefaultAsync(f => f.ContaId == contaId && f.DataFechamento == dataFechamento);
    }

    // Fechada com fechamento ja passado nao muda na reconciliacao, entao fica de fora.
    public async Task<IReadOnlyList<Fatura>> ListarParaReconciliacao(Guid contaId, DateOnly hoje)
    {
        return await _context.Faturas
            .Include(f => f.Conta)
            .Include(f => f.Transferencias)
            .Include(f => f.Lancamentos)
            .Where(f => f.ContaId == contaId &&
                        (f.Status != StatusFatura.Fechada || f.DataFechamento >= hoje))
            .OrderBy(f => f.DataFechamento)
            .ToListAsync();
    }

    public async Task<IEnumerable<Fatura>> ListarFaturasCartaoPorVencimentoNoMes(int ano, int mes)
    {
        return await _context.Faturas
            .Include(f => f.Conta)
            .Include(f => f.Transferencias)
            .Include(f => f.Lancamentos)
            .Where(f => f.Conta!.Tipo == TipoConta.Cartao &&
                        f.DataVencimento.Year == ano &&
                        f.DataVencimento.Month == mes)
            .ToListAsync();
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
