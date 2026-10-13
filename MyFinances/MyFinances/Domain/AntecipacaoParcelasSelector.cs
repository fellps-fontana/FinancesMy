namespace MyFinances.Domain;

// Selecionadas vem em ordem de ANTECIPACAO (maior parcela_numero primeiro);
// vazia quando Sucesso = false.
public record ResultadoSelecaoAntecipacao(
    bool Sucesso,
    IReadOnlyList<Lancamento> Selecionadas,
    string? Erro);

// Regra pura de antecipacao (item 12, "Antecipacao de parcelas"). Sem I/O:
// recebe as parcelas ja carregadas (com Fatura) e a fatura destino. Nao muta nada.
public static class AntecipacaoParcelasSelector
{
    private const int QuantidadeMinima = 1;

    private const string ErroQuantidadeInvalida = "Quantidade de parcelas para antecipar deve ser maior ou igual a 1";
    private const string ErroQuantidadeAcimaDoElegivel = "Quantidade de parcelas para antecipar maior que o numero de parcelas elegiveis";

    // Elegivel = parcela Debit com ParcelaNumero cuja Fatura e POSTERIOR a
    // faturaAbertaAtual (Fatura.DataVencimento maior). Parcela na aberta atual,
    // em fatura anterior, sem fatura, ou Credit (estorno) nao e elegivel.
    // Ordem: ParcelaNumero decrescente.
    public static IReadOnlyList<Lancamento> ObterElegiveis(
        IEnumerable<Lancamento> parcelasDaCompra,
        Fatura faturaAbertaAtual)
    {
        return parcelasDaCompra
            .Where(parcela => EhElegivel(parcela, faturaAbertaAtual))
            .OrderByDescending(parcela => parcela.ParcelaNumero)
            .ToList()
            .AsReadOnly();
    }

    // quantidade < 1 -> rejeita. quantidade > elegiveis -> rejeita INTEIRO.
    // Senao devolve as `quantidade` primeiras de ObterElegiveis.
    public static ResultadoSelecaoAntecipacao Selecionar(
        IEnumerable<Lancamento> parcelasDaCompra,
        Fatura faturaAbertaAtual,
        int quantidade)
    {
        if (quantidade < QuantidadeMinima)
        {
            return Rejeitar(ErroQuantidadeInvalida);
        }

        var elegiveis = ObterElegiveis(parcelasDaCompra, faturaAbertaAtual);
        if (quantidade > elegiveis.Count)
        {
            return Rejeitar(ErroQuantidadeAcimaDoElegivel);
        }

        var selecionadas = elegiveis.Take(quantidade).ToList().AsReadOnly();
        return new ResultadoSelecaoAntecipacao(true, selecionadas, null);
    }

    private static bool EhElegivel(Lancamento parcela, Fatura faturaAbertaAtual)
    {
        return parcela.Tipo == TipoLancamento.Debit
            && parcela.ParcelaNumero.HasValue
            && parcela.Fatura != null
            && parcela.Fatura.DataVencimento > faturaAbertaAtual.DataVencimento;
    }

    private static ResultadoSelecaoAntecipacao Rejeitar(string erro)
    {
        return new ResultadoSelecaoAntecipacao(false, Array.Empty<Lancamento>(), erro);
    }
}
