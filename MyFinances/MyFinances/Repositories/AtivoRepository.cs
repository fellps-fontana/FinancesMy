using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Repositories;

public class AtivoRepository : IAtivoRepository
{
    private readonly MyFinancesDbContext _context;

    public AtivoRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(Ativo ativo)
    {
        await _context.Ativos.AddAsync(ativo);
    }

    public async Task AdicionarMovimentacao(MovimentacaoAtivo movimentacao)
    {
        await _context.MovimentacoesAtivo.AddAsync(movimentacao);
    }

    public async Task<Ativo?> ObterPorId(Guid id)
    {
        return await _context.Ativos.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Ativo>> ListarPorConta(Guid contaId)
    {
        return await _context.Ativos
            .Where(a => a.ContaId == contaId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ativo>> ListarAtivosAtivosPorConta(Guid contaId)
    {
        return await _context.Ativos
            .Where(a => a.ContaId == contaId && a.Ativa)
            .ToListAsync();
    }

    public async Task<Ativo?> ObterAtivoAtivoPorTicker(Guid contaId, string ticker)
    {
        return await _context.Ativos
            .FirstOrDefaultAsync(a => a.ContaId == contaId && a.Ativa && a.Ticker == ticker);
    }

    public async Task<Dictionary<Guid, decimal>> SomarValorAtivosPorConta(IEnumerable<Guid> contaIds)
    {
        var ativos = await _context.Ativos
            .Where(a => contaIds.Contains(a.ContaId) && a.Ativa)
            .ToListAsync();

        // Apenas contas com ativos ativos aparecem no dicionario.
        // Contas sem nenhum ativo ativo nao sao incluidas.
        return ativos
            .GroupBy(a => a.ContaId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(a => a.Quantidade * a.PrecoAtual));
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
