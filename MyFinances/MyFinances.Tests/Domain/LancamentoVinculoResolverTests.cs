using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

// Regra-de-negocio item 18: LancamentoVinculoResolver e a fonte unica de
// "este lancamento e avulso? se nao, quem e o dono?".
public class LancamentoVinculoResolverTests
{
    // Monta um lancamento com o flag Manual informado e os vinculos nomeados
    // (string vazia ou ausente = nenhum vinculo).
    private static Lancamento CriarLancamento(bool manual, params string[] vinculos)
    {
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Manual = manual
        };

        foreach (var vinculo in vinculos)
        {
            AplicarVinculo(lancamento, vinculo);
        }

        return lancamento;
    }

    private static void AplicarVinculo(Lancamento lancamento, string vinculo)
    {
        switch (vinculo)
        {
            case "":
                break;
            case "TransferenciaId":
                lancamento.TransferenciaId = Guid.NewGuid();
                break;
            case "CompraParceladaId":
                lancamento.CompraParceladaId = Guid.NewGuid();
                break;
            case "ContaFixaId":
                lancamento.ContaFixaId = Guid.NewGuid();
                break;
            case "FaturaId":
                lancamento.FaturaId = Guid.NewGuid();
                break;
            case "AssinaturaCartaoId":
                lancamento.AssinaturaCartaoId = Guid.NewGuid();
                break;
            case "ContaReceberId":
                lancamento.ContaReceberId = Guid.NewGuid();
                break;
            case "ConciliadoCom":
                lancamento.ConciliadoCom = Guid.NewGuid();
                break;
            default:
                throw new ArgumentException($"Vinculo desconhecido no teste: {vinculo}", nameof(vinculo));
        }
    }

    // ---------- Null ----------

    [Fact]
    public void ResolverOrigem_ComLancamentoNull_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LancamentoVinculoResolver.ResolverOrigem(null!));
    }

    // ---------- Caso base: avulso e nao manual ----------

    [Fact]
    public void ResolverOrigem_ManualSemNenhumVinculo_RetornaAvulso()
    {
        var lancamento = CriarLancamento(manual: true);

        var resultado = LancamentoVinculoResolver.ResolverOrigem(lancamento);

        Assert.Equal(OrigemVinculoLancamento.Avulso, resultado);
    }

    [Fact]
    public void ResolverOrigem_NaoManualSemNenhumVinculo_RetornaNaoManual()
    {
        var lancamento = CriarLancamento(manual: false);

        var resultado = LancamentoVinculoResolver.ResolverOrigem(lancamento);

        Assert.Equal(OrigemVinculoLancamento.NaoManual, resultado);
    }

    // ---------- Um teste por vinculo (cada vinculo isolado) ----------

    [Theory]
    [InlineData("TransferenciaId", OrigemVinculoLancamento.Transferencia)]
    [InlineData("CompraParceladaId", OrigemVinculoLancamento.CompraParcelada)]
    [InlineData("ContaFixaId", OrigemVinculoLancamento.ContaFixa)]
    [InlineData("FaturaId", OrigemVinculoLancamento.Cartao)]
    [InlineData("AssinaturaCartaoId", OrigemVinculoLancamento.Cartao)]
    [InlineData("ContaReceberId", OrigemVinculoLancamento.ContaReceber)]
    [InlineData("ConciliadoCom", OrigemVinculoLancamento.Conciliado)]
    public void ResolverOrigem_ManualComUnicoVinculo_RetornaOrigemDoVinculo(
        string vinculo,
        OrigemVinculoLancamento origemEsperada)
    {
        var lancamento = CriarLancamento(manual: true, vinculo);

        var resultado = LancamentoVinculoResolver.ResolverOrigem(lancamento);

        Assert.Equal(origemEsperada, resultado);
    }

    // ---------- Precedencia 1: !Manual vence qualquer vinculo ----------

    [Fact]
    public void ResolverOrigem_NaoManualComVinculos_RetornaNaoManualPorPrecedencia()
    {
        var lancamento = CriarLancamento(
            manual: false,
            "TransferenciaId", "ContaFixaId", "FaturaId", "ContaReceberId", "ConciliadoCom");

        var resultado = LancamentoVinculoResolver.ResolverOrigem(lancamento);

        Assert.Equal(OrigemVinculoLancamento.NaoManual, resultado);
    }

    // ---------- Precedencia entre vinculos (o primeiro que casa vence) ----------

    [Fact]
    public void ResolverOrigem_ComTransferenciaETodosOsDemais_RetornaTransferencia()
    {
        var lancamento = CriarLancamento(
            manual: true,
            "TransferenciaId", "CompraParceladaId", "ContaFixaId", "FaturaId", "ContaReceberId", "ConciliadoCom");

        var resultado = LancamentoVinculoResolver.ResolverOrigem(lancamento);

        Assert.Equal(OrigemVinculoLancamento.Transferencia, resultado);
    }

    [Fact]
    public void ResolverOrigem_ComCompraParceladaEContaFixa_RetornaCompraParcelada()
    {
        var lancamento = CriarLancamento(manual: true, "CompraParceladaId", "ContaFixaId");

        var resultado = LancamentoVinculoResolver.ResolverOrigem(lancamento);

        Assert.Equal(OrigemVinculoLancamento.CompraParcelada, resultado);
    }

    [Fact]
    public void ResolverOrigem_ComContaFixaEFatura_RetornaContaFixa()
    {
        var lancamento = CriarLancamento(manual: true, "ContaFixaId", "FaturaId");

        var resultado = LancamentoVinculoResolver.ResolverOrigem(lancamento);

        Assert.Equal(OrigemVinculoLancamento.ContaFixa, resultado);
    }

    [Fact]
    public void ResolverOrigem_ComFaturaEContaReceber_RetornaCartao()
    {
        var lancamento = CriarLancamento(manual: true, "FaturaId", "ContaReceberId");

        var resultado = LancamentoVinculoResolver.ResolverOrigem(lancamento);

        Assert.Equal(OrigemVinculoLancamento.Cartao, resultado);
    }

    [Fact]
    public void ResolverOrigem_ComAssinaturaCartaoEContaReceber_RetornaCartao()
    {
        var lancamento = CriarLancamento(manual: true, "AssinaturaCartaoId", "ContaReceberId");

        var resultado = LancamentoVinculoResolver.ResolverOrigem(lancamento);

        Assert.Equal(OrigemVinculoLancamento.Cartao, resultado);
    }

    [Fact]
    public void ResolverOrigem_ComContaReceberEConciliado_RetornaContaReceber()
    {
        var lancamento = CriarLancamento(manual: true, "ContaReceberId", "ConciliadoCom");

        var resultado = LancamentoVinculoResolver.ResolverOrigem(lancamento);

        Assert.Equal(OrigemVinculoLancamento.ContaReceber, resultado);
    }

    // ---------- EhAvulso ----------

    [Fact]
    public void EhAvulso_ManualSemVinculos_RetornaTrue()
    {
        var lancamento = CriarLancamento(manual: true);

        Assert.True(LancamentoVinculoResolver.EhAvulso(lancamento));
    }

    [Fact]
    public void EhAvulso_NaoManualSemVinculos_RetornaFalse()
    {
        var lancamento = CriarLancamento(manual: false);

        Assert.False(LancamentoVinculoResolver.EhAvulso(lancamento));
    }

    [Theory]
    [InlineData("TransferenciaId")]
    [InlineData("CompraParceladaId")]
    [InlineData("ContaFixaId")]
    [InlineData("FaturaId")]
    [InlineData("AssinaturaCartaoId")]
    [InlineData("ContaReceberId")]
    [InlineData("ConciliadoCom")]
    public void EhAvulso_ManualComQualquerVinculo_RetornaFalse(string vinculo)
    {
        var lancamento = CriarLancamento(manual: true, vinculo);

        Assert.False(LancamentoVinculoResolver.EhAvulso(lancamento));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EhAvulso_ConsistenteComResolverOrigem(bool manual)
    {
        var lancamento = CriarLancamento(manual: manual);

        var avulsoPorResolver = LancamentoVinculoResolver.ResolverOrigem(lancamento) == OrigemVinculoLancamento.Avulso;

        Assert.Equal(avulsoPorResolver, LancamentoVinculoResolver.EhAvulso(lancamento));
    }

    // ---------- ToStorageValue dos 8 valores ----------

    [Theory]
    [InlineData(OrigemVinculoLancamento.Avulso, "AVULSO")]
    [InlineData(OrigemVinculoLancamento.ContaFixa, "CONTA_FIXA")]
    [InlineData(OrigemVinculoLancamento.CompraParcelada, "COMPRA_PARCELADA")]
    [InlineData(OrigemVinculoLancamento.Cartao, "CARTAO")]
    [InlineData(OrigemVinculoLancamento.ContaReceber, "CONTA_RECEBER")]
    [InlineData(OrigemVinculoLancamento.Transferencia, "TRANSFERENCIA")]
    [InlineData(OrigemVinculoLancamento.Conciliado, "CONCILIADO")]
    [InlineData(OrigemVinculoLancamento.NaoManual, "NAO_MANUAL")]
    public void ToStorageValue_CadaOrigem_RetornaValorDeStorage(
        OrigemVinculoLancamento origem,
        string valorEsperado)
    {
        Assert.Equal(valorEsperado, origem.ToStorageValue());
    }
}
