namespace MyFinances.Domain;

// Item 20, "Renda prevista" e "Ciclo do mes". Funcao PURA.
public static class PlanoMensalInicialFactory
{
    // Soma, para cada molde com Ativa == true, molde.Valor * (numero de ocorrencias do molde no
    // mes ano/mes), via RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo
    // (1o ao ultimo dia do mes). Materializadas ou nao. Molde inativo nao conta.
    public static decimal CalcularRendaPrevistaInicial(
        IEnumerable<RecebivelRecorrente> moldes,
        int ano,
        int mes)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = new DateOnly(ano, mes, DateTime.DaysInMonth(ano, mes));

        return moldes
            .Where(molde => molde.Ativa)
            .Sum(molde => ValorNoIntervalo(molde, inicio, fim));
    }

    // Plano novo (Id novo) com Ano/Mes e RendaPrevista informados. Sem planoAnterior:
    // PercentualInvestimento 0 e sem sublimites. Com planoAnterior: copia PercentualInvestimento
    // e os sublimites (so Percentual), EXCETO categoria arquivada (ou ausente em categoriasPorId)
    // e subcategoria cujo pai ficou de fora da copia (invariante do item 14).
    public static PlanoMensal Criar(
        int ano,
        int mes,
        decimal rendaPrevista,
        PlanoMensal? planoAnterior,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        var plano = new PlanoMensal
        {
            Id = Guid.NewGuid(),
            Ano = ano,
            Mes = mes,
            RendaPrevista = rendaPrevista,
            PercentualInvestimento = 0m
        };

        if (planoAnterior is null)
        {
            return plano;
        }

        plano.PercentualInvestimento = planoAnterior.PercentualInvestimento;
        CopiarSublimitesCopiaveis(plano, planoAnterior, categoriasPorId);
        return plano;
    }

    private static decimal ValorNoIntervalo(RecebivelRecorrente molde, DateOnly inicio, DateOnly fim)
    {
        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);
        return molde.Valor * ocorrencias.Count;
    }

    private static void CopiarSublimitesCopiaveis(
        PlanoMensal plano,
        PlanoMensal planoAnterior,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        foreach (var sublimite in planoAnterior.Sublimites)
        {
            if (SublimiteCopiavel(sublimite, categoriasPorId))
            {
                plano.Sublimites.Add(CopiarSublimite(sublimite, plano.Id));
            }
        }
    }

    private static bool SublimiteCopiavel(
        PlanoMensalSublimite sublimite,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        return categoriasPorId.TryGetValue(sublimite.CategoriaId, out var categoria)
            && CategoriaCopiavel(categoria, categoriasPorId);
    }

    private static bool CategoriaCopiavel(
        Categoria categoria,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        if (categoria.Arquivada)
        {
            return false;
        }

        if (categoria.ParentId is null)
        {
            return true;
        }

        return categoriasPorId.TryGetValue(categoria.ParentId.Value, out var pai) && !pai.Arquivada;
    }

    private static PlanoMensalSublimite CopiarSublimite(PlanoMensalSublimite origem, Guid planoId)
    {
        return new PlanoMensalSublimite
        {
            Id = Guid.NewGuid(),
            PlanoMensalId = planoId,
            CategoriaId = origem.CategoriaId,
            Percentual = origem.Percentual
        };
    }
}
