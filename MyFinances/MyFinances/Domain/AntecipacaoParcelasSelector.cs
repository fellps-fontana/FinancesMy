namespace MyFinances.Domain;

// Selecionadas vem em ordem de ANTECIPACAO (maior parcela_numero primeiro);
// vazia quando Sucesso = false.
public record ResultadoSelecaoAntecipacao(
    bool Sucesso,
    IReadOnlyList<Lancamento> Selecionadas,
    string? Erro);

// Regra pura de antecipacao (item 12, "Antecipacao de parcelas"). Sem I/O:
// recebe as parcelas ja carregadas (com Fatura) e a fatura atual. Nao muta nada.
public static class AntecipacaoParcelasSelector
{
    private const int QuantidadeMinima = 1;

    private const string ErroQuantidadeInvalida = "Quantidade de parcelas para antecipar deve ser maior ou igual a 1";
    private const string ErroQuantidadeAcimaDoElegivel = "Quantidade de parcelas para antecipar maior que o numero de parcelas elegiveis";

    // Elegivel = parcela Debit com ParcelaNumero, em fatura POSTERIOR a atual
    // (Fatura.DataVencimento maior) e que nao esta PAGA. Parcela na fatura atual,
    // em fatura anterior, PAGA, sem fatura, ou Credit (estorno) nao e elegivel.
    // Ordem: ParcelaNumero decrescente.
    public static IReadOnlyList<Lancamento> ObterElegiveis(
        IEnumerable<Lancamento> parcelasDaCompra,
        Fatura faturaAtual)
    {
        return parcelasDaCompra
            .Where(parcela => EhElegivel(parcela, faturaAtual))
            .OrderByDescending(parcela => parcela.ParcelaNumero)
            .ToList()
            .AsReadOnly();
    }

    // quantidade < 1 -> rejeita. quantidade > elegiveis -> rejeita INTEIRO.
    // Senao devolve as `quantidade` primeiras de ObterElegiveis.
    public static ResultadoSelecaoAntecipacao Selecionar(
        IEnumerable<Lancamento> parcelasDaCompra,
        Fatura faturaAtual,
        int quantidade)
    {
        if (quantidade < QuantidadeMinima)
        {
            return Rejeitar(ErroQuantidadeInvalida);
        }

        var elegiveis = ObterElegiveis(parcelasDaCompra, faturaAtual);
        if (quantidade > elegiveis.Count)
        {
            return Rejeitar(ErroQuantidadeAcimaDoElegivel);
        }

        var selecionadas = elegiveis.Take(quantidade).ToList().AsReadOnly();
        return new ResultadoSelecaoAntecipacao(true, selecionadas, null);
    }

    private static bool EhElegivel(Lancamento parcela, Fatura faturaAtual)
    {
        return parcela.Tipo == TipoLancamento.Debit
            && parcela.ParcelaNumero.HasValue
            && parcela.Fatura != null
            && parcela.Fatura.DataVencimento > faturaAtual.DataVencimento
            && parcela.Fatura.Status != StatusFatura.Paga;
    }

    private static ResultadoSelecaoAntecipacao Rejeitar(string erro)
    {
        return new ResultadoSelecaoAntecipacao(false, Array.Empty<Lancamento>(), erro);
    }
}
