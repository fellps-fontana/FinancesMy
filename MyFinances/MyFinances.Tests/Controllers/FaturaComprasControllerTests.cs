using System.Net;
using System.Text.Json;
using MyFinances.Domain;
using MyFinances.DTOs;
using Xunit;

namespace MyFinances.Tests.Controllers;

// HTTP de GET /api/contas/{contaId}/faturas/{faturaId}/compras. Reusa a fixture de
// CartaoComprasParceladasControllerTests (SQLite em memoria + autenticacao), padrao
// de CartaoAntecipacaoControllerTests.
public class FaturaComprasControllerTests : IAsyncLifetime
{
    private CartaoComprasParceladasControllerTestsFixture _fixture = null!;

    public async Task InitializeAsync()
    {
        _fixture = new CartaoComprasParceladasControllerTestsFixture();
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    #region GET /api/contas/{contaId}/faturas/{faturaId}/compras

    [Fact]
    public async Task ListarComprasDaFatura_ComParcelaAntecipada_Retorna200ComCamposDeParcela()
    {
        // Arrange: fatura com parcela 2/3 antecipada (data anterior) e parcela 1/3 normal
        var contaId = Guid.NewGuid();
        var faturaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        await _fixture.AddContaAsync(CriarCartao(contaId));
        await _fixture.AddFaturaAsync(CriarFatura(faturaId, contaId));
        await _fixture.AddCompraParceladaAsync(new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Notebook Dell",
            ValorTotal = 300m,
            QuantidadeParcelas = 3,
            DataCompra = new DateOnly(2026, 1, 5),
            Lancamentos = new List<Lancamento>()
        });
        await _fixture.AddLancamentoAsync(CriarParcela(contaId, faturaId, compraParceladaId, 1, new DateOnly(2026, 2, 5), antecipada: false));
        await _fixture.AddLancamentoAsync(CriarParcela(contaId, faturaId, compraParceladaId, 2, new DateOnly(2026, 1, 20), antecipada: true));

        // Act
        var response = await _fixture.Client.GetAsync($"/api/contas/{contaId}/faturas/{faturaId}/compras");

        // Assert: ordenado por data (antecipada primeiro) e campos de parcela preenchidos
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var compras = JsonSerializer.Deserialize<List<CompraResponse>>(body, CartaoComprasParceladasControllerTestsFixture.JsonOptions)!;

        Assert.Equal(2, compras.Count);

        var antecipada = compras[0];
        Assert.True(antecipada.Antecipada);
        Assert.Equal(2, antecipada.ParcelaNumero);
        Assert.Equal(3, antecipada.TotalParcelas);
        Assert.Equal(compraParceladaId, antecipada.CompraParceladaId);
        Assert.Equal(faturaId, antecipada.FaturaId);

        Assert.False(compras[1].Antecipada);
        Assert.Equal(1, compras[1].ParcelaNumero);
    }

    [Fact]
    public async Task ListarComprasDaFatura_FaturaDeOutraConta_Retorna404()
    {
        // Arrange: fatura pertence a conta B, requisicao feita pela conta A
        var contaAId = Guid.NewGuid();
        var contaBId = Guid.NewGuid();
        var faturaId = Guid.NewGuid();

        await _fixture.AddContaAsync(CriarCartao(contaAId));
        await _fixture.AddContaAsync(CriarCartao(contaBId));
        await _fixture.AddFaturaAsync(CriarFatura(faturaId, contaBId));

        // Act
        var response = await _fixture.Client.GetAsync($"/api/contas/{contaAId}/faturas/{faturaId}/compras");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListarComprasDaFatura_FaturaInexistente_Retorna404()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarCartao(contaId));

        // Act
        var response = await _fixture.Client.GetAsync($"/api/contas/{contaId}/faturas/{Guid.NewGuid()}/compras");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    private static Conta CriarCartao(Guid contaId)
    {
        return new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        };
    }

    private static Fatura CriarFatura(Guid faturaId, Guid contaId)
    {
        return new Fatura
        {
            Id = faturaId,
            ContaId = contaId,
            DataFechamento = new DateOnly(2026, 2, 1),
            DataVencimento = new DateOnly(2026, 2, 15),
            Status = StatusFatura.Aberta
        };
    }

    private static Lancamento CriarParcela(
        Guid contaId,
        Guid faturaId,
        Guid compraParceladaId,
        int parcelaNumero,
        DateOnly data,
        bool antecipada)
    {
        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            FaturaId = faturaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = parcelaNumero,
            Antecipada = antecipada,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pendente,
            Data = data
        };
    }
}
