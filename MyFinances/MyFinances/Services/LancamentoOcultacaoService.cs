using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Models;

namespace MyFinances.Services;

public class LancamentoOcultacaoService
{
    private readonly AppDbContext _context;

    public LancamentoOcultacaoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Sucesso, string? Erro)> OcultarAsync(Guid lancamentoId)
    {
        var lancamento = await _context.Lancamentos
            .FirstOrDefaultAsync(l => l.Id == lancamentoId);

        if (lancamento == null)
        {
            return (false, "Lancamento nao encontrado");
        }

        if (!PodeOcultarLancamento(lancamento))
        {
            return (false, "Lancamento manual nao pode ser ocultado. Use DELETE para remover.");
        }

        lancamento.Oculto = true;
        _context.Lancamentos.Update(lancamento);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    private bool PodeOcultarLancamento(Lancamento lancamento)
    {
        return !lancamento.Manual;
    }
}
