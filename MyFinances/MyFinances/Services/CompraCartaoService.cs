using MyFinances.DTOs;
using MyFinances.Models;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class CompraCartaoService
{
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly FaturaCicloService _faturaCicloService;
    private readonly ValidacaoCartaoService _validacaoCartaoService;

    public CompraCartaoService(
        ILancamentoRepository lancamentoRepository,
        FaturaCicloService faturaCicloService,
        ValidacaoCartaoService validacaoCartaoService)
    {
        _lancamentoRepository = lancamentoRepository;
        _faturaCicloService = faturaCicloService;
        _validacaoCartaoService = validacaoCartaoService;
    }

    public async Task<(bool Sucesso, Lancamento? Compra, string? Erro)> CriarCompraAsync(
        Guid contaId,
        CriarCompraRequest request)
    {
        var (valido, conta, erro) = await _validacaoCartaoService.ValidarOperacaoCartaoAsync(
            contaId,
            request.Descricao,
            request.Valor);

        if (!valido)
        {
            return (false, null, erro);
        }

        var (fatura, rejeitada, motivo) = await _faturaCicloService.ResolverFaturaParaLancamentoAsync(
            contaId,
            request.Data);

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
            Tipo = TipoLancamento.Debit,
            Data = request.Data,
            Status = StatusLancamento.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = fatura!.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };

        await _lancamentoRepository.Adicionar(compra);
        await _lancamentoRepository.Salvar();

        return (true, compra, null);
    }

    public async Task<(bool Sucesso, Lancamento? Compra, string? Erro)> EditarCompraAsync(
        Guid contaId,
        Guid compraId,
        EditarCompraRequest request)
    {
        var compra = await _lancamentoRepository.ObterPorId(compraId);

        if (compra == null || compra.ContaId != contaId)
        {
            return (false, null, "Compra nao encontrada");
        }

        var (valido, _, erro) = await _validacaoCartaoService.ValidarOperacaoCartaoAsync(
            contaId,
            request.Descricao,
            request.Valor);

        if (!valido)
        {
            return (false, null, erro);
        }

        if (compra.Fatura?.Status == StatusFatura.Paga)
        {
            return (false, null, "Compra vinculada a fatura ja paga, nao pode ser editada");
        }

        var dataMudou = compra.Data != request.Data;
        if (dataMudou)
        {
            var (fatura, rejeitada, motivo) = await _faturaCicloService.ResolverFaturaParaLancamentoAsync(
                contaId,
                request.Data);

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

        await _lancamentoRepository.Atualizar(compra);
        await _lancamentoRepository.Salvar();

        return (true, compra, null);
    }
}
