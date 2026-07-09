using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Models;

namespace MyFinances.Services;

public class TransferenciaService
{
    private readonly AppDbContext _context;

    public TransferenciaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Sucesso, Transferencia? Transferencia, string? Erro)> CriarAsync(
        CriarTransferenciaRequest request)
    {
        var contaOrigem = await _context.Contas
            .FirstOrDefaultAsync(c => c.Id == request.ContaOrigemId);
        if (contaOrigem == null)
        {
            return (false, null, "Conta de origem nao encontrada");
        }

        var contaDestino = await _context.Contas
            .FirstOrDefaultAsync(c => c.Id == request.ContaDestinoId);
        if (contaDestino == null)
        {
            return (false, null, "Conta de destino nao encontrada");
        }

        var validacaoContas = ValidarContas(contaOrigem, contaDestino);
        if (!validacaoContas.Valido)
        {
            return (false, null, validacaoContas.Erro);
        }

        var validacaoValor = ValidarValor(request.Valor);
        if (!validacaoValor.Valido)
        {
            return (false, null, validacaoValor.Erro);
        }

        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            Data = request.Data ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Valor = request.Valor,
            ContaOrigemId = request.ContaOrigemId,
            ContaDestino = contaDestino,
            ContaOrigem = contaOrigem,
            ContaDestinoId = request.ContaDestinoId,
            FaturaId = null,
            Descricao = request.Descricao
        };

        var lancamentoSaida = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaOrigem.Id,
            Conta = contaOrigem,
            Valor = request.Valor,
            Tipo = TipoLancamentoConstants.Debit,
            Data = transferencia.Data,
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Descricao = request.Descricao,
            TransferenciaId = transferencia.Id,
            CategoriaId = null,
            PierreTxnId = null,
            Oculto = false,
            FaturaId = null,
            ContaFixaId = null,
            ConciliadoCom = null
        };

        var lancamentoEntrada = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaDestino.Id,
            Conta = contaDestino,
            Valor = request.Valor,
            Tipo = TipoLancamentoConstants.Credit,
            Data = transferencia.Data,
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Descricao = request.Descricao,
            TransferenciaId = transferencia.Id,
            CategoriaId = null,
            PierreTxnId = null,
            Oculto = false,
            FaturaId = null,
            ContaFixaId = null,
            ConciliadoCom = null
        };

        _context.Transferencias.Add(transferencia);
        _context.Lancamentos.Add(lancamentoSaida);
        _context.Lancamentos.Add(lancamentoEntrada);

        await _context.SaveChangesAsync();

        return (true, transferencia, null);
    }

    private (bool Valido, string? Erro) ValidarContas(Conta contaOrigem, Conta contaDestino)
    {
        if (contaOrigem.Origem != OrigemConstants.Manual)
        {
            return (false, "Conta de origem deve ter origem MANUAL");
        }

        if (contaDestino.Origem != OrigemConstants.Manual)
        {
            return (false, "Conta de destino deve ter origem MANUAL");
        }

        if (contaOrigem.Id == contaDestino.Id)
        {
            return (false, "Conta de origem e destino devem ser diferentes");
        }

        return (true, null);
    }

    private (bool Valido, string? Erro) ValidarValor(decimal valor)
    {
        if (valor <= 0)
        {
            return (false, "Valor deve ser maior que zero");
        }

        return (true, null);
    }
}
