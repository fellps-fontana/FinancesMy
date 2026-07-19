using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class EstornoCartaoService
{
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly FaturaCicloService _faturaCicloService;
    private readonly ValidacaoCartaoService _validacaoCartaoService;

    public EstornoCartaoService(
        ILancamentoRepository lancamentoRepository,
        FaturaCicloService faturaCicloService,
        ValidacaoCartaoService validacaoCartaoService)
    {
        _lancamentoRepository = lancamentoRepository;
        _faturaCicloService = faturaCicloService;
        _validacaoCartaoService = validacaoCartaoService;
    }

    public async Task<(bool Sucesso, Lancamento? Estorno, string? Erro)> CriarEstornoAsync(
        Guid contaId,
        CriarEstornoRequest request)
    {
        var compraOriginal = await _lancamentoRepository.ObterPorId(request.CompraId);
        if (compraOriginal == null || compraOriginal.ContaId != contaId)
        {
            return (false, null, "Compra nao encontrada");
        }

        if (compraOriginal.Fatura?.Status == StatusFatura.Paga)
        {
            return (false, null, "Nao e permitido fazer estorno de compra em fatura ja paga");
        }

        var (valido, conta, erro) = await _validacaoCartaoService.ValidarOperacaoCartaoAsync(
            contaId,
            $"Estorno: {request.Motivo}",
            compraOriginal.Valor);

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

        var estorno = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta!,
            CategoriaId = compraOriginal.CategoriaId,
            Descricao = $"Estorno: {request.Motivo}",
            Valor = compraOriginal.Valor,
            Tipo = TipoLancamento.Credit,
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

        await _lancamentoRepository.Adicionar(estorno);
        await _lancamentoRepository.Salvar();

        return (true, estorno, null);
    }
}
