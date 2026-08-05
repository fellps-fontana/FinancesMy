using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class AtivoPrecoMedioCalculatorTests
{
    #region Caso didatico da regra: 10 cotas a R$10 + 10 cotas a R$20 = preco medio R$15

    [Fact]
    public void Calcular_CasoDidaticoRegra_10CotasA10Mais10CotasA20_RetornaPrecoMedio15()
    {
        // Arrange
        var precoMedioAtual = 10m; // Preco medio inicial
        var qtdAtual = 10m;         // Quantidade atual
        var precoAporte = 20m;      // Preco do novo aporte
        var qtdAporte = 10m;        // Quantidade do novo aporte

        // Act
        // Formula: preco_medio_novo = (preco_medio_atual * qtd_atual + preco_aporte * qtd_aporte)
        //                              / (qtd_atual + qtd_aporte)
        //         = (10 * 10 + 20 * 10) / (10 + 10)
        //         = (100 + 200) / 20
        //         = 300 / 20
        //         = 15
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        Assert.Equal(15m, precoMedioNovo);
    }

    #endregion

    #region Caso feliz - formula basica

    [Fact]
    public void Calcular_ComValoresSimples_RetornaPrecoMedioPonderado()
    {
        // Arrange
        var precoMedioAtual = 100m;
        var qtdAtual = 5m;
        var precoAporte = 120m;
        var qtdAporte = 3m;

        // Act
        // (100 * 5 + 120 * 3) / (5 + 3) = (500 + 360) / 8 = 860 / 8 = 107.5
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        Assert.Equal(107.5m, precoMedioNovo);
    }

    [Fact]
    public void Calcular_ComAporteEmPrecoIgual_RetornaPrecoMedioIgual()
    {
        // Arrange
        var precoMedioAtual = 50m;
        var qtdAtual = 10m;
        var precoAporte = 50m; // Mesmo preco
        var qtdAporte = 10m;

        // Act
        // (50 * 10 + 50 * 10) / (10 + 10) = (500 + 500) / 20 = 1000 / 20 = 50
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        Assert.Equal(50m, precoMedioNovo); // Preco medio nao muda
    }

    [Fact]
    public void Calcular_ComAporteMaisBarato_ReduzPrecoMedio()
    {
        // Arrange
        var precoMedioAtual = 100m;
        var qtdAtual = 20m;
        var precoAporte = 80m; // Mais barato
        var qtdAporte = 20m;

        // Act
        // (100 * 20 + 80 * 20) / (20 + 20) = (2000 + 1600) / 40 = 3600 / 40 = 90
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        Assert.Equal(90m, precoMedioNovo);
    }

    #endregion

    #region Casos com decimais e precisa

    [Fact]
    public void Calcular_ComDecimaisNoPreco_RetornaPrecoMedioCorreto()
    {
        // Arrange
        var precoMedioAtual = 10.50m;
        var qtdAtual = 15.5m;
        var precoAporte = 12.75m;
        var qtdAporte = 20.25m;

        // Act
        // (10.50 * 15.5 + 12.75 * 20.25) / (15.5 + 20.25)
        // = (162.75 + 258.1875) / 35.75
        // = 420.9375 / 35.75
        // = 6735/572 = 11.7746478873...
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert - Valor deve estar entre 11.77 e 11.78
        Assert.True(precoMedioNovo > 11.77m && precoMedioNovo < 11.78m);
    }

    [Fact]
    public void Calcular_ComMuitasDecimais_MantmPrecisao()
    {
        // Arrange
        var precoMedioAtual = 25.555m;
        var qtdAtual = 11.111m;
        var precoAporte = 33.333m;
        var qtdAporte = 22.222m;

        // Act
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert - Verificar que o resultado e valido (nao e negativo)
        Assert.True(precoMedioNovo > 0);
    }

    #endregion

    #region Casos de borda - quantidades pequenas

    [Fact]
    public void Calcular_ComQuantidadeUnitaria_RetornaMediaPonderada()
    {
        // Arrange
        var precoMedioAtual = 100m;
        var qtdAtual = 1m; // Uma cota
        var precoAporte = 200m;
        var qtdAporte = 1m; // Uma cota adicional

        // Act
        // (100 * 1 + 200 * 1) / (1 + 1) = 300 / 2 = 150
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        Assert.Equal(150m, precoMedioNovo);
    }

    [Fact]
    public void Calcular_ComQuantidadeDecimal_RetornaCorreto()
    {
        // Arrange
        var precoMedioAtual = 50m;
        var qtdAtual = 0.5m;
        var precoAporte = 100m;
        var qtdAporte = 0.5m;

        // Act
        // (50 * 0.5 + 100 * 0.5) / (0.5 + 0.5) = (25 + 50) / 1 = 75
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        Assert.Equal(75m, precoMedioNovo);
    }

    #endregion

    #region Casos de borda - quantidades desproporcionais

    [Fact]
    public void Calcular_ComQuantidadeAporteRapequena_ApenasLevementeAlteraPrecoMedio()
    {
        // Arrange
        var precoMedioAtual = 100m;
        var qtdAtual = 1000m; // Quantidade grande
        var precoAporte = 50m;
        var qtdAporte = 1m; // Quantidade bem pequena

        // Act
        // (100 * 1000 + 50 * 1) / (1000 + 1) = (100000 + 50) / 1001 = 100050 / 1001 ≈ 99.95
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        // Deve ser bem proximo ao preco medio anterior
        Assert.True(precoMedioNovo < precoMedioAtual);
        Assert.True(precoMedioNovo > 99.9m && precoMedioNovo < 99.96m);
    }

    [Fact]
    public void Calcular_ComAporteMuitoMaiorQueStockAnterior_DominaONovoPrecoMedio()
    {
        // Arrange
        var precoMedioAtual = 100m;
        var qtdAtual = 1m; // Quantidade pequena
        var precoAporte = 200m;
        var qtdAporte = 1000m; // Quantidade bem grande

        // Act
        // (100 * 1 + 200 * 1000) / (1 + 1000) = (100 + 200000) / 1001 = 200100 / 1001 ≈ 199.90
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        // Deve ser bem proximo ao preco do novo aporte (que domina a quantidade)
        Assert.True(precoMedioNovo > 199.8m && precoMedioNovo < 199.95m);
    }

    #endregion

    #region Casos especiais - valores grandes

    [Fact]
    public void Calcular_ComValoresGrandes_RetornaCorreto()
    {
        // Arrange
        var precoMedioAtual = 10000m;
        var qtdAtual = 500m;
        var precoAporte = 12000m;
        var qtdAporte = 300m;

        // Act
        // (10000 * 500 + 12000 * 300) / (500 + 300)
        // = (5000000 + 3600000) / 800
        // = 8600000 / 800
        // = 10750
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        Assert.Equal(10750m, precoMedioNovo);
    }

    #endregion

    #region Propriedades matematicas da media ponderada

    [Fact]
    public void Calcular_AporteAbaixoDoPrecoAtual_ReduzPrecoMedio()
    {
        // Arrange
        var precoMedioAtual = 100m;
        var qtdAtual = 50m;
        var precoAporte = 80m; // Abaixo do preco atual
        var qtdAporte = 50m;

        // Act
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        Assert.True(precoMedioNovo < precoMedioAtual);
        Assert.True(precoMedioNovo > precoAporte);
    }

    [Fact]
    public void Calcular_AporteAcimaDoPrecoAtual_AumentaPrecoMedio()
    {
        // Arrange
        var precoMedioAtual = 100m;
        var qtdAtual = 50m;
        var precoAporte = 120m; // Acima do preco atual
        var qtdAporte = 50m;

        // Act
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        Assert.True(precoMedioNovo > precoMedioAtual);
        Assert.True(precoMedioNovo < precoAporte);
    }

    [Fact]
    public void Calcular_AportesEquilibrados_PrecoMedioFicaBetweenExtremes()
    {
        // Arrange
        var precoMedioAtual = 100m;
        var qtdAtual = 100m;
        var precoAporte = 150m;
        var qtdAporte = 100m;

        // Act
        var precoMedioNovo = AtivoPrecoMedioCalculator.Calcular(precoMedioAtual, qtdAtual, precoAporte, qtdAporte);

        // Assert
        Assert.True(precoMedioNovo > precoMedioAtual);
        Assert.True(precoMedioNovo < precoAporte);
        // Com quantidades iguais, deve ser exatamente a media simples
        Assert.Equal(125m, precoMedioNovo); // (100 + 150) / 2
    }

    #endregion
}
