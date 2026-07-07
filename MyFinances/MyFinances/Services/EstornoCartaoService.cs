using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Models;

namespace MyFinances.Services;

/// <summary>
/// Servico de estorno de compra no cartao.
///
/// Modelagem: Estorno e um Lancamento com Tipo=DEBIT e Valor NEGATIVO,
/// representando a devolucao de uma compra. Segue a mesma logica de fatura
/// que a compra (ResolverFaturaParaLancamentoAsync), pois estorno e um
/// lancamento de cartao como outro qualquer, apenas com valor negativo.
///
/// O valor recebido do client e POSITIVO, e convertido para NEGATIVO
/// antes de armazenar no banco (abordagem mais intuitiva para UX).
/// </summary>
public class EstornoCartaoService
{
    private readonly AppDbContext _context;
    private readonly FaturaCicloService _faturaCicloService;
    private readonly ValidacaoCartaoService _validacaoCartaoService;

    public EstornoCartaoService(
        AppDbContext context,
        FaturaCicloService faturaCicloService,
        ValidacaoCartaoService validacaoCartaoService)
    {
        _context = context;
        _faturaCicloService = faturaCicloService;
        _validacaoCartaoService = validacaoCartaoService;
    }

    public async Task<(bool Sucesso, Lancamento? Estorno, string? Erro)> CriarEstornoAsync(
        Guid contaId,
        CriarEstornoRequest request)
    {
        var (valido, conta, erro) = await _validacaoCartaoService.ValidarOperacaoCartaoAsync(
            contaId,
            request.Descricao,
            request.Valor);

        if (!valido)
        {
            return (false, null, erro);
        }

        var (fatura, rejeitada, motivo) = await _faturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, request.Data);

        if (rejeitada)
        {
            return (false, null, motivo);
        }

        var estorno = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta!,
            CategoriaId = request.CategoriaId,
            Descricao = request.Descricao,
            Valor = -request.Valor, // Converte valor positivo para negativo
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

        _context.Lancamentos.Add(estorno);
        await _context.SaveChangesAsync();

        return (true, estorno, null);
    }
}
