using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class RendimentoValorizacaoCalculatorTests
{
    #region Regra: Calculo de delta (valorizacao - caso feliz)

    [Fact]
    public void Calcular_ComDeltaPositivo_RetornaDeltaPositivo()
    {
        // Arrange
        var valorAtualAnterior = 1000m;
        var valorAtualNovo = 1200m;
        var deltEsperado = 200m;

        // Act
        var delta = RendimentoValorizacaoCalculator.Calcular(valorAtualAnterior, valorAtualNovo);

        // Assert
        Assert.NotNull(delta);
        Assert.Equal(deltEsperado, delta.Value);
    }

    [Fact]
    public void Calcular_ComDeltaNegativo_RetornaDeltaNegativo()
    {
        // Arrange
        var valorAtualAnterior = 1000m;
        var valorAtualNovo = 800m;
        var deltEsperado = -200m;

        // Act
        var delta = RendimentoValorizacaoCalculator.Calcular(valorAtualAnterior, valorAtualNovo);

        // Assert
        Assert.NotNull(delta);
        Assert.Equal(deltEsperado, delta.Value);
    }

    [Fact]
    public void Calcular_ComDeltaZero_RetornaNull()
    {
        // Arrange
        var valorAtualAnterior = 1000m;
        var valorAtualNovo = 1000m;

        // Act
        var delta = RendimentoValorizacaoCalculator.Calcular(valorAtualAnterior, valorAtualNovo);

        // Assert
        Assert.Null(delta);
    }

    #endregion

    #region Regra: Calculo com decimais precisos

    [Fact]
    public void Calcular_ComValoresComCentavos_RetornaDeltaPreciso()
    {
        // Arrange
        var valorAtualAnterior = 1234.56m;
        var valorAtualNovo = 1456.78m;
        var deltEsperado = 222.22m;

        // Act
        var delta = RendimentoValorizacaoCalculator.Calcular(valorAtualAnterior, valorAtualNovo);

        // Assert
        Assert.NotNull(delta);
        Assert.Equal(deltEsperado, delta.Value);
    }

    [Fact]
    public void Calcular_ComValoresComCentavosNegativo_RetornaDeltaNegativoPreciso()
    {
        // Arrange
        var valorAtualAnterior = 5678.90m;
        var valorAtualNovo = 5234.56m;
        var deltEsperado = -444.34m;

        // Act
        var delta = RendimentoValorizacaoCalculator.Calcular(valorAtualAnterior, valorAtualNovo);

        // Assert
        Assert.NotNull(delta);
        Assert.Equal(deltEsperado, delta.Value);
    }

    #endregion

    #region Regra: Casos extremos

    [Fact]
    public void Calcular_ComZeroParaValorPositivo_RetornaDeltaPositivo()
    {
        // Arrange
        var valorAtualAnterior = 0m;
        var valorAtualNovo = 100m;
        var deltEsperado = 100m;

        // Act
        var delta = RendimentoValorizacaoCalculator.Calcular(valorAtualAnterior, valorAtualNovo);

        // Assert
        Assert.NotNull(delta);
        Assert.Equal(deltEsperado, delta.Value);
    }

    [Fact]
    public void Calcular_ComValorGrande_RetornaDeltaCorretamente()
    {
        // Arrange
        var valorAtualAnterior = 1_000_000m;
        var valorAtualNovo = 1_500_000m;
        var deltEsperado = 500_000m;

        // Act
        var delta = RendimentoValorizacaoCalculator.Calcular(valorAtualAnterior, valorAtualNovo);

        // Assert
        Assert.NotNull(delta);
        Assert.Equal(deltEsperado, delta.Value);
    }

    [Fact]
    public void Calcular_ComPequenaDiferenca_RetornaDeltaPreciso()
    {
        // Arrange
        var valorAtualAnterior = 10000m;
        var valorAtualNovo = 10000.01m;
        var deltEsperado = 0.01m;

        // Act
        var delta = RendimentoValorizacaoCalculator.Calcular(valorAtualAnterior, valorAtualNovo);

        // Assert
        Assert.NotNull(delta);
        Assert.Equal(deltEsperado, delta.Value);
    }

    #endregion
}
