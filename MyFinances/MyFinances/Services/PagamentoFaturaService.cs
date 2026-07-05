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
            .Include(f => f.Lancamentos)
            .Include(f => f.Transferencias)
            .FirstOrDefaultAsync(f => f.Id == faturaId);

        if (fatura == null)
        {
            return (false, null, "Fatura nao encontrada");
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

        if (request.Valor <= 0)
        {
            return (false, null, "Valor deve ser positivo");
        }

        if (!fatura.Lancamentos.Any())
        {
            return (false, null, "Fatura nao possui lancamentos para pagar");
        }

        var saldo = FaturaSaldoCalculator.Calcular(fatura);

        if (saldo.ValorPendente <= 0)
        {
            return (false, null, "Fatura ja esta quitada, nao aceita mais pagamento");
        }

        if (request.Valor > saldo.ValorPendente)
        {
            return (false, null, "Valor excede o saldo pendente da fatura");
        }

        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            Data = request.Data,
            Valor = request.Valor,
            ContaOrigemId = request.ContaOrigemId,
            ContaDestinoId = fatura.ContaId,
            FaturaId = fatura.Id,
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
            Valor = request.Valor,
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
            Valor = request.Valor,
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

        _context.Transferencias.Add(transferencia);
        _context.Lancamentos.Add(lancamentoSaida);
        _context.Lancamentos.Add(lancamentoEntrada);

        var novoSaldoPendente = saldo.ValorPendente - request.Valor;

        if (novoSaldoPendente <= 0)
        {
            if (fatura.Status == FaturaStatusConstants.Fechada)
            {
                fatura.Status = FaturaStatusConstants.Paga;
            }
        }

        await _context.SaveChangesAsync();

        return (true, fatura, null);
    }
}
