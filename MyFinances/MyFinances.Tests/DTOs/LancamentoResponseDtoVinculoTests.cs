using MyFinances.Domain;
using MyFinances.DTOs;
using Xunit;

namespace MyFinances.Tests.DTOs;

// Regra-de-negocio item 18 e 17: LancamentoResponseDto expoe Avulso, OrigemVinculo
// e os campos de parcela (CompraParceladaId, ParcelaNumero, TotalParcelas).
public class LancamentoResponseDtoVinculoTests
{
    private static Lancamento CriarLancamento(bool manual = true)
    {
        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Descricao = "Lancamento",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 4, 1),
            Status = StatusLancamento.Pendente,
            Manual = manual
        };
    }

    [Fact]
    public void FromLancamento_AvulsoSemVinculo_MarcaAvulsoEOrigemAVULSO()
    {
        var lancamento = CriarLancamento(manual: true);

        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        Assert.True(dto.Avulso);
        Assert.Equal("AVULSO", dto.OrigemVinculo);
    }

    [Theory]
    [InlineData("TransferenciaId", "TRANSFERENCIA")]
    [InlineData("ContaFixaId", "CONTA_FIXA")]
    [InlineData("FaturaId", "CARTAO")]
    [InlineData("AssinaturaCartaoId", "CARTAO")]
    [InlineData("ContaReceberId", "CONTA_RECEBER")]
    [InlineData("ConciliadoCom", "CONCILIADO")]
    public void FromLancamento_ComVinculo_MarcaAvulsoFalsoEInformaOrigem(string vinculo, string origemEsperada)
    {
        var lancamento = CriarLancamento(manual: true);
        switch (vinculo)
        {
            case "TransferenciaId":
                lancamento.TransferenciaId = Guid.NewGuid();
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
        }

        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        Assert.False(dto.Avulso);
        Assert.Equal(origemEsperada, dto.OrigemVinculo);
    }

    [Fact]
    public void FromLancamento_NaoManual_MarcaAvulsoFalsoEOrigemNAO_MANUAL()
    {
        var lancamento = CriarLancamento(manual: false);

        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        Assert.False(dto.Avulso);
        Assert.Equal("NAO_MANUAL", dto.OrigemVinculo);
    }

    [Fact]
    public void FromLancamento_ParcelaDeCompraParcelada_PreencheIdParcelaETotalDaNavegacao()
    {
        var compraId = Guid.NewGuid();
        var lancamento = CriarLancamento(manual: true);
        lancamento.CompraParceladaId = compraId;
        lancamento.ParcelaNumero = 3;
        lancamento.CompraParcelada = new CompraParcelada
        {
            Id = compraId,
            Descricao = "Notebook",
            ValorTotal = 3000m,
            QuantidadeParcelas = 10,
            DataCompra = new DateOnly(2025, 1, 10)
        };

        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        Assert.Equal(compraId, dto.CompraParceladaId);
        Assert.Equal(3, dto.ParcelaNumero);
        Assert.Equal(10, dto.TotalParcelas);
        Assert.Equal("COMPRA_PARCELADA", dto.OrigemVinculo);
        Assert.False(dto.Avulso);
    }

    [Fact]
    public void FromLancamento_ParcelaSemNavegacaoCarregada_TotalParcelasNulo()
    {
        var compraId = Guid.NewGuid();
        var lancamento = CriarLancamento(manual: true);
        lancamento.CompraParceladaId = compraId;
        lancamento.ParcelaNumero = 2;
        lancamento.CompraParcelada = null;

        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        Assert.Equal(compraId, dto.CompraParceladaId);
        Assert.Equal(2, dto.ParcelaNumero);
        Assert.Null(dto.TotalParcelas);
    }

    [Fact]
    public void FromLancamento_SemCompraParcelada_CamposDeParcelaNulos()
    {
        var lancamento = CriarLancamento(manual: true);

        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        Assert.Null(dto.CompraParceladaId);
        Assert.Null(dto.ParcelaNumero);
        Assert.Null(dto.TotalParcelas);
    }
}
