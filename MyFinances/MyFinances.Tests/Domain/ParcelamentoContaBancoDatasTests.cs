using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

// Testes para ParcelamentoContaBancoDatas.Calcular (regra-de-negocio item 17,
// lancamento parcelado em conta banco). Calculo puro, sem repository.
// Regra: parcela i cai i-1 meses depois da primeira, no mesmo dia da primeira;
// se o mes nao tem o dia, usa o ultimo dia do mes. Sem deriva.
public class ParcelamentoContaBancoDatasTests
{
    [Fact]
    public void Calcular_DiaQuinzeTresParcelas_MantemDiaQuinzeEmTodosOsMeses()
    {
        var resultado = ParcelamentoContaBancoDatas.Calcular(new DateOnly(2026, 1, 15), 3);

        Assert.Equal(
            new[] { new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 15), new DateOnly(2026, 3, 15) },
            resultado);
    }

    [Fact]
    public void Calcular_DiaTrintaEUmTresParcelas_FevereiroClampaEMarcoVoltaAoDia31()
    {
        var resultado = ParcelamentoContaBancoDatas.Calcular(new DateOnly(2026, 1, 31), 3);

        Assert.Equal(
            new[] { new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31) },
            resultado);
    }

    [Fact]
    public void Calcular_DiaTrintaEUmEmAnoBissexto_FevereiroClampaEm29()
    {
        var resultado = ParcelamentoContaBancoDatas.Calcular(new DateOnly(2024, 1, 31), 3);

        Assert.Equal(
            new[] { new DateOnly(2024, 1, 31), new DateOnly(2024, 2, 29), new DateOnly(2024, 3, 31) },
            resultado);
    }

    [Fact]
    public void Calcular_DiaTrintaQuatroQuatroParcelas_ClampaFevereiroEAbrilSemDerivar()
    {
        var resultado = ParcelamentoContaBancoDatas.Calcular(new DateOnly(2026, 1, 30), 4);

        Assert.Equal(
            new[]
            {
                new DateOnly(2026, 1, 30),
                new DateOnly(2026, 2, 28),
                new DateOnly(2026, 3, 30),
                new DateOnly(2026, 4, 30)
            },
            resultado);
    }

    [Fact]
    public void Calcular_DezembroTresParcelas_ViraOAnoSemPerderODia()
    {
        var resultado = ParcelamentoContaBancoDatas.Calcular(new DateOnly(2026, 12, 31), 3);

        Assert.Equal(
            new[] { new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 28) },
            resultado);
    }

    [Fact]
    public void Calcular_DiaTrintaEUmMaioDuasParcelas_SegundaCaiEm30DeJunho()
    {
        var resultado = ParcelamentoContaBancoDatas.Calcular(new DateOnly(2026, 5, 31), 2);

        Assert.Equal(
            new[] { new DateOnly(2026, 5, 31), new DateOnly(2026, 6, 30) },
            resultado);
    }

    [Fact]
    public void Calcular_DozeParcelasDiaTrintaEUm_MantemDia31NosMesesDeTrintaEUmEClampaNosDemais()
    {
        var resultado = ParcelamentoContaBancoDatas.Calcular(new DateOnly(2026, 1, 31), 12);

        Assert.Equal(
            new[]
            {
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 28),
                new DateOnly(2026, 3, 31),
                new DateOnly(2026, 4, 30),
                new DateOnly(2026, 5, 31),
                new DateOnly(2026, 6, 30),
                new DateOnly(2026, 7, 31),
                new DateOnly(2026, 8, 31),
                new DateOnly(2026, 9, 30),
                new DateOnly(2026, 10, 31),
                new DateOnly(2026, 11, 30),
                new DateOnly(2026, 12, 31)
            },
            resultado);
    }

    [Fact]
    public void Calcular_QuantidadeUm_LancaArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => ParcelamentoContaBancoDatas.Calcular(new DateOnly(2026, 1, 15), 1));
    }

    [Fact]
    public void Calcular_QuantidadeZero_LancaArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => ParcelamentoContaBancoDatas.Calcular(new DateOnly(2026, 1, 15), 0));
    }
}
