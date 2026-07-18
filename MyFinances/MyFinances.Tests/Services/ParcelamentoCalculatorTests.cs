using System.Linq;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Services;

public class ParcelamentoCalculatorTests
{
    // Caso (a): R$100,00 em 3x -> [33.33, 33.33, 33.34]
    [Fact]
    public void CalcularValoresParcelas_CemReaisTresParcelas_Retorna33_33_33_33_33_34()
    {
        // Arrange
        decimal valorTotal = 100m;
        int quantidadeParcelas = 3;
        var expected = new[] { 33.33m, 33.33m, 33.34m };

        // Act
        var resultado = ParcelamentoCalculator.CalcularValoresParcelas(valorTotal, quantidadeParcelas);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(quantidadeParcelas, resultado.Count);
        Assert.Equal(expected, resultado);
    }

    // Caso (b): R$100,00 em 4x -> [25.00, 25.00, 25.00, 25.00] (sem resto)
    [Fact]
    public void CalcularValoresParcelas_CemReaisQuatroParcelas_Retorna25EmTodasAsParcelas()
    {
        // Arrange
        decimal valorTotal = 100m;
        int quantidadeParcelas = 4;
        var expected = new[] { 25.00m, 25.00m, 25.00m, 25.00m };

        // Act
        var resultado = ParcelamentoCalculator.CalcularValoresParcelas(valorTotal, quantidadeParcelas);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(quantidadeParcelas, resultado.Count);
        Assert.Equal(expected, resultado);
    }

    // Caso (c): R$10,00 em 3x -> [3.33, 3.33, 3.34]
    [Fact]
    public void CalcularValoresParcelas_DezReaisTresParcelas_Retorna3_33_3_33_3_34()
    {
        // Arrange
        decimal valorTotal = 10m;
        int quantidadeParcelas = 3;
        var expected = new[] { 3.33m, 3.33m, 3.34m };

        // Act
        var resultado = ParcelamentoCalculator.CalcularValoresParcelas(valorTotal, quantidadeParcelas);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(quantidadeParcelas, resultado.Count);
        Assert.Equal(expected, resultado);
    }

    // Caso (d): soma das N parcelas retornadas == valorTotal exatamente
    // Teste 1: casos com resto (reutilizando caso a)
    [Fact]
    public void CalcularValoresParcelas_CemReaisTresParcelas_SomaExatamenteCemReais()
    {
        // Arrange
        decimal valorTotal = 100m;
        int quantidadeParcelas = 3;

        // Act
        var resultado = ParcelamentoCalculator.CalcularValoresParcelas(valorTotal, quantidadeParcelas);

        // Assert
        decimal soma = resultado.Aggregate(0m, (acc, val) => acc + val);
        Assert.Equal(valorTotal, soma);
    }

    // Caso (d): Teste 2 - caso com resto diferente (reutilizando caso c)
    [Fact]
    public void CalcularValoresParcelas_DezReaisTresParcelas_SomaExatamenteDezReais()
    {
        // Arrange
        decimal valorTotal = 10m;
        int quantidadeParcelas = 3;

        // Act
        var resultado = ParcelamentoCalculator.CalcularValoresParcelas(valorTotal, quantidadeParcelas);

        // Assert
        decimal soma = resultado.Aggregate(0m, (acc, val) => acc + val);
        Assert.Equal(valorTotal, soma);
    }

    // Caso (d): Teste 3 - caso sem resto, para garantir que funciona tambem
    [Fact]
    public void CalcularValoresParcelas_CemReaisQuatroParcelas_SomaExatamenteCemReais()
    {
        // Arrange
        decimal valorTotal = 100m;
        int quantidadeParcelas = 4;

        // Act
        var resultado = ParcelamentoCalculator.CalcularValoresParcelas(valorTotal, quantidadeParcelas);

        // Assert
        decimal soma = resultado.Aggregate(0m, (acc, val) => acc + val);
        Assert.Equal(valorTotal, soma);
    }

    // Caso (e): quantidadeParcelas = 0 ou 1 lanca ArgumentException
    [Fact]
    public void CalcularValoresParcelas_QuantidadeParcelasZero_LancaArgumentException()
    {
        // Arrange
        decimal valorTotal = 100m;
        int quantidadeParcelas = 0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => ParcelamentoCalculator.CalcularValoresParcelas(valorTotal, quantidadeParcelas)
        );
        Assert.NotNull(exception);
    }

    [Fact]
    public void CalcularValoresParcelas_QuantidadeParcelasUm_LancaArgumentException()
    {
        // Arrange
        decimal valorTotal = 100m;
        int quantidadeParcelas = 1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => ParcelamentoCalculator.CalcularValoresParcelas(valorTotal, quantidadeParcelas)
        );
        Assert.NotNull(exception);
    }

    // Caso (f): valorTotal <= 0 lanca ArgumentException
    [Fact]
    public void CalcularValoresParcelas_ValorTotalZero_LancaArgumentException()
    {
        // Arrange
        decimal valorTotal = 0m;
        int quantidadeParcelas = 3;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => ParcelamentoCalculator.CalcularValoresParcelas(valorTotal, quantidadeParcelas)
        );
        Assert.NotNull(exception);
    }

    [Fact]
    public void CalcularValoresParcelas_ValorTotalNegativo_LancaArgumentException()
    {
        // Arrange
        decimal valorTotal = -50m;
        int quantidadeParcelas = 3;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => ParcelamentoCalculator.CalcularValoresParcelas(valorTotal, quantidadeParcelas)
        );
        Assert.NotNull(exception);
    }
}
