using MyFinances.Domain;
using MyFinances.Exceptions;
using Xunit;

namespace MyFinances.Tests.Domain;

// Regra 20 (validacao ao salvar do planejamento mensal) e regra 14 (sublimite por categoria).
// Ordem de checagem documentada no cabecalho de PlanoMensalValidator.
public class PlanoMensalValidatorTests
{
    #region Helpers

    private static Categoria Despesa(Guid id, Guid? parentId = null, bool arquivada = false) => new()
    {
        Id = id,
        Nome = "Despesa de teste",
        Tipo = TipoCategoria.Despesa,
        ParentId = parentId,
        Arquivada = arquivada
    };

    private static Categoria Receita(Guid id) => new()
    {
        Id = id,
        Nome = "Receita de teste",
        Tipo = TipoCategoria.Receita
    };

    private static IReadOnlyDictionary<Guid, Categoria> Mapa(params Categoria[] categorias) =>
        categorias.ToDictionary(categoria => categoria.Id);

    private static SublimiteEntrada Sublimite(Guid categoriaId, decimal percentual) =>
        new(categoriaId, percentual);

    #endregion

    #region Renda prevista (regra 20: renda_prevista >= 0)

    [Fact]
    public void Validar_RendaNegativaUmCentavo_LancaRendaPrevistaNegativaException()
    {
        var ex = Assert.Throws<RendaPrevistaNegativaException>(() =>
            PlanoMensalValidator.Validar(-0.01m, 0m, Array.Empty<SublimiteEntrada>(), Mapa()));

        Assert.Equal(-0.01m, ex.Valor);
    }

    [Fact]
    public void Validar_RendaZero_NaoLanca()
    {
        PlanoMensalValidator.Validar(0m, 0m, Array.Empty<SublimiteEntrada>(), Mapa());
    }

    #endregion

    #region Percentual de investimento (regra 20: 0 a 100)

    [Fact]
    public void Validar_PercentualInvestimentoMenosUmCentavo_LancaValorInvalidoException()
    {
        var ex = Assert.Throws<ValorInvalidoException>(() =>
            PlanoMensalValidator.Validar(1000m, -0.01m, Array.Empty<SublimiteEntrada>(), Mapa()));

        Assert.Equal(-0.01m, ex.Valor);
    }

    [Fact]
    public void Validar_PercentualInvestimentoZero_NaoLanca()
    {
        PlanoMensalValidator.Validar(1000m, 0m, Array.Empty<SublimiteEntrada>(), Mapa());
    }

    [Fact]
    public void Validar_PercentualInvestimentoCem_NaoLanca()
    {
        PlanoMensalValidator.Validar(1000m, 100m, Array.Empty<SublimiteEntrada>(), Mapa());
    }

    [Fact]
    public void Validar_PercentualInvestimentoCemUmCentavo_LancaValorInvalidoException()
    {
        var ex = Assert.Throws<ValorInvalidoException>(() =>
            PlanoMensalValidator.Validar(1000m, 100.01m, Array.Empty<SublimiteEntrada>(), Mapa()));

        Assert.Equal(100.01m, ex.Valor);
    }

    #endregion

    #region Percentual de sublimite (regra 20: cada percentual entre 0 e 100)

    [Fact]
    public void Validar_PercentualSublimiteMenosUmCentavo_LancaValorInvalidoException()
    {
        var categoriaId = Guid.NewGuid();

        var ex = Assert.Throws<ValorInvalidoException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(categoriaId, -0.01m) },
                Mapa(Despesa(categoriaId))));

        Assert.Equal(-0.01m, ex.Valor);
    }

    [Fact]
    public void Validar_PercentualSublimiteZero_NaoLanca()
    {
        var categoriaId = Guid.NewGuid();

        PlanoMensalValidator.Validar(1000m, 0m,
            new[] { Sublimite(categoriaId, 0m) },
            Mapa(Despesa(categoriaId)));
    }

    [Fact]
    public void Validar_PercentualSublimiteCem_NaoLanca()
    {
        var categoriaId = Guid.NewGuid();

        PlanoMensalValidator.Validar(1000m, 0m,
            new[] { Sublimite(categoriaId, 100m) },
            Mapa(Despesa(categoriaId)));
    }

    [Fact]
    public void Validar_PercentualSublimiteCemUmCentavo_LancaValorInvalidoException()
    {
        var categoriaId = Guid.NewGuid();

        var ex = Assert.Throws<ValorInvalidoException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(categoriaId, 100.01m) },
                Mapa(Despesa(categoriaId))));

        Assert.Equal(100.01m, ex.Valor);
    }

    #endregion

    #region Soma dos percentuais de primeiro nivel (regra 20: <= 100)

    [Fact]
    public void Validar_SomaPrimeiroNivelIgualCem_NaoLanca()
    {
        var categoriaA = Guid.NewGuid();
        var categoriaB = Guid.NewGuid();

        PlanoMensalValidator.Validar(1000m, 0m,
            new[] { Sublimite(categoriaA, 60m), Sublimite(categoriaB, 40m) },
            Mapa(Despesa(categoriaA), Despesa(categoriaB)));
    }

    [Fact]
    public void Validar_SomaPrimeiroNivelAcimaDeCem_LancaSomaPercentuaisExcedeCemComPaiNulo()
    {
        var categoriaA = Guid.NewGuid();
        var categoriaB = Guid.NewGuid();

        var ex = Assert.Throws<SomaPercentuaisExcedeCemException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(categoriaA, 60m), Sublimite(categoriaB, 40.01m) },
                Mapa(Despesa(categoriaA), Despesa(categoriaB))));

        Assert.Null(ex.CategoriaPaiId);
        Assert.Equal(100.01m, ex.Soma);
    }

    #endregion

    #region Soma dos percentuais das subcategorias de um mesmo pai (regra 20: <= 100)

    [Fact]
    public void Validar_SomaSubcategoriasDoMesmoPaiIgualCem_NaoLanca()
    {
        var paiId = Guid.NewGuid();
        var subA = Guid.NewGuid();
        var subB = Guid.NewGuid();

        PlanoMensalValidator.Validar(1000m, 0m,
            new[] { Sublimite(paiId, 100m), Sublimite(subA, 60m), Sublimite(subB, 40m) },
            Mapa(Despesa(paiId), Despesa(subA, paiId), Despesa(subB, paiId)));
    }

    [Fact]
    public void Validar_SomaSubcategoriasDoMesmoPaiAcimaDeCem_LancaSomaExcedeComIdDoPai()
    {
        var paiId = Guid.NewGuid();
        var subA = Guid.NewGuid();
        var subB = Guid.NewGuid();

        var ex = Assert.Throws<SomaPercentuaisExcedeCemException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(paiId, 100m), Sublimite(subA, 60m), Sublimite(subB, 40.01m) },
                Mapa(Despesa(paiId), Despesa(subA, paiId), Despesa(subB, paiId))));

        Assert.Equal(paiId, ex.CategoriaPaiId);
        Assert.Equal(100.01m, ex.Soma);
    }

    [Fact]
    public void Validar_SubcategoriasDePaisDiferentes_NaoSomamEntreSi()
    {
        var paiUmId = Guid.NewGuid();
        var paiDoisId = Guid.NewGuid();
        var subUmId = Guid.NewGuid();
        var subDoisId = Guid.NewGuid();

        PlanoMensalValidator.Validar(1000m, 0m,
            new[]
            {
                Sublimite(paiUmId, 50m),
                Sublimite(paiDoisId, 50m),
                Sublimite(subUmId, 60m),
                Sublimite(subDoisId, 60m)
            },
            Mapa(
                Despesa(paiUmId),
                Despesa(paiDoisId),
                Despesa(subUmId, paiUmId),
                Despesa(subDoisId, paiDoisId)));
    }

    #endregion

    #region Subcategoria sem pai no conjunto (regra 14: subcategoria so com sublimite se o pai tiver)

    [Fact]
    public void Validar_SubcategoriaComPaiForaDoConjunto_LancaSubcategoriaSemSublimiteNoPai()
    {
        var paiId = Guid.NewGuid();
        var subId = Guid.NewGuid();

        var ex = Assert.Throws<SubcategoriaSemSublimiteNoPaiException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(subId, 10m) },
                Mapa(Despesa(paiId), Despesa(subId, paiId))));

        Assert.Equal(subId, ex.SubcategoriaId);
        Assert.Equal(paiId, ex.PaiId);
    }

    #endregion

    #region Validacao por sublimite (regra 14 / regra 20: duplicada, inexistente, RECEITA ou arquivada)

    [Fact]
    public void Validar_CategoriaRepetidaNoConjunto_LancaSublimiteDuplicadoException()
    {
        var categoriaId = Guid.NewGuid();

        var ex = Assert.Throws<SublimiteDuplicadoException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(categoriaId, 10m), Sublimite(categoriaId, 20m) },
                Mapa(Despesa(categoriaId))));

        Assert.Equal(categoriaId, ex.CategoriaId);
    }

    [Fact]
    public void Validar_CategoriaAusenteDoDicionario_LancaCategoriaNaoEncontradaException()
    {
        var categoriaId = Guid.NewGuid();

        var ex = Assert.Throws<CategoriaNaoEncontradaException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(categoriaId, 10m) },
                Mapa()));

        Assert.Equal(categoriaId, ex.CategoriaId);
    }

    [Fact]
    public void Validar_CategoriaReceita_LancaCategoriaInvalidaParaSublimiteException()
    {
        var categoriaId = Guid.NewGuid();

        var ex = Assert.Throws<CategoriaInvalidaParaSublimiteException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(categoriaId, 10m) },
                Mapa(Receita(categoriaId))));

        Assert.Equal(categoriaId, ex.CategoriaId);
    }

    [Fact]
    public void Validar_CategoriaDespesaArquivada_LancaCategoriaInvalidaParaSublimiteException()
    {
        var categoriaId = Guid.NewGuid();

        var ex = Assert.Throws<CategoriaInvalidaParaSublimiteException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(categoriaId, 10m) },
                Mapa(Despesa(categoriaId, arquivada: true))));

        Assert.Equal(categoriaId, ex.CategoriaId);
    }

    #endregion

    #region Conjunto vazio

    [Fact]
    public void Validar_ConjuntoDeSublimitesVazio_NaoLanca()
    {
        PlanoMensalValidator.Validar(1000m, 50m, Array.Empty<SublimiteEntrada>(), Mapa());
    }

    #endregion

    #region Ordem de checagem (violacao anterior vence a posterior)

    [Fact]
    public void Validar_RendaNegativaEInvestimentoInvalido_LancaRendaPrevistaNegativa()
    {
        Assert.Throws<RendaPrevistaNegativaException>(() =>
            PlanoMensalValidator.Validar(-1m, 150m, Array.Empty<SublimiteEntrada>(), Mapa()));
    }

    [Fact]
    public void Validar_InvestimentoInvalidoESublimiteInvalido_LancaValorInvalidoDoInvestimento()
    {
        var categoriaId = Guid.NewGuid();

        var ex = Assert.Throws<ValorInvalidoException>(() =>
            PlanoMensalValidator.Validar(1000m, 150m,
                new[] { Sublimite(categoriaId, 200m) },
                Mapa(Despesa(categoriaId))));

        Assert.Equal(150m, ex.Valor);
    }

    [Fact]
    public void Validar_PercentualInvalidoDoPrimeiroSublimiteEDuplicataDoSegundo_LancaValorInvalidoDoPrimeiro()
    {
        var categoriaId = Guid.NewGuid();

        var ex = Assert.Throws<ValorInvalidoException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(categoriaId, 200m), Sublimite(categoriaId, 10m) },
                Mapa(Despesa(categoriaId))));

        Assert.Equal(200m, ex.Valor);
    }

    [Fact]
    public void Validar_CategoriaAusenteNoPrimeiroSublimiteEDuplicataNoSegundo_LancaCategoriaNaoEncontrada()
    {
        var categoriaId = Guid.NewGuid();

        var ex = Assert.Throws<CategoriaNaoEncontradaException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(categoriaId, 10m), Sublimite(categoriaId, 10m) },
                Mapa()));

        Assert.Equal(categoriaId, ex.CategoriaId);
    }

    [Fact]
    public void Validar_CategoriaAusenteNoPrimeiroSublimiteEReceitaNoSegundo_LancaCategoriaNaoEncontrada()
    {
        var ausenteId = Guid.NewGuid();
        var receitaId = Guid.NewGuid();

        var ex = Assert.Throws<CategoriaNaoEncontradaException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(ausenteId, 10m), Sublimite(receitaId, 10m) },
                Mapa(Receita(receitaId))));

        Assert.Equal(ausenteId, ex.CategoriaId);
    }

    [Fact]
    public void Validar_ReceitaComPercentualInvalido_LancaCategoriaInvalidaAntesDoValorInvalido()
    {
        var receitaId = Guid.NewGuid();

        var ex = Assert.Throws<CategoriaInvalidaParaSublimiteException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(receitaId, 200m) },
                Mapa(Receita(receitaId))));

        Assert.Equal(receitaId, ex.CategoriaId);
    }

    [Fact]
    public void Validar_PercentualInvalidoDeSubcategoriaSemPai_LancaValorInvalidoAntesDaSubcategoriaSemPai()
    {
        var paiId = Guid.NewGuid();
        var subId = Guid.NewGuid();

        var ex = Assert.Throws<ValorInvalidoException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[] { Sublimite(subId, 200m) },
                Mapa(Despesa(paiId), Despesa(subId, paiId))));

        Assert.Equal(200m, ex.Valor);
    }

    [Fact]
    public void Validar_SubcategoriaSemPaiESomaDePrimeiroNivelAcimaDeCem_LancaSubcategoriaSemPai()
    {
        var paiId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var categoriaA = Guid.NewGuid();
        var categoriaB = Guid.NewGuid();

        var ex = Assert.Throws<SubcategoriaSemSublimiteNoPaiException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[]
                {
                    Sublimite(subId, 10m),
                    Sublimite(categoriaA, 60m),
                    Sublimite(categoriaB, 60m)
                },
                Mapa(
                    Despesa(paiId),
                    Despesa(subId, paiId),
                    Despesa(categoriaA),
                    Despesa(categoriaB))));

        Assert.Equal(subId, ex.SubcategoriaId);
    }

    [Fact]
    public void Validar_SomaDePrimeiroNivelAcimaDeCemEDeSubcategoriasAcimaDeCem_LancaSomaDePrimeiroNivel()
    {
        var paiId = Guid.NewGuid();
        var outraId = Guid.NewGuid();
        var subAId = Guid.NewGuid();
        var subBId = Guid.NewGuid();

        var ex = Assert.Throws<SomaPercentuaisExcedeCemException>(() =>
            PlanoMensalValidator.Validar(1000m, 0m,
                new[]
                {
                    Sublimite(paiId, 50m),
                    Sublimite(outraId, 60m),
                    Sublimite(subAId, 60m),
                    Sublimite(subBId, 60m)
                },
                Mapa(
                    Despesa(paiId),
                    Despesa(outraId),
                    Despesa(subAId, paiId),
                    Despesa(subBId, paiId))));

        Assert.Null(ex.CategoriaPaiId);
        Assert.Equal(110m, ex.Soma);
    }

    #endregion
}
