using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Dtos;
using MyFinances.Models;

namespace MyFinances.Services;

public class PagamentoFaturaService
{
    private readonly AppDbContext _context;

    public PagamentoFaturaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Sucesso, Fatura? Fatura, string? Erro)> PagarFaturaAsync(
        Guid faturaId,
        PagarFaturaRequest request)
    {
        var fatura = await _context.Faturas
            .Include(f => f.Conta)
            .FirstOrDefaultAsync(f => f.Id == faturaId);

        if (fatura == null)
        {
            return (false, null, "Fatura nao encontrada");
        }

        if (fatura.Status == FaturaStatusConstants.Aberta)
        {
            return (false, null, "Nao e possivel pagar fatura ainda ABERTA");
        }

        if (fatura.Status == FaturaStatusConstants.Paga)
        {
            return (false, null, "Fatura ja foi paga");
        }

        if (request.ContaOrigemId == fatura.ContaId)
        {
            return (false, null, "Nao e possivel pagar fatura com a propria conta do cartao");
        }

        var contaOrigem = await _context.Contas
            .FirstOrDefaultAsync(c => c.Id == request.ContaOrigemId);

        if (contaOrigem == null)
        {
            return (false, null, "Conta de origem nao encontrada");
        }

        if (contaOrigem.Tipo != TipoContaConstants.Banco)
        {
            return (false, null, "Conta de origem deve ser do tipo BANCO");
        }

        if (contaOrigem.Origem == OrigemConstants.OpenFinance)
        {
            return (false, null, "Nao e permitido pagar fatura com conta Open Finance nesta versao");
        }

        var valorPagamento = await CalcularValorPagamentoAsync(faturaId);

        if (valorPagamento <= 0)
        {
            return (false, null, "Fatura nao possui lancamentos para pagar");
        }

        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            Data = request.Data,
            Valor = valorPagamento,
            ContaOrigemId = request.ContaOrigemId,
            ContaDestinoId = fatura.ContaId,
            Descricao = $"Pagamento de fatura - {fatura.DataFechamento:dd/MM/yyyy} a {fatura.DataVencimento:dd/MM/yyyy}",
            ContaOrigem = contaOrigem,
            ContaDestino = fatura.Conta
        };

        var lancamentoSaida = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = request.ContaOrigemId,
            Conta = contaOrigem,
            CategoriaId = null,
            Descricao = transferencia.Descricao,
            Valor = valorPagamento,
            Tipo = TipoLancamentoConstants.Debit,
            Data = request.Data,
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            ContaFixaId = null,
            ConciliadoCom = null,
            TransferenciaId = transferencia.Id,
            FaturaId = null
        };

        var lancamentoEntrada = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = fatura.ContaId,
            Conta = fatura.Conta,
            CategoriaId = null,
            Descricao = transferencia.Descricao,
            Valor = valorPagamento,
            Tipo = TipoLancamentoConstants.Credit,
            Data = request.Data,
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            ContaFixaId = null,
            ConciliadoCom = null,
            TransferenciaId = transferencia.Id,
            FaturaId = null
        };

        fatura.Status = FaturaStatusConstants.Paga;
        fatura.TransferenciaId = transferencia.Id;

        _context.Transferencias.Add(transferencia);
        _context.Lancamentos.Add(lancamentoSaida);
        _context.Lancamentos.Add(lancamentoEntrada);

        await _context.SaveChangesAsync();

        return (true, fatura, null);
    }

    private async Task<decimal> CalcularValorPagamentoAsync(Guid faturaId)
    {
        var lancamentos = await _context.Lancamentos
            .Where(l => l.FaturaId == faturaId)
            .ToListAsync();

        return lancamentos.Sum(l => l.Valor);
    }
}
