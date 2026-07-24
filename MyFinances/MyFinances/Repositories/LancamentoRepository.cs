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

    public async Task<IEnumerable<Lancamento>> ListarParaFluxoCaixa(Guid? contaId)
    {
        var lancamentos = await _context.Lancamentos
            .Include(l => l.Conta)
            .Include(l => l.Categoria)
            .Where(l => l.FaturaId == null)
            .Where(l => !l.Oculto)
            .ToListAsync();

        if (contaId.HasValue)
        {
            lancamentos = lancamentos
                .Where(l => l.ContaId == contaId.Value)
                .ToList();
        }
        else
        {
            lancamentos = lancamentos
                .Where(l => ClassificacaoLancamentoService.Classificar(l) != ClassificacaoLancamento.Transferencia)
                .ToList();
        }

        return lancamentos;
    }

    public async Task<IEnumerable<Lancamento>> ListarPorCategoriasEPeriodo(IEnumerable<Guid> categoriaIds, int ano, int mes)
    {
        var categoriasSet = categoriaIds.ToList();

        return await _context.Lancamentos
            .Include(l => l.Conta)
            .Include(l => l.Categoria)
            .Where(l => l.CategoriaId.HasValue && categoriasSet.Contains(l.CategoriaId.Value))
            .Where(l => l.Data.Year == ano && l.Data.Month == mes)
            .ToListAsync();
    }

    public async Task<IEnumerable<Lancamento>> ListarParaFluxoCaixaDoMes(int ano, int mes)
    {
        return await _context.Lancamentos
            .Include(l => l.Conta)
            .Include(l => l.Categoria)
            .Where(l => l.FaturaId == null)
            .Where(l => !l.Oculto)
            .Where(l => l.Data.Year == ano && l.Data.Month == mes)
            .ToListAsync();
    }

    public async Task Atualizar(Lancamento lancamento)
    {
        _context.Lancamentos.Update(lancamento);
    }

    public async Task Remover(Lancamento lancamento)
    {
        _context.Lancamentos.Remove(lancamento);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
