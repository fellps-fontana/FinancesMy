using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Models;

namespace MyFinances.Services;

public class ProjecaoService
{
    private readonly AppDbContext _context;
    private readonly ValidacaoCartaoService _validacaoCartao;

    public ProjecaoService(AppDbContext context, ValidacaoCartaoService validacaoCartao)
    {
        _context = context;
        _validacaoCartao = validacaoCartao;
    }

    public async Task<(bool Sucesso, ProjecaoCartaoResponseDto? Projecao, string? Erro)>
        ObterProjecaoCartaoAsync(Guid contaId, int mes, int ano)
    {
        var (validoCartao, conta, erroCartao) = await _validacaoCartao.ValidarContaCartaoAsync(contaId);

        if (!validoCartao)
        {
            return (false, null, erroCartao);
        }

        var dataPrimeiroMes = new DateOnly(ano, mes, 1);
        var dataUltimoMes = dataPrimeiroMes.AddMonths(1).AddDays(-1);

        var faturas = await _context.Faturas
            .Where(f => f.ContaId == contaId &&
                        f.DataVencimento >= dataPrimeiroMes &&
                        f.DataVencimento <= dataUltimoMes)
            .Include(f => f.Lancamentos)
            .Include(f => f.Transferencias)
            .OrderByDescending(f => f.DataFechamento)
            .ToListAsync();

        // Lacuna de regra pendente: multiplas faturas no mesmo mes nao sao impedidas
        // pelo banco. Hoje pega silenciosamente a mais recente. Registrar para diagnostico.
        if (faturas.Count > 1)
        {
            var faturasIds = string.Join(", ", faturas.Select(f => f.Id));
            Console.WriteLine($"[ATENCAO] Multiplas faturas encontradas para conta {contaId} " +
                             $"no mes {mes}/{ano}. Ids: {faturasIds}. " +
                             $"Usando a mais recente por DataFechamento. Revisao de regra necessaria.");
        }

        if (!faturas.Any())
        {
            return (true, new ProjecaoCartaoResponseDto
            {
                ContaId = contaId,
                Mes = mes,
                Ano = ano,
                TemFatura = false,
                FaturaId = null,
                StatusPagamento = null,
                Valor = 0
            }, null);
        }

        var fatura = faturas.First();
        var saldo = FaturaSaldoCalculator.Calcular(fatura);

        var statusPagamento = saldo.ValorPendente <= 0 ? StatusProjecaoConstants.Pago : StatusProjecaoConstants.NaoPago;
        var valor = saldo.ValorPendente > 0 ? saldo.ValorPendente : saldo.ValorTotal;

        var projecao = new ProjecaoCartaoResponseDto
        {
            ContaId = contaId,
            Mes = mes,
            Ano = ano,
            TemFatura = true,
            FaturaId = fatura.Id,
            StatusPagamento = statusPagamento,
            Valor = valor
        };

        return (true, projecao, null);
    }
}
