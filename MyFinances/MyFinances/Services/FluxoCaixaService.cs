using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;

namespace MyFinances.Services;

public class FluxoCaixaService
{
    private readonly AppDbContext _context;

    public FluxoCaixaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<LancamentoCaixaResponseDto>> ObterLancamentosCaixaAsync(Guid? contaId = null)
    {
        var query = _context.Lancamentos
            .Where(l => l.FaturaId == null)
            .Where(l => l.TransferenciaId == null || l.Tipo == TipoLancamentoConstants.Debit)
            .Where(l => !l.Oculto);

        if (contaId.HasValue)
        {
            query = query.Where(l => l.ContaId == contaId.Value);
        }

        var lancamentos = await query
            .OrderByDescending(l => l.Data)
            .Select(l => new LancamentoCaixaResponseDto
            {
                Id = l.Id,
                ContaId = l.ContaId,
                CategoriaId = l.CategoriaId,
                Descricao = l.Descricao,
                Valor = l.Valor,
                Tipo = l.Tipo,
                Data = l.Data,
                Manual = l.Manual
            })
            .ToListAsync();

        return lancamentos;
    }
}
