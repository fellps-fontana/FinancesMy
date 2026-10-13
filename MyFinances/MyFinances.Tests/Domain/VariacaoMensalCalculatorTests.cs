using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class VariacaoMensalCalculatorTests
{
    #region Regra 1: Variacao = (atual - anterior) / anterior, em percentual

    [Fact]
    public void Calcular_AumentoDe100Para120_RetornaVinte()
    {
        // Act
        var variacao = VariacaoMensalCalculator.Calcular(120m, 100m);

        // Assert
        Assert.Equal<decimal?>(20.00m, variacao);
    }

    [Fact]
    public void Calcular_QuedaDe100Para80_RetornaMenosVinte()
    {
        // Act
        var variacao = VariacaoMensalCalculator.Calcular(80m, 100m);

        // Assert
        Assert.Equal<decimal?>(-20.00m, variacao);
    }

    [Fact]
    public void Calcular_De300Para100_RetornaMenosSessentaESeisVirgulaSessentaESeis()
    {
        // Act
        var variacao = VariacaoMensalCalculator.Calcular(100m, 300m);

        // Assert
        Assert.Equal<decimal?>(-66.67m, variacao);
    }

    [Fact]
    public void Calcular_MesAtualZero_RetornaMenosCem()
    {
        // Act
        var variacao = VariacaoMensalCalculator.Calcular(0m, 100m);

        // Assert
        Assert.Equal<decimal?>(-100.00m, variacao);
    }

    #endregion

    #region Regra 2: Mes anterior zero retorna null (nao exibe variacao)

    [Fact]
    public void Calcular_MesAnteriorZero_RetornaNull()
    {
        // Act
        var variacao = VariacaoMensalCalculator.Calcular(120m, 0m);

        // Assert
        Assert.Null(variacao);
    }

    [Fact]
    public void Calcular_MesAnteriorZeroEMesAtualZero_RetornaNull()
    {
        // Act
        var variacao = VariacaoMensalCalculator.Calcular(0m, 0m);

        // Assert
        Assert.Null(variacao);
    }

    #endregion

    #region Regra 3: Arredondamento em 2 casas AwayFromZero

    [Fact]
    public void Calcular_MeioDoCentesimo_ArredondaAwayFromZero()
    {
        // Arrange: (801 - 800) / 800 = 0,125% -> AwayFromZero da 0,13 (ToEven daria 0,12)

        // Act
        var variacao = VariacaoMensalCalculator.Calcular(801m, 800m);

        // Assert
        Assert.Equal<decimal?>(0.13m, variacao);
    }

    #endregion
}
