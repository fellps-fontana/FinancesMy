namespace MyFinances.Domain;

// Item 20 (CRITICA - calculo). Funcao PURA: nao persiste, nao le relogio. Aplica TODOS os filtros
// por conta propria (nao confia na query), como GastoPorCategoriaCalculator.
//
// Base elegivel: Tipo == Debit, !Oculto, TransferenciaId == null, Status Pendente ou Pago
// (GastoPorCategoriaCalculator.StatusElegiveis), Data no ano/mes do plano.
//   - EhFixa:           elegivel E (ContaFixaId != null OU AssinaturaCartaoId != null)
//   - EhGastoVariavel:  elegivel E ContaFixaId == null E AssinaturaCartaoId == null
// Cada despesa cai em exatamente um balde: fixas OU sublimite/sem sublimite.
public static class PlanejamentoMensalCalculator
{
    private const decimal BasePercentual = 100m;

    private static readonly StringComparer OrdemDosNomes = StringComparer.CurrentCultureIgnoreCase;

    // Compoe todos os termos. Categorias = TODAS (qualquer tipo, arquivadas inclusive).
    public static PlanoMensalCalculado Calcular(
        PlanoMensal plano,
        IEnumerable<Lancamento> debitosDoMes,
        IEnumerable<Categoria> categorias)
    {
        var lancamentos = debitosDoMes.ToList();
        var listaCategorias = categorias.ToList();
        var renda = plano.RendaPrevista;
        var fixas = CalcularFixas(lancamentos, plano.Ano, plano.Mes);
        var investimentoPlanejado = CalcularInvestimentoPlanejado(renda, plano.PercentualInvestimento);
        var investimento = CalcularInvestimento(renda, fixas, investimentoPlanejado);
        var sobra = CalcularSobra(renda, fixas, investimento);
        var sublimites = CalcularSublimites(plano, lancamentos, listaCategorias, sobra);
        var valoresPrimeiroNivel = sublimites
            .Where(sublimite => sublimite.ParentId == null)
            .Select(sublimite => sublimite.Valor);
        var categoriasComSublimite = plano.Sublimites.Select(sublimite => sublimite.CategoriaId).ToHashSet();

        return new PlanoMensalCalculado(
            plano.Ano,
            plano.Mes,
            renda,
            fixas,
            plano.PercentualInvestimento,
            investimentoPlanejado,
            investimento,
            EhInvestimentoReduzido(investimento, investimentoPlanejado),
            EhPlanoNaoFecha(renda, fixas),
            sobra,
            CalcularNaoAlocado(sobra, valoresPrimeiroNivel),
            CalcularGastoSemSublimite(lancamentos, listaCategorias, categoriasComSublimite, plano.Ano, plano.Mes),
            sublimites,
            CalcularGastosPorCategoria(lancamentos, listaCategorias, plano.Ano, plano.Mes));
    }

    public static bool EhFixa(Lancamento lancamento, int ano, int mes) =>
        EhElegivelNoMes(lancamento, ano, mes)
        && (lancamento.ContaFixaId != null || lancamento.AssinaturaCartaoId != null);

    public static bool EhGastoVariavel(Lancamento lancamento, int ano, int mes) =>
        EhElegivelNoMes(lancamento, ano, mes)
        && lancamento.ContaFixaId == null
        && lancamento.AssinaturaCartaoId == null;

    // fixas = soma(valor) dos lancamentos EhFixa.
    public static decimal CalcularFixas(IEnumerable<Lancamento> lancamentos, int ano, int mes) =>
        lancamentos.Where(lancamento => EhFixa(lancamento, ano, mes)).Sum(lancamento => lancamento.Valor);

    // renda * percentualInvestimento / 100.
    public static decimal CalcularInvestimentoPlanejado(decimal renda, decimal percentualInvestimento) =>
        renda * percentualInvestimento / BasePercentual;

    // max(0, min(investimentoPlanejado, renda - fixas)). Renda - fixas < 0 => 0.
    public static decimal CalcularInvestimento(decimal renda, decimal fixas, decimal investimentoPlanejado) =>
        Math.Max(0m, Math.Min(investimentoPlanejado, renda - fixas));

    // max(0, renda - fixas - investimento).
    public static decimal CalcularSobra(decimal renda, decimal fixas, decimal investimento) =>
        Math.Max(0m, renda - fixas - investimento);

    // sobra * percentual / 100.
    public static decimal CalcularSublimitePrimeiroNivel(decimal sobra, decimal percentual) =>
        sobra * percentual / BasePercentual;

    // sublimitePai * percentual / 100.
    public static decimal CalcularSublimiteSubcategoria(decimal sublimitePai, decimal percentual) =>
        sublimitePai * percentual / BasePercentual;

    // sobra - soma(sublimites de primeiro nivel em R$).
    public static decimal CalcularNaoAlocado(decimal sobra, IEnumerable<decimal> sublimitesPrimeiroNivel) =>
        sobra - sublimitesPrimeiroNivel.Sum();

    // Soma dos EhGastoVariavel cuja CategoriaId esta em categoriaIds.
    // Primeiro nivel: [propria + subcategorias diretas]. Subcategoria: [ela mesma].
    public static decimal CalcularGastoDoSublimite(
        IEnumerable<Lancamento> lancamentos,
        IReadOnlyCollection<Guid> categoriaIds,
        int ano,
        int mes)
    {
        var idsElegiveis = categoriaIds.ToHashSet();

        return lancamentos
            .Where(lancamento => EhGastoVariavel(lancamento, ano, mes)
                && lancamento.CategoriaId is Guid categoriaId
                && idsElegiveis.Contains(categoriaId))
            .Sum(lancamento => lancamento.Valor);
    }

    // Gasto variavel do mes (item 14) para cada categoria DESPESA (ativas e arquivadas, 1o nivel e subcategorias).
    // Primeiro nivel soma propria + subcategorias diretas; subcategoria soma apenas ela mesma.
    // Ordenado por CategoriaId.
    public static IReadOnlyList<GastoCategoriaCalculado> CalcularGastosPorCategoria(
        IEnumerable<Lancamento> lancamentos,
        IEnumerable<Categoria> categorias,
        int ano,
        int mes)
    {
        var listaLancamentos = lancamentos.ToList();
        var listaCategorias = categorias.ToList();

        return listaCategorias
            .Where(categoria => categoria.Tipo == TipoCategoria.Despesa)
            .OrderBy(categoria => categoria.Id)
            .Select(categoria => new GastoCategoriaCalculado(
                categoria.Id,
                CalcularGastoDoSublimite(
                    listaLancamentos,
                    IdsDoGastoDoSublimite(categoria, listaCategorias),
                    ano,
                    mes)))
            .ToList();
    }

    // Soma dos EhGastoVariavel cuja categoria e null OU nem ela nem o pai direto dela
    // esta em categoriasComSublimite.
    public static decimal CalcularGastoSemSublimite(
        IEnumerable<Lancamento> lancamentos,
        IEnumerable<Categoria> categorias,
        IReadOnlySet<Guid> categoriasComSublimite,
        int ano,
        int mes)
    {
        var categoriasPorId = categorias.ToDictionary(categoria => categoria.Id);

        return lancamentos
            .Where(lancamento => EhGastoVariavel(lancamento, ano, mes)
                && EhSemSublimite(lancamento.CategoriaId, categoriasPorId, categoriasComSublimite))
            .Sum(lancamento => lancamento.Valor);
    }

    private static bool EhElegivelNoMes(Lancamento lancamento, int ano, int mes) =>
        lancamento.Tipo == TipoLancamento.Debit
        && !lancamento.Oculto
        && lancamento.TransferenciaId == null
        && GastoPorCategoriaCalculator.StatusElegiveis.Contains(lancamento.Status)
        && lancamento.Data.Year == ano
        && lancamento.Data.Month == mes;

    private static bool EhSemSublimite(
        Guid? categoriaId,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId,
        IReadOnlySet<Guid> categoriasComSublimite)
    {
        if (categoriaId == null)
            return true;

        if (categoriasComSublimite.Contains(categoriaId.Value))
            return false;

        if (!categoriasPorId.TryGetValue(categoriaId.Value, out var categoria))
            return true;

        return categoria.ParentId == null || !categoriasComSublimite.Contains(categoria.ParentId.Value);
    }

    private static bool EhInvestimentoReduzido(decimal investimento, decimal investimentoPlanejado) =>
        investimento < investimentoPlanejado;

    private static bool EhPlanoNaoFecha(decimal renda, decimal fixas) => renda - fixas < 0m;

    private static bool EhPrimeiroNivel(Categoria categoria) => categoria.ParentId == null;

    private static bool EhEstourado(decimal gastoRealizado, decimal valor) => gastoRealizado > valor;

    private static decimal CalcularFracaoUtilizada(decimal gastoRealizado, decimal valor) =>
        valor == 0m ? 0m : gastoRealizado / valor;

    private static IReadOnlyList<SublimiteCalculado> CalcularSublimites(
        PlanoMensal plano,
        IReadOnlyList<Lancamento> lancamentos,
        IReadOnlyList<Categoria> categorias,
        decimal sobra)
    {
        var planejados = ObterSublimitesPlanejados(plano, categorias);
        var valoresPrimeiroNivel = CalcularValoresPrimeiroNivel(planejados, sobra);

        return OrdenarPorHierarquia(planejados)
            .Select(item => MontarSublimite(
                item,
                CalcularValorDoSublimite(item, sobra, valoresPrimeiroNivel),
                plano,
                lancamentos,
                categorias))
            .ToList();
    }

    // Sublimite cuja categoria nao existe na lista de categorias e ignorado.
    private static IReadOnlyList<SublimitePlanejado> ObterSublimitesPlanejados(
        PlanoMensal plano,
        IReadOnlyList<Categoria> categorias)
    {
        var categoriasPorId = categorias.ToDictionary(categoria => categoria.Id);

        return plano.Sublimites
            .Where(sublimite => categoriasPorId.ContainsKey(sublimite.CategoriaId))
            .Select(sublimite => new SublimitePlanejado(categoriasPorId[sublimite.CategoriaId], sublimite.Percentual))
            .ToList();
    }

    private static IReadOnlyDictionary<Guid, decimal> CalcularValoresPrimeiroNivel(
        IEnumerable<SublimitePlanejado> planejados,
        decimal sobra) =>
        planejados
            .Where(item => EhPrimeiroNivel(item.Categoria))
            .ToDictionary(
                item => item.Categoria.Id,
                item => CalcularSublimitePrimeiroNivel(sobra, item.Percentual));

    private static decimal CalcularValorDoSublimite(
        SublimitePlanejado item,
        decimal sobra,
        IReadOnlyDictionary<Guid, decimal> valoresPrimeiroNivel)
    {
        if (EhPrimeiroNivel(item.Categoria))
            return CalcularSublimitePrimeiroNivel(sobra, item.Percentual);

        return CalcularSublimiteSubcategoria(ValorDoPai(item.Categoria, valoresPrimeiroNivel), item.Percentual);
    }

    // Pai sem sublimite no plano nao tem valor: subcategoria fica em 0 (a validacao impede esse estado).
    private static decimal ValorDoPai(Categoria subcategoria, IReadOnlyDictionary<Guid, decimal> valoresPrimeiroNivel) =>
        subcategoria.ParentId is Guid paiId && valoresPrimeiroNivel.TryGetValue(paiId, out var valor)
            ? valor
            : 0m;

    // Primeiro nivel por Nome; cada subcategoria logo apos o pai, por Nome.
    // Subcategoria sem pai no plano vai ao fim, por Nome.
    private static IEnumerable<SublimitePlanejado> OrdenarPorHierarquia(IReadOnlyList<SublimitePlanejado> planejados)
    {
        var primeirosNiveis = planejados
            .Where(item => EhPrimeiroNivel(item.Categoria))
            .OrderBy(item => item.Categoria.Nome, OrdemDosNomes)
            .ToList();
        var subcategorias = planejados
            .Where(item => !EhPrimeiroNivel(item.Categoria))
            .OrderBy(item => item.Categoria.Nome, OrdemDosNomes)
            .ToList();
        var idsPrimeirosNiveis = primeirosNiveis.Select(item => item.Categoria.Id).ToHashSet();

        foreach (var pai in primeirosNiveis)
        {
            yield return pai;

            foreach (var filha in subcategorias.Where(item => item.Categoria.ParentId == pai.Categoria.Id))
                yield return filha;
        }

        foreach (var orfa in subcategorias.Where(item => !TemPaiNoPlano(item, idsPrimeirosNiveis)))
            yield return orfa;
    }

    private static bool TemPaiNoPlano(SublimitePlanejado item, IReadOnlySet<Guid> idsPrimeirosNiveis) =>
        item.Categoria.ParentId is Guid paiId && idsPrimeirosNiveis.Contains(paiId);

    private static SublimiteCalculado MontarSublimite(
        SublimitePlanejado item,
        decimal valor,
        PlanoMensal plano,
        IReadOnlyList<Lancamento> lancamentos,
        IReadOnlyList<Categoria> categorias)
    {
        var categoria = item.Categoria;
        var gastoRealizado = CalcularGastoDoSublimite(
            lancamentos,
            IdsDoGastoDoSublimite(categoria, categorias),
            plano.Ano,
            plano.Mes);

        return new SublimiteCalculado(
            categoria.Id,
            categoria.ParentId,
            categoria.Nome,
            categoria.Icone,
            categoria.Arquivada,
            item.Percentual,
            valor,
            gastoRealizado,
            CalcularFracaoUtilizada(gastoRealizado, valor),
            EhEstourado(gastoRealizado, valor));
    }

    private static IReadOnlyCollection<Guid> IdsDoGastoDoSublimite(Categoria categoria, IReadOnlyList<Categoria> categorias)
    {
        if (!EhPrimeiroNivel(categoria))
            return [categoria.Id];

        return categorias
            .Where(candidata => candidata.ParentId == categoria.Id)
            .Select(candidata => candidata.Id)
            .Prepend(categoria.Id)
            .ToList();
    }

    private sealed record SublimitePlanejado(Categoria Categoria, decimal Percentual);
}
