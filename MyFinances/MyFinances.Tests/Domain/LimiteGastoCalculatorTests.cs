using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class LimiteGastoCalculatorTests
{
    #region Regra 1: Calcular soma SO lancamentos Tipo == Debit (ignora Credit)

    [Fact]
    public void Calcular_ComApenasDebits_SomaValoresCorretamente()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 500m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 100m,
                Oculto = false
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 150m,
                Oculto = false
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 200m,
                Oculto = false
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert
        Assert.Equal(450m, resultado.GastoRealizado);
        Assert.False(resultado.Estourado);
    }

    [Fact]
    public void Calcular_ComDebitsECredits_IgnoraCredits()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 500m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 300m,
                Oculto = false
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Credit,
                Valor = 100m,
                Oculto = false
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 50m,
                Oculto = false
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert - deve somar SO os Debits: 300 + 50 = 350
        Assert.Equal(350m, resultado.GastoRealizado);
        Assert.False(resultado.Estourado);
    }

    #endregion

    #region Regra 2: Calcular ignora lancamentos com Oculto == true

    [Fact]
    public void Calcular_ComLancamentosOcultos_IgnoraOcultos()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 500m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 200m,
                Oculto = false
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 150m,
                Oculto = true  // Oculto - deve ser ignorado
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 100m,
                Oculto = false
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert - deve somar SO 200 + 100 = 300, ignorando os 150 ocultos
        Assert.Equal(300m, resultado.GastoRealizado);
        Assert.False(resultado.Estourado);
    }

    [Fact]
    public void Calcular_ComTodosOsLancamentosOcultos_GastoRealizado0()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 500m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 100m,
                Oculto = true
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 200m,
                Oculto = true
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert
        Assert.Equal(0m, resultado.GastoRealizado);
        Assert.False(resultado.Estourado);
    }

    #endregion

    #region Regra 3: Estourado == true quando GastoRealizado > ValorLimite; false quando igual ou menor

    [Fact]
    public void Calcular_GastoMenorQueLimite_EstouradoFalse()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 500m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 300m,
                Oculto = false
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert
        Assert.False(resultado.Estourado);
        Assert.Equal(300m, resultado.GastoRealizado);
    }

    [Fact]
    public void Calcular_GastoIgualAoLimite_EstouradoFalse()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 500m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 500m,
                Oculto = false
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert
        Assert.False(resultado.Estourado);
        Assert.Equal(500m, resultado.GastoRealizado);
    }

    [Fact]
    public void Calcular_GastoMaiorQueLimite_EstouradoTrue()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 500m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 550m,
                Oculto = false
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert
        Assert.True(resultado.Estourado);
        Assert.Equal(550m, resultado.GastoRealizado);
    }

    #endregion

    #region Regra 4: PercentualUtilizado calculado corretamente (gasto / limite)

    [Fact]
    public void Calcular_PercentualUtilizado_CalculoExato()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 100m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 50m,
                Oculto = false
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert - 50 / 100 = 0.5m
        Assert.Equal(0.5m, resultado.PercentualUtilizado);
    }

    [Fact]
    public void Calcular_PercentualUtilizado_CemPorcento()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 200m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 200m,
                Oculto = false
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert - 200 / 200 = 1.0m (100%)
        Assert.Equal(1.0m, resultado.PercentualUtilizado);
    }

    [Fact]
    public void Calcular_PercentualUtilizado_Acima100Porcento()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 100m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 150m,
                Oculto = false
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert - 150 / 100 = 1.5m (150%)
        Assert.Equal(1.5m, resultado.PercentualUtilizado);
    }

    #endregion

    #region Regra 5: PercentualUtilizado == 0 quando ValorLimite == 0 (nao lanca DivideByZeroException)

    [Fact]
    public void Calcular_LimiteZero_PercentualUtilizadoZeroSemExcecao()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 0m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = TipoLancamento.Debit,
                Valor = 100m,
                Oculto = false
            }
        };

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert - nao deve lancar DivideByZeroException
        Assert.Equal(0m, resultado.PercentualUtilizado);
        Assert.Equal(100m, resultado.GastoRealizado);
        Assert.True(resultado.Estourado);  // 100 > 0, portanto estourado
    }

    [Fact]
    public void Calcular_LimiteZeroESemLancamentos_PercentualUtilizadoZero()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 0m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>();

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert
        Assert.Equal(0m, resultado.PercentualUtilizado);
        Assert.Equal(0m, resultado.GastoRealizado);
        Assert.False(resultado.Estourado);
    }

    #endregion

    #region Regra 6: Sem lancamentos, gasto = 0

    [Fact]
    public void Calcular_SemLancamentos_GastoZero()
    {
        // Arrange
        var limiteGasto = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            ValorLimite = 500m,
            Periodo = PeriodoLimiteGasto.Mensal
        };

        var lancamentos = new List<Lancamento>();

        // Act
        var resultado = LimiteGastoCalculator.Calcular(limiteGasto, lancamentos);

        // Assert
        Assert.Equal(0m, resultado.GastoRealizado);
        Assert.Equal(0m, resultado.PercentualUtilizado);
        Assert.False(resultado.Estourado);
    }

    #endregion
}
