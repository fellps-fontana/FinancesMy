using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

// TASK-241 / item 6 "Apagar conta fixa": criterio unico de consumada / nao consumada.
// Banco: consumada <=> Status == Pago. Cartao: consumada <=> Fatura.Status == Paga.
public class OcorrenciaContaFixaPoliticaTests
{
    [Fact]
    public void Banco_Pendente_NaoEhConsumada()
    {
        var ocorrencia = CriarBanco(StatusLancamento.Pendente);

        Assert.False(OcorrenciaContaFixaPolitica.EhConsumada(ocorrencia));
    }

    [Fact]
    public void Banco_Sugerido_NaoEhConsumada()
    {
        var ocorrencia = CriarBanco(StatusLancamento.Sugerido);

        Assert.False(OcorrenciaContaFixaPolitica.EhConsumada(ocorrencia));
    }

    [Fact]
    public void Banco_Pago_EhConsumada()
    {
        var ocorrencia = CriarBanco(StatusLancamento.Pago);

        Assert.True(OcorrenciaContaFixaPolitica.EhConsumada(ocorrencia));
    }

    [Theory]
    [InlineData(StatusFatura.Aberta)]
    [InlineData(StatusFatura.Futura)]
    [InlineData(StatusFatura.Fechada)]
    public void Cartao_FaturaNaoPaga_NaoEhConsumada(StatusFatura statusFatura)
    {
        var ocorrencia = CriarCartao(statusFatura, StatusLancamento.Pendente);

        Assert.False(OcorrenciaContaFixaPolitica.EhConsumada(ocorrencia));
        Assert.True(OcorrenciaContaFixaPolitica.EhNaoConsumada(ocorrencia));
    }

    [Fact]
    public void Cartao_FaturaPaga_EhConsumadaMesmoComLancamentoPendente()
    {
        var ocorrencia = CriarCartao(StatusFatura.Paga, StatusLancamento.Pendente);

        Assert.True(OcorrenciaContaFixaPolitica.EhConsumada(ocorrencia));
        Assert.False(OcorrenciaContaFixaPolitica.EhNaoConsumada(ocorrencia));
    }

    [Fact]
    public void QualquerEntrada_ExatamenteUmDosPredicadosEVerdadeiro()
    {
        var entradas = new List<Lancamento>
        {
            CriarBanco(StatusLancamento.Pendente),
            CriarBanco(StatusLancamento.Sugerido),
            CriarBanco(StatusLancamento.Pago),
            CriarCartao(StatusFatura.Aberta, StatusLancamento.Pendente),
            CriarCartao(StatusFatura.Futura, StatusLancamento.Pago),
            CriarCartao(StatusFatura.Fechada, StatusLancamento.Pendente),
            CriarCartao(StatusFatura.Paga, StatusLancamento.Pendente),
            CriarCartao(StatusFatura.Paga, StatusLancamento.Pago)
        };

        foreach (var entrada in entradas)
        {
            var consumada = OcorrenciaContaFixaPolitica.EhConsumada(entrada);
            var naoConsumada = OcorrenciaContaFixaPolitica.EhNaoConsumada(entrada);

            Assert.NotEqual(consumada, naoConsumada);
        }
    }

    private static Lancamento CriarBanco(StatusLancamento status) => new()
    {
        Id = Guid.NewGuid(),
        ContaId = Guid.NewGuid(),
        Descricao = "Aluguel",
        Valor = 1500m,
        Tipo = TipoLancamento.Debit,
        Data = new DateOnly(2026, 11, 15),
        Status = status,
        Fatura = null
    };

    private static Lancamento CriarCartao(StatusFatura statusFatura, StatusLancamento status) => new()
    {
        Id = Guid.NewGuid(),
        ContaId = Guid.NewGuid(),
        Descricao = "Parcela Cartao",
        Valor = 500m,
        Tipo = TipoLancamento.Debit,
        Data = new DateOnly(2026, 11, 15),
        Status = status,
        Fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            Status = statusFatura
        }
    };
}
