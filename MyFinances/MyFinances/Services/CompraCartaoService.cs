using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Dtos;
using MyFinances.Models;

namespace MyFinances.Services;

public class CompraCartaoService
{
    private readonly AppDbContext _context;
    private readonly FaturaCicloService _faturaCicloService;

    public CompraCartaoService(AppDbContext context, FaturaCicloService faturaCicloService)
    {
        _context = context;
        _faturaCicloService = faturaCicloService;
    }

    public async Task<(bool Sucesso, Lancamento? Compra, string? Erro)> CriarCompraAsync(
        Guid contaId,
        CriarCompraRequest request)
    {
        var validacao = await ValidarCriacaoCompraAsync(contaId, request);
        if (!validacao.Valido)
        {
            return (false, null, validacao.Erro);
        }

        var conta = await _context.Contas.FirstOrDefaultAsync(c => c.Id == contaId);

        var (fatura, rejeitada, motivo) = await _faturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, request.Data);

        if (rejeitada)
        {
            return (false, null, motivo);
        }

        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta!,
            CategoriaId = request.CategoriaId,
            Descricao = request.Descricao,
            Valor = request.Valor,
            Tipo = TipoLancamentoConstants.Debit,
            Data = request.Data,
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = fatura!.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };

        _context.Lancamentos.Add(compra);
        await _context.SaveChangesAsync();

        return (true, compra, null);
    }

    public async Task<(bool Sucesso, Lancamento? Compra, string? Erro)> EditarCompraAsync(
        Guid contaId,
        Guid compraId,
        EditarCompraRequest request)
    {
        var compra = await _context.Lancamentos
            .FirstOrDefaultAsync(l => l.Id == compraId && l.ContaId == contaId);

        if (compra == null)
        {
            return (false, null, "Compra nao encontrada");
        }

        var validacao = await ValidarEdicaoCompraAsync(contaId, request);
        if (!validacao.Valido)
        {
            return (false, null, validacao.Erro);
        }

        var dataMudou = compra.Data != request.Data;
        if (dataMudou)
        {
            var (fatura, rejeitada, motivo) = await _faturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, request.Data);

            if (rejeitada)
            {
                return (false, null, motivo);
            }

            compra.FaturaId = fatura!.Id;
            compra.Data = request.Data;
        }

        compra.CategoriaId = request.CategoriaId;
        compra.Descricao = request.Descricao;
        compra.Valor = request.Valor;

        _context.Lancamentos.Update(compra);
        await _context.SaveChangesAsync();

        return (true, compra, null);
    }

    private async Task<(bool Valido, string? Erro)> ValidarCriacaoCompraAsync(Guid contaId, CriarCompraRequest request)
    {
        var conta = await _context.Contas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contaId);

        if (conta == null)
        {
            return (false, "Conta nao encontrada");
        }

        if (conta.Tipo != TipoContaConstants.Cartao)
        {
            return (false, "Conta nao e do tipo CARTAO");
        }

        if (string.IsNullOrWhiteSpace(request.Descricao))
        {
            return (false, "Descricao e obrigatoria");
        }

        if (request.Valor <= 0)
        {
            return (false, "Valor deve ser positivo");
        }

        return (true, null);
    }

    private async Task<(bool Valido, string? Erro)> ValidarEdicaoCompraAsync(Guid contaId, EditarCompraRequest request)
    {
        var conta = await _context.Contas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contaId);

        if (conta == null)
        {
            return (false, "Conta nao encontrada");
        }

        if (conta.Tipo != TipoContaConstants.Cartao)
        {
            return (false, "Conta nao e do tipo CARTAO");
        }

        if (string.IsNullOrWhiteSpace(request.Descricao))
        {
            return (false, "Descricao e obrigatoria");
        }

        if (request.Valor <= 0)
        {
            return (false, "Valor deve ser positivo");
        }

        return (true, null);
    }
}

public static class TipoLancamentoConstants
{
    public const string Debit = "DEBIT";
    public const string Credit = "CREDIT";
}

public static class LancamentoStatusConstants
{
    public const string Pendente = "PENDENTE";
    public const string Sugerido = "SUGERIDO";
    public const string Pago = "PAGO";
}
