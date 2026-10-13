using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class GastoPorCategoriaCalculatorTests
{
    private static readonly DateOnly DataPadrao = new(2026, 3, 10);

    #region Regra 1: Calcular ignora lancamento Tipo == Credit

    [Fact]
    public void Calcular_LancamentoCredit_NaoEntraNoTotalNemNasLinhas()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoria.Id),
            NovoLancamento(40m, categoria.Id, tipo: TipoLancamento.Credit)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { categoria });

        // Assert
        Assert.Equal(100m, resultado.Total);
        Assert.Equal(100m, Assert.Single(resultado.Linhas).Valor);
    }

    #endregion

    #region Regra 2: Calcular ignora lancamento Oculto

    [Fact]
    public void Calcular_LancamentoOculto_NaoEntraNoTotal()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoria.Id),
            NovoLancamento(60m, categoria.Id, oculto: true)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { categoria });

        // Assert
        Assert.Equal(100m, resultado.Total);
        Assert.Equal(100m, Assert.Single(resultado.Linhas).Valor);
    }

    #endregion

    #region Regra 3: Calcular ignora lancamento com TransferenciaId (transferencia e pagamento de fatura)

    [Fact]
    public void Calcular_LancamentoComTransferenciaId_NaoEntraNoTotal()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoria.Id),
            NovoLancamento(70m, categoria.Id, transferenciaId: Guid.NewGuid())
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { categoria });

        // Assert
        Assert.Equal(100m, resultado.Total);
        Assert.Equal(100m, Assert.Single(resultado.Linhas).Valor);
    }

    #endregion

    #region Regra 4: Calcular ignora Status Sugerido; Pendente e Pago entram

    [Fact]
    public void Calcular_StatusSugerido_NaoEntraNoTotal()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoria.Id, status: StatusLancamento.Pago),
            NovoLancamento(30m, categoria.Id, status: StatusLancamento.Sugerido)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { categoria });

        // Assert
        Assert.Equal(100m, resultado.Total);
    }

    [Fact]
    public void Calcular_StatusPendente_EntraNoTotal()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoria.Id, status: StatusLancamento.Pendente)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { categoria });

        // Assert
        Assert.Equal(100m, resultado.Total);
        Assert.Equal(100m, Assert.Single(resultado.Linhas).Valor);
    }

    [Fact]
    public void Calcular_StatusPago_EntraNoTotal()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoria.Id, status: StatusLancamento.Pago)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { categoria });

        // Assert
        Assert.Equal(100m, resultado.Total);
        Assert.Equal(100m, Assert.Single(resultado.Linhas).Valor);
    }

    #endregion

    #region Regra 5: Subcategoria soma na linha da categoria raiz

    [Fact]
    public void Calcular_Subcategoria_SomaNaLinhaDaRaiz()
    {
        // Arrange
        var raiz = Despesa("Alimentacao", icone: "food");
        var sub = Despesa("Restaurante", parentId: raiz.Id);
        var lancamentos = new[]
        {
            NovoLancamento(50m, sub.Id),
            NovoLancamento(30m, raiz.Id)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { raiz, sub });

        // Assert
        var linha = Assert.Single(resultado.Linhas);
        Assert.Equal(raiz.Id, linha.CategoriaId);
        Assert.Equal("Alimentacao", linha.Nome);
        Assert.Equal("food", linha.Icone);
        Assert.Equal(80m, linha.Valor);
        Assert.Equal(80m, resultado.Total);
    }

    #endregion

    #region Regra 6: Categoria raiz nao-Despesa (Receita) e ignorada

    [Fact]
    public void Calcular_CategoriaRaizReceita_EhIgnorada()
    {
        // Arrange
        var receita = Receita("Salario");
        var lancamentos = new[]
        {
            NovoLancamento(100m, receita.Id)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { receita });

        // Assert
        Assert.Equal(0m, resultado.Total);
        Assert.Empty(resultado.Linhas);
    }

    #endregion

    #region Regra 7: Lancamento cuja categoria nao esta na lista e ignorado

    [Fact]
    public void Calcular_CategoriaForaDaLista_EhIgnorada()
    {
        // Arrange
        var categoriaConhecida = Despesa("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, Guid.NewGuid())
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { categoriaConhecida });

        // Assert
        Assert.Equal(0m, resultado.Total);
        Assert.Empty(resultado.Linhas);
    }

    #endregion

    #region Regra 8: CategoriaId null gera linha "Sem categoria" (Icone null)

    [Fact]
    public void Calcular_LancamentoSemCategoria_GeraLinhaSemCategoria()
    {
        // Arrange
        var categoria = Despesa("Mercado", icone: "cart");
        var lancamentos = new[]
        {
            NovoLancamento(45m, null),
            NovoLancamento(10m, categoria.Id)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { categoria });

        // Assert
        var linhaSemCategoria = resultado.Linhas.Single(l => l.CategoriaId == null);
        Assert.Equal("Sem categoria", linhaSemCategoria.Nome);
        Assert.Null(linhaSemCategoria.Icone);
        Assert.Equal(45m, linhaSemCategoria.Valor);
    }

    #endregion

    #region Regra 9: Categoria com gasto zero nao aparece na lista

    [Fact]
    public void Calcular_CategoriaComGastoZero_NaoApareceNaLista()
    {
        // Arrange
        var categoriaZerada = Despesa("Lazer");
        var categoriaComGasto = Despesa("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(0m, categoriaZerada.Id),
            NovoLancamento(100m, categoriaComGasto.Id)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { categoriaZerada, categoriaComGasto });

        // Assert
        var linha = Assert.Single(resultado.Linhas);
        Assert.Equal(categoriaComGasto.Id, linha.CategoriaId);
    }

    #endregion

    #region Regra 10: Linhas em ordem de valor decrescente

    [Fact]
    public void Calcular_Linhas_OrdenadasPorValorDecrescente()
    {
        // Arrange
        var a = Despesa("A");
        var b = Despesa("B");
        var c = Despesa("C");
        var lancamentos = new[]
        {
            NovoLancamento(50m, a.Id),
            NovoLancamento(300m, b.Id),
            NovoLancamento(120m, c.Id)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { a, b, c });

        // Assert
        Assert.Equal(new[] { "B", "C", "A" }, resultado.Linhas.Select(l => l.Nome));
    }

    #endregion

    #region Regra 11: Percentual arredondado em 2 casas (1/3 -> 33.33; 2/3 -> 66.67)

    [Fact]
    public void Calcular_Percentuais_ArredondamEmDuasCasasComUmTercioEDoisTercos()
    {
        // Arrange
        var a = Despesa("A");
        var b = Despesa("B");
        var lancamentos = new[]
        {
            NovoLancamento(100m, a.Id),
            NovoLancamento(200m, b.Id)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { a, b });

        // Assert
        Assert.Equal(66.67m, resultado.Linhas.Single(l => l.CategoriaId == b.Id).Percentual);
        Assert.Equal(33.33m, resultado.Linhas.Single(l => l.CategoriaId == a.Id).Percentual);
    }

    #endregion

    #region Regra 12: Total e a soma das linhas

    [Fact]
    public void Calcular_Total_EhSomaDasLinhas()
    {
        // Arrange
        var a = Despesa("A");
        var b = Despesa("B");
        var lancamentos = new[]
        {
            NovoLancamento(100m, a.Id),
            NovoLancamento(250m, b.Id),
            NovoLancamento(50m, null)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { a, b });

        // Assert
        Assert.Equal(400m, resultado.Total);
        Assert.Equal(400m, resultado.Linhas.Sum(l => l.Valor));
    }

    #endregion

    #region Regra 13: Categoria arquivada continua aparecendo se tiver gasto no mes

    [Fact]
    public void Calcular_CategoriaArquivadaComGasto_AparecaNaLista()
    {
        // Arrange
        var arquivada = Despesa("Antiga", arquivada: true);
        var lancamentos = new[]
        {
            NovoLancamento(90m, arquivada.Id)
        };

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, new[] { arquivada });

        // Assert
        var linha = Assert.Single(resultado.Linhas);
        Assert.Equal(arquivada.Id, linha.CategoriaId);
        Assert.Equal(90m, linha.Valor);
    }

    #endregion

    #region Regra 14: Sem lancamentos elegiveis, total zero e lista vazia

    [Fact]
    public void Calcular_SemLancamentos_RetornaTotalZeroELinhasVazias()
    {
        // Arrange
        var categoria = Despesa("Mercado");

        // Act
        var resultado = GastoPorCategoriaCalculator.Calcular(
            Array.Empty<Lancamento>(),
            new[] { categoria });

        // Assert
        Assert.Equal(0m, resultado.Total);
        Assert.Empty(resultado.Linhas);
    }

    #endregion

    #region Regra 15: CalcularPercentual (total <= 0 retorna 0; 2 casas AwayFromZero)

    [Fact]
    public void CalcularPercentual_TotalZero_RetornaZero()
    {
        // Act
        var percentual = GastoPorCategoriaCalculator.CalcularPercentual(100m, 0m);

        // Assert
        Assert.Equal(0m, percentual);
    }

    [Fact]
    public void CalcularPercentual_TotalNegativo_RetornaZero()
    {
        // Act
        var percentual = GastoPorCategoriaCalculator.CalcularPercentual(100m, -5m);

        // Assert
        Assert.Equal(0m, percentual);
    }

    [Fact]
    public void CalcularPercentual_ValorIgualAoTotal_RetornaCem()
    {
        // Act
        var percentual = GastoPorCategoriaCalculator.CalcularPercentual(250m, 250m);

        // Assert
        Assert.Equal(100.00m, percentual);
    }

    [Fact]
    public void CalcularPercentual_UmTercio_RetornaTrintaETresVirgulaTrintaETres()
    {
        // Act
        var percentual = GastoPorCategoriaCalculator.CalcularPercentual(100m, 300m);

        // Assert
        Assert.Equal(33.33m, percentual);
    }

    [Fact]
    public void CalcularPercentual_MeioDoCentesimo_ArredondaAwayFromZero()
    {
        // Arrange: 1 / 800 = 0,125% -> AwayFromZero da 0,13 (ToEven daria 0,12)

        // Act
        var percentual = GastoPorCategoriaCalculator.CalcularPercentual(1m, 800m);

        // Assert
        Assert.Equal(0.13m, percentual);
    }

    #endregion

    #region Regra 16: LancamentosDaLinha - soma do detalhe == valor da linha

    [Fact]
    public void LancamentosDaLinha_SomaEhIgualAoValorDaLinha_IncluindoSubcategoria()
    {
        // Arrange
        var raiz = Despesa("Alimentacao");
        var sub = Despesa("Restaurante", parentId: raiz.Id);
        var lancamentos = new[]
        {
            NovoLancamento(50m, sub.Id, data: new DateOnly(2026, 3, 5)),
            NovoLancamento(30m, raiz.Id, data: new DateOnly(2026, 3, 2)),
            NovoLancamento(999m, raiz.Id, tipo: TipoLancamento.Credit)
        };
        var categorias = new[] { raiz, sub };

        // Act
        var linha = Assert.Single(GastoPorCategoriaCalculator.Calcular(lancamentos, categorias).Linhas);
        var detalhe = GastoPorCategoriaCalculator.LancamentosDaLinha(lancamentos, categorias, raiz.Id);

        // Assert
        Assert.Equal(80m, linha.Valor);
        Assert.Equal(linha.Valor, detalhe.Sum(l => l.Valor));
        Assert.Equal(2, detalhe.Count);
    }

    [Fact]
    public void LancamentosDaLinha_SemCategoria_RetornaApenasLancamentosComCategoriaNula()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var semCategoria = NovoLancamento(45m, null);
        var lancamentos = new[]
        {
            semCategoria,
            NovoLancamento(10m, categoria.Id)
        };
        var categorias = new[] { categoria };

        // Act
        var detalhe = GastoPorCategoriaCalculator.LancamentosDaLinha(lancamentos, categorias, null);

        // Assert
        var unico = Assert.Single(detalhe);
        Assert.Equal(semCategoria.Id, unico.Id);
        Assert.Null(unico.CategoriaId);
        Assert.Equal(45m, detalhe.Sum(l => l.Valor));
    }

    #endregion

    #region Regra 17: LancamentosDaLinha - ordem de data crescente

    [Fact]
    public void LancamentosDaLinha_OrdenaPorDataCrescente()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var tarde = NovoLancamento(10m, categoria.Id, data: new DateOnly(2026, 3, 20));
        var cedo = NovoLancamento(10m, categoria.Id, data: new DateOnly(2026, 3, 2));
        var meio = NovoLancamento(10m, categoria.Id, data: new DateOnly(2026, 3, 10));
        var lancamentos = new[] { tarde, cedo, meio };

        // Act
        var detalhe = GastoPorCategoriaCalculator.LancamentosDaLinha(lancamentos, new[] { categoria }, categoria.Id);

        // Assert
        Assert.Equal(new[] { cedo.Id, meio.Id, tarde.Id }, detalhe.Select(l => l.Id));
    }

    #endregion

    #region Regra 18: LancamentosDaLinha - mesma data desempata por Id

    [Fact]
    public void LancamentosDaLinha_MesmaData_DesempataPorId()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var idMaior = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var idMenor = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var data = new DateOnly(2026, 3, 10);
        var lancamentos = new[]
        {
            NovoLancamento(10m, categoria.Id, data: data, id: idMaior),
            NovoLancamento(10m, categoria.Id, data: data, id: idMenor)
        };

        // Act
        var detalhe = GastoPorCategoriaCalculator.LancamentosDaLinha(lancamentos, new[] { categoria }, categoria.Id);

        // Assert
        Assert.Equal(new[] { idMenor, idMaior }, detalhe.Select(l => l.Id));
    }

    #endregion

    #region Regra 19: LancamentosDaLinha - aplica o mesmo filtro elegivel do Calcular

    [Fact]
    public void LancamentosDaLinha_IgnoraNaoElegiveis()
    {
        // Arrange
        var categoria = Despesa("Mercado");
        var elegivel = NovoLancamento(100m, categoria.Id, status: StatusLancamento.Pago);
        var lancamentos = new[]
        {
            elegivel,
            NovoLancamento(40m, categoria.Id, tipo: TipoLancamento.Credit),
            NovoLancamento(20m, categoria.Id, oculto: true),
            NovoLancamento(30m, categoria.Id, transferenciaId: Guid.NewGuid()),
            NovoLancamento(10m, categoria.Id, status: StatusLancamento.Sugerido)
        };

        // Act
        var detalhe = GastoPorCategoriaCalculator.LancamentosDaLinha(lancamentos, new[] { categoria }, categoria.Id);

        // Assert
        var unico = Assert.Single(detalhe);
        Assert.Equal(elegivel.Id, unico.Id);
    }

    #endregion

    #region Regra 20: LancamentosDaLinha - raiz nao-Despesa ou sem lancamentos retorna lista vazia

    [Fact]
    public void LancamentosDaLinha_RaizReceita_RetornaListaVazia()
    {
        // Arrange
        var receita = Receita("Salario");
        var lancamentos = new[]
        {
            NovoLancamento(100m, receita.Id)
        };

        // Act
        var detalhe = GastoPorCategoriaCalculator.LancamentosDaLinha(lancamentos, new[] { receita }, receita.Id);

        // Assert
        Assert.Empty(detalhe);
    }

    [Fact]
    public void LancamentosDaLinha_CategoriaSemLancamentos_RetornaListaVazia()
    {
        // Arrange
        var categoriaSemGasto = Despesa("Lazer");
        var outra = Despesa("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, outra.Id)
        };

        // Act
        var detalhe = GastoPorCategoriaCalculator.LancamentosDaLinha(
            lancamentos,
            new[] { categoriaSemGasto, outra },
            categoriaSemGasto.Id);

        // Assert
        Assert.Empty(detalhe);
    }

    #endregion

    #region Helpers

    private static Categoria Despesa(string nome, Guid? parentId = null, string? icone = null, bool arquivada = false) => new()
    {
        Id = Guid.NewGuid(),
        Nome = nome,
        Tipo = TipoCategoria.Despesa,
        ParentId = parentId,
        Icone = icone,
        Arquivada = arquivada
    };

    private static Categoria Receita(string nome) => new()
    {
        Id = Guid.NewGuid(),
        Nome = nome,
        Tipo = TipoCategoria.Receita
    };

    private static Lancamento NovoLancamento(
        decimal valor,
        Guid? categoriaId,
        TipoLancamento tipo = TipoLancamento.Debit,
        StatusLancamento status = StatusLancamento.Pago,
        bool oculto = false,
        Guid? transferenciaId = null,
        DateOnly? data = null,
        Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Valor = valor,
        CategoriaId = categoriaId,
        Tipo = tipo,
        Status = status,
        Oculto = oculto,
        TransferenciaId = transferenciaId,
        Data = data ?? DataPadrao
    };

    #endregion
}
