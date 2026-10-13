using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

// Testes para AntecipacaoParcelasSelector (regra critica de antecipacao de parcelas,
// item 12 subsecao "Antecipacao de parcelas"). Selector puro: sem repository, sem banco.
// Cenario base: compra em 10x, parcela 3/10 na fatura ABERTA (vencimento mar/25),
// parcelas 4..10 em faturas posteriores, parcelas 1..2 em faturas anteriores.
public class AntecipacaoParcelasSelectorTests
{
    private static readonly Guid CompraId = Guid.NewGuid();

    private static Fatura CriarFatura(DateOnly vencimento, StatusFatura status = StatusFatura.Fechada) => new()
    {
        Id = Guid.NewGuid(),
        DataFechamento = vencimento.AddDays(-10),
        DataVencimento = vencimento,
        Status = status
    };

    private static Lancamento CriarParcela(
        int? parcelaNumero,
        Fatura? fatura,
        TipoLancamento tipo = TipoLancamento.Debit) => new()
    {
        Id = Guid.NewGuid(),
        CompraParceladaId = CompraId,
        ParcelaNumero = parcelaNumero,
        Valor = 100m,
        Tipo = tipo,
        Data = new DateOnly(2025, 1, 5),
        Status = StatusLancamento.Pendente,
        Fatura = fatura,
        FaturaId = fatura?.Id
    };

    private sealed class CenarioBase
    {
        public Fatura FaturaAberta { get; }
        public Fatura Fatura1 { get; }
        public Fatura Fatura2 { get; }
        public List<Lancamento> Parcelas { get; } = new();

        public CenarioBase()
        {
            FaturaAberta = CriarFatura(new DateOnly(2025, 3, 20), StatusFatura.Aberta);
            Fatura1 = CriarFatura(new DateOnly(2025, 1, 20), StatusFatura.Paga);
            Fatura2 = CriarFatura(new DateOnly(2025, 2, 20), StatusFatura.Fechada);

            Parcelas.Add(CriarParcela(1, Fatura1));
            Parcelas.Add(CriarParcela(2, Fatura2));
            Parcelas.Add(CriarParcela(3, FaturaAberta));

            for (var numero = 4; numero <= 10; numero++)
            {
                var fatura = CriarFatura(new DateOnly(2025, numero, 20));
                Parcelas.Add(CriarParcela(numero, fatura));
            }
        }
    }

    [Fact]
    public void ObterElegiveis_CenarioBase_DevolveParcelas4a10EmFaturasPosteriores()
    {
        // Arrange
        var cenario = new CenarioBase();

        // Act
        var elegiveis = AntecipacaoParcelasSelector.ObterElegiveis(cenario.Parcelas, cenario.FaturaAberta);

        // Assert
        Assert.Equal(7, elegiveis.Count);
        Assert.Equal(
            new[] { 10, 9, 8, 7, 6, 5, 4 },
            elegiveis.Select(l => l.ParcelaNumero!.Value).ToArray());
    }

    [Fact]
    public void ObterElegiveis_ParcelaNaFaturaAbertaAtual_NaoEElegivel()
    {
        // Arrange
        var cenario = new CenarioBase();

        // Act
        var elegiveis = AntecipacaoParcelasSelector.ObterElegiveis(cenario.Parcelas, cenario.FaturaAberta);

        // Assert
        Assert.DoesNotContain(elegiveis, l => l.ParcelaNumero == 3);
    }

    [Fact]
    public void ObterElegiveis_ParcelasEmFaturasAnterioresFechadaOuPaga_NaoSaoElegiveis()
    {
        // Arrange
        var cenario = new CenarioBase();

        // Act
        var elegiveis = AntecipacaoParcelasSelector.ObterElegiveis(cenario.Parcelas, cenario.FaturaAberta);

        // Assert
        Assert.DoesNotContain(elegiveis, l => l.ParcelaNumero == 1);
        Assert.DoesNotContain(elegiveis, l => l.ParcelaNumero == 2);
    }

    [Fact]
    public void ObterElegiveis_ParcelaSemFatura_NaoEElegivel()
    {
        // Arrange
        var cenario = new CenarioBase();
        var semFatura = CriarParcela(5, fatura: null);
        cenario.Parcelas.Add(semFatura);

        // Act
        var elegiveis = AntecipacaoParcelasSelector.ObterElegiveis(cenario.Parcelas, cenario.FaturaAberta);

        // Assert
        Assert.DoesNotContain(elegiveis, l => l.Id == semFatura.Id);
    }

    [Fact]
    public void ObterElegiveis_EstornoCredit_NaoEElegivelMesmoEmFaturaPosterior()
    {
        // Arrange
        var cenario = new CenarioBase();
        var faturaPosterior = CriarFatura(new DateOnly(2025, 9, 20));
        var estorno = CriarParcela(9, faturaPosterior, TipoLancamento.Credit);
        cenario.Parcelas.Add(estorno);

        // Act
        var elegiveis = AntecipacaoParcelasSelector.ObterElegiveis(cenario.Parcelas, cenario.FaturaAberta);

        // Assert
        Assert.DoesNotContain(elegiveis, l => l.Id == estorno.Id);
    }

    [Fact]
    public void ObterElegiveis_DebitSemParcelaNumero_NaoEElegivel()
    {
        // Arrange
        var cenario = new CenarioBase();
        var semNumero = CriarParcela(parcelaNumero: null, CriarFatura(new DateOnly(2025, 9, 20)));
        cenario.Parcelas.Add(semNumero);

        // Act
        var elegiveis = AntecipacaoParcelasSelector.ObterElegiveis(cenario.Parcelas, cenario.FaturaAberta);

        // Assert
        Assert.DoesNotContain(elegiveis, l => l.Id == semNumero.Id);
    }

    [Fact]
    public void ObterElegiveis_EntradaForaDeOrdem_DevolveParcelaNumeroDecrescente()
    {
        // Arrange
        var cenario = new CenarioBase();
        var embaralhadas = cenario.Parcelas.OrderBy(l => (l.ParcelaNumero * 7) % 11).ToList();

        // Act
        var elegiveis = AntecipacaoParcelasSelector.ObterElegiveis(embaralhadas, cenario.FaturaAberta);

        // Assert
        Assert.Equal(
            new[] { 10, 9, 8, 7, 6, 5, 4 },
            elegiveis.Select(l => l.ParcelaNumero!.Value).ToArray());
    }

    [Fact]
    public void Selecionar_QuantidadeDentroDoLimite_DevolveUltimasParcelasNaOrdemDeAntecipacao()
    {
        // Arrange
        var cenario = new CenarioBase();

        // Act
        var resultado = AntecipacaoParcelasSelector.Selecionar(cenario.Parcelas, cenario.FaturaAberta, 2);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Null(resultado.Erro);
        Assert.Equal(
            new[] { 10, 9 },
            resultado.Selecionadas.Select(l => l.ParcelaNumero!.Value).ToArray());
    }

    [Fact]
    public void Selecionar_QuantidadeIgualAoNumeroDeElegiveis_AceitaTodasAsElegiveis()
    {
        // Arrange
        var cenario = new CenarioBase();

        // Act
        var resultado = AntecipacaoParcelasSelector.Selecionar(cenario.Parcelas, cenario.FaturaAberta, 7);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Equal(7, resultado.Selecionadas.Count);
        Assert.Equal(
            new[] { 10, 9, 8, 7, 6, 5, 4 },
            resultado.Selecionadas.Select(l => l.ParcelaNumero!.Value).ToArray());
    }

    [Fact]
    public void Selecionar_QuantidadeMaiorQueElegiveis_RejeitaInteiroSemSelecionarParcial()
    {
        // Arrange
        var cenario = new CenarioBase();

        // Act
        var resultado = AntecipacaoParcelasSelector.Selecionar(cenario.Parcelas, cenario.FaturaAberta, 8);

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Empty(resultado.Selecionadas);
        Assert.NotNull(resultado.Erro);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Selecionar_QuantidadeMenorQueUm_Rejeita(int quantidade)
    {
        // Arrange
        var cenario = new CenarioBase();

        // Act
        var resultado = AntecipacaoParcelasSelector.Selecionar(cenario.Parcelas, cenario.FaturaAberta, quantidade);

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Empty(resultado.Selecionadas);
        Assert.NotNull(resultado.Erro);
    }
}
