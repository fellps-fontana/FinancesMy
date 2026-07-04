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
        var validacao = await ValidarCompraAsync(contaId, request.Descricao, request.Valor);
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

        var validacao = await ValidarCompraAsync(contaId, request.Descricao, request.Valor);
        if (!validacao.Valido)
        {
            return (false, null, validacao.Erro);
        }

        // Validar que fatura atualmente vinculada nao esta PAGA antes de qualquer edicao
        var faturaAtual = await _context.Faturas
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == compra.FaturaId);

        if (faturaAtual?.Status == FaturaStatusConstants.Paga)
        {
            return (false, null, "Compra vinculada a fatura ja paga, nao pode ser editada");
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

    private async Task<(bool Valido, string? Erro)> ValidarCompraAsync(Guid contaId, string descricao, decimal valor)
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

        if (string.IsNullOrWhiteSpace(descricao))
        {
            return (false, "Descricao e obrigatoria");
        }

        if (valor <= 0)
        {
            return (false, "Valor deve ser positivo");
        }

        return (true, null);
    }
}
