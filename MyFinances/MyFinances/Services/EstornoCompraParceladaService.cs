using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

// Estorno de compra parcelada (regra-de-negocio.md item 12, subsecao
// "Estorno de compra parcelada"). Servico separado de EstornoCartaoService
// porque opera sobre compra_parcelada_id (nao lancamento_id isolado),
// cancela N parcelas de uma vez e faz o OPOSTO do que EstornoCartaoService
// permite (aquele proibe estorno em fatura paga; este e o unico caminho
// que alcanca fatura ja paga retroativamente). Ver "Decisoes de
// modelagem" em tasks.md para o tradeoff completo.
public class EstornoCompraParceladaService
{
    private readonly ICompraParceladaRepository _compraParceladaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly ValidacaoCartaoService _validacaoCartaoService;

    public EstornoCompraParceladaService(
        ICompraParceladaRepository compraParceladaRepository,
        ILancamentoRepository lancamentoRepository,
        ValidacaoCartaoService validacaoCartaoService)
    {
        _compraParceladaRepository = compraParceladaRepository;
        _lancamentoRepository = lancamentoRepository;
        _validacaoCartaoService = validacaoCartaoService;
    }

    // Acao unica sobre a compra inteira. Cancela (remove) toda parcela cuja
    // fatura ainda NAO esta paga (ABERTA/FECHADA); para toda parcela cuja
    // fatura JA esta paga, gera um lancamento de estorno (Credit, Pago) na
    // MESMA fatura paga -- nunca estorno de parcela isolada, nunca altera
    // o status da fatura paga. Idempotente: chamar duas vezes na mesma
    // compra_parcelada nao duplica o lancamento de estorno de uma parcela
    // ja estornada. O credito retroativo gerado e abatido na proxima
    // fatura em aberto por FaturaCreditoService, sem nenhuma acao deste
    // metodo sobre outras faturas.
    public async Task<(bool Sucesso, IReadOnlyList<Lancamento>? ParcelasCanceladas, IReadOnlyList<Lancamento>? EstornosRetroativos, string? Erro)> EstornarCompraParceladaAsync(
        Guid contaId,
        Guid compraParceladaId,
        EstornarCompraParceladaRequest request)
    {
        var compra = await _compraParceladaRepository.ObterPorId(compraParceladaId);
        if (compra == null)
        {
            return (false, null, null, "Compra parcelada nao encontrada");
        }

        // Validar que todas as parcelas pertencem a mesma conta
        if (compra.Lancamentos.Any(l => l.ContaId != contaId))
        {
            return (false, null, null, "Compra parcelada nao pertence a esta conta");
        }

        var (valido, conta, erro) = await _validacaoCartaoService.ValidarOperacaoCartaoAsync(
            contaId,
            $"Estorno de compra parcelada: {request.Motivo}",
            compra.ValorTotal);

        if (!valido)
        {
            return (false, null, null, erro);
        }

        var canceladas = new List<Lancamento>();
        var estornos = new List<Lancamento>();

        using (var transaction = await _compraParceladaRepository.BeginTransactionAsync())
        {
            try
            {
                // Iterar apenas sobre parcelas originais (Debit), nao sobre estornos ja criados (Credit)
                var parcelasOriginais = compra.Lancamentos
                    .Where(l => l.Tipo == TipoLancamento.Debit)
                    .ToList();

                foreach (var parcela in parcelasOriginais)
                {
                    if (parcela.Fatura == null)
                    {
                        continue;
                    }

                    if (parcela.Fatura.Status == StatusFatura.Paga)
                    {
                        // Gerar estorno retroativo
                        var estorno = CriarEstornoRetroativo(parcela, request);

                        // Verificar idempotencia: se ja existe estorno com mesmo CompraParceladaId e ParcelaNumero
                        var estornoExistente = compra.Lancamentos.FirstOrDefault(l =>
                            l.Tipo == TipoLancamento.Credit &&
                            l.CompraParceladaId == compraParceladaId &&
                            l.ParcelaNumero == parcela.ParcelaNumero &&
                            l.FaturaId == parcela.FaturaId);

                        if (estornoExistente == null)
                        {
                            await _lancamentoRepository.Adicionar(estorno);
                            estornos.Add(estorno);
                            // Mantem a colecao em memoria da compra em sincronia:
                            // uma chamada subsequente que reutilize esta mesma
                            // instancia de CompraParcelada (ex: dentro do mesmo
                            // escopo/unit of work) deve enxergar o estorno recem
                            // criado e nao duplica-lo.
                            compra.Lancamentos.Add(estorno);
                        }
                        else
                        {
                            // Chamada idempotente: nao cria duplicado, mas o
                            // retorno reflete o estorno ja existente (resposta
                            // consistente entre a 1a e as chamadas seguintes).
                            estornos.Add(estornoExistente);
                        }
                    }
                    else
                    {
                        // Cancelar parcela em fatura nao paga
                        await _lancamentoRepository.Remover(parcela);
                        canceladas.Add(parcela);
                    }
                }

                await _lancamentoRepository.Salvar();
                await _compraParceladaRepository.CommitAsync();

                return (true, canceladas.AsReadOnly(), estornos.AsReadOnly(), null);
            }
            catch
            {
                await _compraParceladaRepository.RollbackAsync();
                throw;
            }
        }
    }

    private Lancamento CriarEstornoRetroativo(Lancamento parcelaOriginal, EstornarCompraParceladaRequest request)
    {
        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = parcelaOriginal.ContaId,
            Conta = parcelaOriginal.Conta,
            CategoriaId = parcelaOriginal.CategoriaId,
            Categoria = parcelaOriginal.Categoria,
            Descricao = $"Estorno: {request.Motivo}",
            Valor = parcelaOriginal.Valor,
            Tipo = TipoLancamento.Credit,
            Data = request.Data,
            Status = StatusLancamento.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = parcelaOriginal.FaturaId,
            Fatura = parcelaOriginal.Fatura,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null,
            ContaReceberId = null,
            CompraParceladaId = parcelaOriginal.CompraParceladaId,
            ParcelaNumero = parcelaOriginal.ParcelaNumero
        };
    }
}
