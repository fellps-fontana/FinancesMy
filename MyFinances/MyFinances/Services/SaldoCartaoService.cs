using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Services;

public class SaldoCartaoService
{
    private readonly AppDbContext _context;
    private readonly ValidacaoCartaoService _validacaoCartaoService;

    public SaldoCartaoService(AppDbContext context, ValidacaoCartaoService validacaoCartaoService)
    {
        _context = context;
        _validacaoCartaoService = validacaoCartaoService;
    }

    public async Task<(bool Sucesso, decimal Saldo, string? Erro)> CalcularSaldoAsync(Guid contaId)
    {
        var (valido, conta, erro) = await _validacaoCartaoService.ValidarContaCartaoAsync(contaId);

        if (!valido)
        {
            return (false, 0, erro);
        }

        var comprasEstornos = await _context.Lancamentos
            .Where(l => l.ContaId == contaId && l.FaturaId != null)
            .SumAsync(l => l.Valor);

        var pagamentos = await _context.Lancamentos
            .Where(l => l.ContaId == contaId
                && l.TransferenciaId != null
                && l.Tipo == TipoLancamentoConstants.Credit)
            .SumAsync(l => l.Valor);

        var saldo = comprasEstornos - pagamentos;

        return (true, saldo, null);
    }
}
