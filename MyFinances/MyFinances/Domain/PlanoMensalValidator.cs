using MyFinances.Exceptions;

namespace MyFinances.Domain;

public record SublimiteEntrada(Guid CategoriaId, decimal Percentual);

// Item 20, "Validacao ao salvar" + item 14 (CRITICA). Funcao PURA; lanca na primeira violacao.
// Ordem de checagem (deterministica, para os testes):
//  1. rendaPrevista < 0                       -> RendaPrevistaNegativaException
//     rendaPrevista > teto numeric(18,2)      -> ValorInvalidoException(rendaPrevista, valor, 0, teto)
//  2. percentualInvestimento fora de 0..100   -> ValorInvalidoException(campo, valor, 0, 100)
//  3. por sublimite, na ordem recebida:
//     a. CategoriaId repetido                 -> SublimiteDuplicadoException
//     b. categoria ausente em categoriasPorId -> CategoriaNaoEncontradaException
//     c. categoria nao Despesa ou arquivada   -> CategoriaInvalidaParaSublimiteException
//     d. percentual fora de 0..100            -> ValorInvalidoException(campo, valor, 0, 100)
//  4. subcategoria (ParentId != null) cujo pai nao esta no conjunto
//                                             -> SubcategoriaSemSublimiteNoPaiException
//  5. soma dos percentuais de primeiro nivel > 100
//                                             -> SomaPercentuaisExcedeCemException(null, soma)
//  6. soma dos percentuais das subcategorias de um mesmo pai > 100
//                                             -> SomaPercentuaisExcedeCemException(paiId, soma)
// Soma == 100 e valida.
public static class PlanoMensalValidator
{
    public const decimal PercentualMinimo = 0m;
    public const decimal PercentualMaximo = 100m;

    // Teto de numeric(18,2) da coluna renda_prevista: 16 digitos inteiros e 2 decimais.
    public const decimal RendaPrevistaMinima = 0m;
    public const decimal RendaPrevistaMaxima = 9_999_999_999_999_999.99m;

    private const string NomeCampoRendaPrevista = "rendaPrevista";
    private const string NomeCampoPercentualSublimite = "percentual";

    public static void Validar(
        decimal rendaPrevista,
        decimal percentualInvestimento,
        IReadOnlyList<SublimiteEntrada> sublimites,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        ValidarRendaPrevista(rendaPrevista);
        ValidarPercentual(nameof(percentualInvestimento), percentualInvestimento);
        ValidarSublimitesIndividuais(sublimites, categoriasPorId);
        ValidarPaiDasSubcategorias(sublimites, categoriasPorId);
        ValidarSomaPrimeiroNivel(sublimites, categoriasPorId);
        ValidarSomaPorPai(sublimites, categoriasPorId);
    }

    private static void ValidarRendaPrevista(decimal rendaPrevista)
    {
        if (rendaPrevista < RendaPrevistaMinima)
            throw new RendaPrevistaNegativaException(rendaPrevista);

        if (rendaPrevista > RendaPrevistaMaxima)
            throw new ValorInvalidoException(NomeCampoRendaPrevista, rendaPrevista, RendaPrevistaMinima, RendaPrevistaMaxima);
    }

    private static void ValidarSublimitesIndividuais(
        IReadOnlyList<SublimiteEntrada> sublimites,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        var categoriasRegistradas = new HashSet<Guid>();

        foreach (var sublimite in sublimites)
        {
            RegistrarCategoriaSemRepeticao(sublimite.CategoriaId, categoriasRegistradas);
            var categoria = ObterCategoria(sublimite.CategoriaId, categoriasPorId);
            ValidarCategoriaElegivelParaSublimite(sublimite.CategoriaId, categoria);
            ValidarPercentual(NomeCampoPercentualSublimite, sublimite.Percentual);
        }
    }

    private static void RegistrarCategoriaSemRepeticao(Guid categoriaId, HashSet<Guid> categoriasRegistradas)
    {
        if (!categoriasRegistradas.Add(categoriaId))
            throw new SublimiteDuplicadoException(categoriaId);
    }

    private static Categoria ObterCategoria(Guid categoriaId, IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        if (!categoriasPorId.TryGetValue(categoriaId, out var categoria))
            throw new CategoriaNaoEncontradaException(categoriaId);

        return categoria;
    }

    private static void ValidarCategoriaElegivelParaSublimite(Guid categoriaId, Categoria categoria)
    {
        if (!EhDespesaAtiva(categoria))
            throw new CategoriaInvalidaParaSublimiteException(categoriaId);
    }

    private static bool EhDespesaAtiva(Categoria categoria) =>
        categoria.Tipo == TipoCategoria.Despesa && !categoria.Arquivada;

    private static void ValidarPercentual(string nomeCampo, decimal percentual)
    {
        if (!EstaEntrePercentuaisValidos(percentual))
            throw new ValorInvalidoException(nomeCampo, percentual, (int)PercentualMinimo, (int)PercentualMaximo);
    }

    private static bool EstaEntrePercentuaisValidos(decimal percentual) =>
        percentual >= PercentualMinimo && percentual <= PercentualMaximo;

    private static void ValidarPaiDasSubcategorias(
        IReadOnlyList<SublimiteEntrada> sublimites,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        foreach (var sublimite in sublimites)
        {
            var paiId = ObterCategoria(sublimite.CategoriaId, categoriasPorId).ParentId;

            if (paiId is Guid idDoPai && !ContemCategoria(sublimites, idDoPai))
                throw new SubcategoriaSemSublimiteNoPaiException(sublimite.CategoriaId, idDoPai);
        }
    }

    private static bool ContemCategoria(IReadOnlyList<SublimiteEntrada> sublimites, Guid categoriaId) =>
        sublimites.Any(sublimite => sublimite.CategoriaId == categoriaId);

    private static void ValidarSomaPrimeiroNivel(
        IReadOnlyList<SublimiteEntrada> sublimites,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        var primeiroNivel = sublimites.Where(sublimite => !TemPai(sublimite, categoriasPorId));
        var soma = SomarPercentuais(primeiroNivel);

        if (soma > PercentualMaximo)
            throw new SomaPercentuaisExcedeCemException(null, soma);
    }

    private static void ValidarSomaPorPai(
        IReadOnlyList<SublimiteEntrada> sublimites,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        var gruposPorPai = sublimites
            .Where(sublimite => TemPai(sublimite, categoriasPorId))
            .GroupBy(sublimite => ObterCategoria(sublimite.CategoriaId, categoriasPorId).ParentId);

        foreach (var grupo in gruposPorPai)
        {
            var soma = SomarPercentuais(grupo);

            if (soma > PercentualMaximo)
                throw new SomaPercentuaisExcedeCemException(grupo.Key, soma);
        }
    }

    private static bool TemPai(SublimiteEntrada sublimite, IReadOnlyDictionary<Guid, Categoria> categoriasPorId) =>
        ObterCategoria(sublimite.CategoriaId, categoriasPorId).ParentId is not null;

    private static decimal SomarPercentuais(IEnumerable<SublimiteEntrada> sublimites) =>
        sublimites.Sum(sublimite => sublimite.Percentual);
}
