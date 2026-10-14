using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

// TASK-238 / item 12 "Ciclo e status da fatura": regra pura, sem I/O e sem relogio.
// hoje = 2026-10-13; fechamento do ciclo corrente = 2026-10-20 (salvo onde indicado).
public class FaturaCicloPoliticaTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 13);
    private static readonly DateOnly FechCicloAtual = new(2026, 10, 20);

    #region 1. ClassificarPorCalendario

    [Fact]
    public void ClassificarPorCalendario_FechamentoAnteriorAHoje_RetornaFechada()
    {
        var status = FaturaCicloPolitica.ClassificarPorCalendario(new DateOnly(2026, 9, 20), Hoje, FechCicloAtual);

        Assert.Equal(StatusFatura.Fechada, status);
    }

    [Fact]
    public void ClassificarPorCalendario_FechamentoIgualAHoje_RetornaAberta()
    {
        var status = FaturaCicloPolitica.ClassificarPorCalendario(Hoje, Hoje, FechCicloAtual);

        Assert.Equal(StatusFatura.Aberta, status);
    }

    [Fact]
    public void ClassificarPorCalendario_FechamentoNoCicloAtual_RetornaAberta()
    {
        var status = FaturaCicloPolitica.ClassificarPorCalendario(FechCicloAtual, Hoje, FechCicloAtual);

        Assert.Equal(StatusFatura.Aberta, status);
    }

    [Fact]
    public void ClassificarPorCalendario_FechamentoDepoisDoCicloAtual_RetornaFutura()
    {
        var status = FaturaCicloPolitica.ClassificarPorCalendario(new DateOnly(2026, 11, 20), Hoje, FechCicloAtual);

        Assert.Equal(StatusFatura.Futura, status);
    }

    #endregion

    #region 2. SelecionarAtual

    [Fact]
    public void SelecionarAtual_ComVariasCandidatas_RetornaADeMenorDataFechamento()
    {
        var faturas = new List<Fatura>
        {
            CriarFatura(new DateOnly(2026, 11, 20), StatusFatura.Futura),
            CriarFatura(FechCicloAtual, StatusFatura.Aberta)
        };

        var atual = FaturaCicloPolitica.SelecionarAtual(faturas, Hoje, new DateOnly(2026, 11, 20));

        Assert.NotNull(atual);
        Assert.Equal(FechCicloAtual, atual.DataFechamento);
    }

    [Fact]
    public void SelecionarAtual_FaturaPagaNaJanela_ContaComoAtual()
    {
        var paga = CriarFatura(FechCicloAtual, StatusFatura.Paga);
        var faturas = new List<Fatura> { paga, CriarFatura(new DateOnly(2026, 11, 20), StatusFatura.Futura) };

        var atual = FaturaCicloPolitica.SelecionarAtual(faturas, Hoje, FechCicloAtual);

        Assert.NotNull(atual);
        Assert.Equal(paga.Id, atual.Id);
    }

    [Fact]
    public void SelecionarAtual_SemCandidataNaJanela_RetornaNull()
    {
        var faturas = new List<Fatura> { CriarFatura(new DateOnly(2026, 11, 20), StatusFatura.Futura) };

        var atual = FaturaCicloPolitica.SelecionarAtual(faturas, Hoje, FechCicloAtual);

        Assert.Null(atual);
    }

    [Fact]
    public void SelecionarAtual_IgnoraFaturaComFechamentoAnteriorAHoje()
    {
        var faturas = new List<Fatura> { CriarFatura(new DateOnly(2026, 9, 20), StatusFatura.Aberta) };

        var atual = FaturaCicloPolitica.SelecionarAtual(faturas, Hoje, FechCicloAtual);

        Assert.Null(atual);
    }

    #endregion

    #region 3. Reconciliar

    [Fact]
    public void Reconciliar_AbertaVencida_ViraFechadaEProximaFuturaViraAberta()
    {
        var vencida = CriarFatura(new DateOnly(2026, 9, 20), StatusFatura.Aberta);
        var proxima = CriarFatura(FechCicloAtual, StatusFatura.Futura);
        var faturas = new List<Fatura> { vencida, proxima };

        var mudancas = FaturaCicloPolitica.Reconciliar(faturas, Hoje, FechCicloAtual);

        Assert.Equal(2, mudancas.Count);
        Assert.Contains(mudancas, m => m.Fatura.Id == vencida.Id && m.NovoStatus == StatusFatura.Fechada);
        Assert.Contains(mudancas, m => m.Fatura.Id == proxima.Id && m.NovoStatus == StatusFatura.Aberta);
    }

    [Fact]
    public void Reconciliar_AtualPaga_NinguemViraAberta()
    {
        var paga = CriarFatura(FechCicloAtual, StatusFatura.Paga);
        var futura = CriarFatura(new DateOnly(2026, 11, 20), StatusFatura.Futura);
        var faturas = new List<Fatura> { paga, futura };

        var mudancas = FaturaCicloPolitica.Reconciliar(faturas, Hoje, FechCicloAtual);

        Assert.DoesNotContain(mudancas, m => m.NovoStatus == StatusFatura.Aberta);
    }

    [Fact]
    public void Reconciliar_FaturaPaga_NuncaAparecenaMudancas()
    {
        var pagaVencida = CriarFatura(new DateOnly(2026, 9, 20), StatusFatura.Paga);
        var faturas = new List<Fatura> { pagaVencida };

        var mudancas = FaturaCicloPolitica.Reconciliar(faturas, Hoje, FechCicloAtual);

        Assert.Empty(mudancas);
    }

    [Fact]
    public void Reconciliar_DoisCandidatasAbertas_SoAMenorFicaAberta()
    {
        // Legado: duas ABERTA na janela. Com fechamento do ciclo corrente em nov, ambas estao na janela.
        var menor = CriarFatura(FechCicloAtual, StatusFatura.Aberta);
        var maior = CriarFatura(new DateOnly(2026, 11, 20), StatusFatura.Aberta);
        var faturas = new List<Fatura> { menor, maior };

        var mudancas = FaturaCicloPolitica.Reconciliar(faturas, Hoje, new DateOnly(2026, 11, 20));

        Assert.Single(mudancas);
        Assert.Equal(maior.Id, mudancas[0].Fatura.Id);
        Assert.Equal(StatusFatura.Futura, mudancas[0].NovoStatus);
    }

    [Fact]
    public void Reconciliar_SegundaAplicacao_DevolveListaVazia()
    {
        var vencida = CriarFatura(new DateOnly(2026, 9, 20), StatusFatura.Aberta);
        var proxima = CriarFatura(FechCicloAtual, StatusFatura.Futura);
        var faturas = new List<Fatura> { vencida, proxima };

        foreach (var mudanca in FaturaCicloPolitica.Reconciliar(faturas, Hoje, FechCicloAtual))
        {
            mudanca.Fatura.Status = mudanca.NovoStatus;
        }

        var segunda = FaturaCicloPolitica.Reconciliar(faturas, Hoje, FechCicloAtual);

        Assert.Empty(segunda);
    }

    #endregion

    #region 4. DecidirDestino

    [Fact]
    public void DecidirDestino_SemFaturaDoCiclo_RetornaCriarNova()
    {
        var destino = FaturaCicloPolitica.DecidirDestino(null, Hoje);

        Assert.Equal(DestinoLancamentoFatura.CriarNova, destino);
    }

    [Theory]
    [InlineData(StatusFatura.Futura)]
    [InlineData(StatusFatura.Aberta)]
    [InlineData(StatusFatura.Fechada)]
    public void DecidirDestino_FaturaNaoPaga_RetornaUsar(StatusFatura status)
    {
        var fatura = CriarFatura(new DateOnly(2026, 9, 20), status);

        var destino = FaturaCicloPolitica.DecidirDestino(fatura, Hoje);

        Assert.Equal(DestinoLancamentoFatura.Usar, destino);
    }

    [Fact]
    public void DecidirDestino_PagaComFechamentoAnteriorAHoje_RetornaRejeitarPaga()
    {
        var fatura = CriarFatura(new DateOnly(2026, 9, 20), StatusFatura.Paga);

        var destino = FaturaCicloPolitica.DecidirDestino(fatura, Hoje);

        Assert.Equal(DestinoLancamentoFatura.RejeitarPaga, destino);
    }

    [Theory]
    [InlineData("2026-10-20")]
    [InlineData("2026-10-13")]
    public void DecidirDestino_PagaComFechamentoIgualOuPosteriorAHoje_RetornaReabrirEUsar(string fechamento)
    {
        var fatura = CriarFatura(DateOnly.Parse(fechamento), StatusFatura.Paga);

        var destino = FaturaCicloPolitica.DecidirDestino(fatura, Hoje);

        Assert.Equal(DestinoLancamentoFatura.ReabrirEUsar, destino);
    }

    #endregion

    #region 5. StatusFatura (FUTURA)

    [Fact]
    public void StatusFatura_FuturaFazRoundTripComValorDeStorage()
    {
        Assert.Equal("FUTURA", StatusFatura.Futura.ToStorageValue());
        Assert.Equal(StatusFatura.Futura, StatusFaturaExtensions.FromStorageValue("FUTURA"));
    }

    #endregion

    #region TASK-239. PodeReceberCompraMovida (assinatura com dia_referencia alterado)

    [Theory]
    [InlineData(StatusFatura.Aberta)]
    [InlineData(StatusFatura.Futura)]
    public void PodeReceberCompraMovida_FaturaExistenteAbertaOuFutura_RetornaTrue(StatusFatura status)
    {
        var pode = FaturaCicloPolitica.PodeReceberCompraMovida(status, new DateOnly(2026, 11, 20), Hoje, FechCicloAtual);

        Assert.True(pode);
    }

    [Theory]
    [InlineData(StatusFatura.Fechada)]
    [InlineData(StatusFatura.Paga)]
    public void PodeReceberCompraMovida_FaturaExistenteFechadaOuPaga_RetornaFalse(StatusFatura status)
    {
        var pode = FaturaCicloPolitica.PodeReceberCompraMovida(status, new DateOnly(2026, 10, 20), Hoje, FechCicloAtual);

        Assert.False(pode);
    }

    [Fact]
    public void PodeReceberCompraMovida_PagaComFechamentoIgualOuPosteriorAHoje_RetornaFalse()
    {
        // Paga antecipada no ciclo atual ou futuro: nao reabre, nao recebe compra movida
        var pode = FaturaCicloPolitica.PodeReceberCompraMovida(StatusFatura.Paga, FechCicloAtual, Hoje, FechCicloAtual);

        Assert.False(pode);
    }

    [Theory]
    [InlineData("2026-10-20")]
    [InlineData("2026-11-20")]
    public void PodeReceberCompraMovida_InexistenteComCalendarioAbertaOuFutura_RetornaTrue(string fechamento)
    {
        var pode = FaturaCicloPolitica.PodeReceberCompraMovida(null, DateOnly.Parse(fechamento), Hoje, FechCicloAtual);

        Assert.True(pode);
    }

    [Fact]
    public void PodeReceberCompraMovida_InexistenteComCalendarioFechada_RetornaFalse()
    {
        var pode = FaturaCicloPolitica.PodeReceberCompraMovida(null, new DateOnly(2026, 9, 20), Hoje, FechCicloAtual);

        Assert.False(pode);
    }

    #endregion

    private static Fatura CriarFatura(DateOnly dataFechamento, StatusFatura status) => new()
    {
        Id = Guid.NewGuid(),
        ContaId = Guid.NewGuid(),
        DataFechamento = dataFechamento,
        DataVencimento = dataFechamento.AddDays(7),
        Status = status
    };
}
