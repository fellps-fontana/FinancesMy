namespace MyFinances.Domain;

// Item 19. Funcao pura. Calcular (lista) e LancamentosDaLinha (detalhe) compartilham
// FiltrarElegiveis e ResolverRaiz, entao soma(detalhe) == valor da linha.
// NAO reutiliza LimiteGastoCalculator (ele nao filtra transferencia nem status).
public static class GastoPorCategoriaCalculator
{
    public const string NomeSemCategoria = "Sem categoria";

    private const int CasasDecimaisPercentual = 2;
    private const decimal PercentualMaximo = 100m;

    // Status que compoem o gasto do relatorio (item 19). Compartilhado com a consulta do repositorio.
    public static readonly StatusLancamento[] StatusElegiveis =
    {
        StatusLancamento.Pendente,
        StatusLancamento.Pago
    };

    // Elegivel: Tipo == Debit, !Oculto, TransferenciaId == null (exclui transferencia e
    // pagamento de fatura), Status Pendente ou Pago. Estorno de cartao (Credit) fica fora.
    // "categorias" = TODAS as categorias (qualquer tipo, arquivadas inclusive).
    public static ResultadoGastoPorCategoria Calcular(
        IEnumerable<Lancamento> lancamentosDoMes,
        IEnumerable<Categoria> categorias)
    {
        var categoriasPorId = IndexarPorId(categorias);
        var somaPorRaiz = SomarPorRaiz(FiltrarElegiveis(lancamentosDoMes), categoriasPorId);
        var total = somaPorRaiz.Sum(item => item.Valor);

        var linhas = somaPorRaiz
            .OrderByDescending(item => item.Valor)
            .Select(item => MontarLinha(item.RaizId, item.Valor, total, categoriasPorId))
            .ToList();

        return new ResultadoGastoPorCategoria(total, linhas);
    }

    // Lancamentos elegiveis cuja raiz (primeiro nivel) == categoriaRaizId.
    // categoriaRaizId == null => elegiveis com CategoriaId == null ("Sem categoria").
    // Ordem crescente de Data (desempate por Id).
    public static IReadOnlyList<Lancamento> LancamentosDaLinha(
        IEnumerable<Lancamento> lancamentosDoMes,
        IEnumerable<Categoria> categorias,
        Guid? categoriaRaizId)
    {
        var categoriasPorId = IndexarPorId(categorias);

        return AssociarRaiz(FiltrarElegiveis(lancamentosDoMes), categoriasPorId)
            .Where(item => item.RaizId == categoriaRaizId)
            .Select(item => item.Lancamento)
            .OrderBy(lancamento => lancamento.Data)
            .ThenBy(lancamento => lancamento.Id)
            .ToList();
    }

    // Percentual 0-100, 2 casas, AwayFromZero. total <= 0 => 0.
    public static decimal CalcularPercentual(decimal valor, decimal total)
    {
        if (total <= 0m)
            return 0m;

        var percentual = valor * PercentualMaximo / total;
        return Math.Round(percentual, CasasDecimaisPercentual, MidpointRounding.AwayFromZero);
    }

    private static Dictionary<Guid, Categoria> IndexarPorId(IEnumerable<Categoria> categorias) =>
        categorias.ToDictionary(categoria => categoria.Id);

    private static IEnumerable<Lancamento> FiltrarElegiveis(IEnumerable<Lancamento> lancamentos) =>
        lancamentos.Where(EhElegivel);

    private static bool EhElegivel(Lancamento lancamento) =>
        lancamento.Tipo == TipoLancamento.Debit
        && !lancamento.Oculto
        && lancamento.TransferenciaId == null
        && StatusElegiveis.Contains(lancamento.Status);

    // Associa cada lancamento a sua raiz. Lancamento cuja raiz nao e Despesa (ou fora da lista)
    // e descartado; sem categoria vira RaizId null.
    private static IEnumerable<(Lancamento Lancamento, Guid? RaizId)> AssociarRaiz(
        IEnumerable<Lancamento> elegiveis,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        foreach (var lancamento in elegiveis)
        {
            if (lancamento.CategoriaId == null)
            {
                yield return (lancamento, null);
                continue;
            }

            var raiz = ResolverRaiz(lancamento.CategoriaId.Value, categoriasPorId);
            if (raiz != null)
                yield return (lancamento, raiz.Id);
        }
    }

    private static IReadOnlyList<(Guid? RaizId, decimal Valor)> SomarPorRaiz(
        IEnumerable<Lancamento> elegiveis,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        return AssociarRaiz(elegiveis, categoriasPorId)
            .GroupBy(item => item.RaizId)
            .Select(grupo => (RaizId: grupo.Key, Valor: grupo.Sum(item => item.Lancamento.Valor)))
            .Where(item => item.Valor > 0m)
            .ToList();
    }

    private static LinhaGastoCategoria MontarLinha(
        Guid? raizId,
        decimal valor,
        decimal total,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        var percentual = CalcularPercentual(valor, total);

        if (raizId == null)
            return new LinhaGastoCategoria(null, NomeSemCategoria, null, valor, percentual);

        var raiz = categoriasPorId[raizId.Value];
        return new LinhaGastoCategoria(raiz.Id, raiz.Nome, raiz.Icone, valor, percentual);
    }

    // Sobe pelo ParentId ate o primeiro nivel. Null se a categoria nao existir na lista
    // ou se a raiz nao for TipoCategoria.Despesa (lancamento ignorado).
    private static Categoria? ResolverRaiz(Guid categoriaId, IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        if (!categoriasPorId.TryGetValue(categoriaId, out var atual))
            return null;

        while (atual.ParentId != null)
        {
            if (!categoriasPorId.TryGetValue(atual.ParentId.Value, out var pai))
                return null;

            atual = pai;
        }

        return atual.Tipo == TipoCategoria.Despesa ? atual : null;
    }
}

public record LinhaGastoCategoria(Guid? CategoriaId, string Nome, string? Icone, decimal Valor, decimal Percentual);

public record ResultadoGastoPorCategoria(decimal Total, IReadOnlyList<LinhaGastoCategoria> Linhas);
