using System.Net;
using System.Text;
using System.Text.Json;
using MyFinances.Domain;
using MyFinances.DTOs;
using Xunit;

namespace MyFinances.Tests.Controllers;

// HTTP da antecipacao de parcelas (item 12). Reusa a fixture de CartaoComprasParceladasControllerTests
// (SQLite em memoria + autenticacao), pois a acao abre transacao real.
public class CartaoAntecipacaoControllerTests : IAsyncLifetime
{
    // Data da antecipacao e sempre hoje no servidor (cliente nao envia data).
    private static DateOnly DataAntecipacaoHoje => DateOnly.FromDateTime(DateTime.Today);
    private const decimal ValorParcela = 100m;

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

    #region GET /api/contas/{contaId}/compras-parceladas/{id}/antecipacoes/elegiveis

    [Fact]
    public async Task ObterElegiveis_CompraComParcelasEmFaturasFuturas_Retorna200ComParcelasDaMaiorParaMenor()
    {
        // Arrange: compra 4x, parcela 1 na fatura aberta (nao elegivel), 2 a 4 em faturas futuras
        var cenario = await SeedCenarioAsync();

        // Act
        var response = await _fixture.Client.GetAsync(
            $"/api/contas/{cenario.ContaId}/compras-parceladas/{cenario.CompraId}/antecipacoes/elegiveis");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var elegiveis = await LerAsync<List<ParcelaElegivelResponse>>(response);

        Assert.Equal(new[] { 4, 3, 2 }, elegiveis.Select(p => p.ParcelaNumero).ToArray());
        Assert.All(elegiveis, p => Assert.Equal(4, p.TotalParcelas));
        Assert.Equal(cenario.FaturaF4Id, elegiveis[0].FaturaId);
        Assert.Equal(new DateOnly(2027, 2, 15), elegiveis[0].FaturaDataVencimento);
        Assert.Equal(ValorParcela, elegiveis[0].Valor);
    }

    [Fact]
    public async Task ObterElegiveis_CompraInexistente_Retorna404()
    {
        // Arrange
        var cenario = await SeedCenarioAsync();

        // Act
        var response = await _fixture.Client.GetAsync(
            $"/api/contas/{cenario.ContaId}/compras-parceladas/{Guid.NewGuid()}/antecipacoes/elegiveis");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("nao encontrada", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region POST /api/contas/{contaId}/compras-parceladas/{id}/antecipacoes

    [Fact]
    public async Task Antecipar_QuantidadeDentroDoElegivel_Retorna200MoveUltimasParcelasParaFaturaAberta()
    {
        // Arrange
        var cenario = await SeedCenarioAsync();

        // Act: antecipa 2 -> deve mover 4/4 e 3/4 para a fatura aberta
        var response = await PostAntecipacaoAsync(cenario, quantidade: 2);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await LerAsync<AnteciparParcelasResponse>(response);

        Assert.Equal(cenario.CompraId, body.CompraParceladaId);
        Assert.Equal(cenario.FaturaAbertaId, body.FaturaDestinoId);
        Assert.Equal(2, body.QuantidadeAntecipada);
        Assert.Equal(2 * ValorParcela, body.ValorAntecipado);

        var numeros = body.Parcelas.Select(p => p.ParcelaNumero!.Value).OrderByDescending(n => n).ToArray();
        Assert.Equal(new[] { 4, 3 }, numeros);
        Assert.All(body.Parcelas, p => Assert.True(p.Antecipada));
        Assert.All(body.Parcelas, p => Assert.Equal(4, p.TotalParcelas));
        Assert.All(body.Parcelas, p => Assert.Equal(DataAntecipacaoHoje, p.Data));
        Assert.All(body.Parcelas, p => Assert.Equal(cenario.FaturaAbertaId, p.FaturaId));
    }

    [Fact]
    public async Task Antecipar_Aplicada_FaturaAtualSobeFuturasCaem()
    {
        // Arrange: antes, fatura aberta = 100 (parcela 1); F2 = 100; F3 = 100; F4 = 100
        var cenario = await SeedCenarioAsync();

        // Act
        var antecipacao = await PostAntecipacaoAsync(cenario, quantidade: 2);
        Assert.Equal(HttpStatusCode.OK, antecipacao.StatusCode);

        var faturasResponse = await _fixture.Client.GetAsync($"/api/contas/{cenario.ContaId}/faturas");
        Assert.Equal(HttpStatusCode.OK, faturasResponse.StatusCode);
        var faturas = await LerAsync<List<FaturaResponse>>(faturasResponse);

        // Assert: aberta sobe para 300; F2 nao muda; F3 e F4 zeram
        Assert.Equal(3 * ValorParcela, faturas.Single(f => f.Id == cenario.FaturaAbertaId).ValorTotal);
        Assert.Equal(ValorParcela, faturas.Single(f => f.Id == cenario.FaturaF2Id).ValorTotal);
        Assert.Equal(0m, faturas.Single(f => f.Id == cenario.FaturaF3Id).ValorTotal);
        Assert.Equal(0m, faturas.Single(f => f.Id == cenario.FaturaF4Id).ValorTotal);

        // Parcelas restantes elegiveis: so a 2/4
        var elegiveisResponse = await _fixture.Client.GetAsync(
            $"/api/contas/{cenario.ContaId}/compras-parceladas/{cenario.CompraId}/antecipacoes/elegiveis");
        var elegiveis = await LerAsync<List<ParcelaElegivelResponse>>(elegiveisResponse);
        Assert.Equal(new[] { 2 }, elegiveis.Select(p => p.ParcelaNumero).ToArray());
    }

    [Fact]
    public async Task Antecipar_QuantidadeAcimaDoDisponivel_Retorna400SemAlterarNada()
    {
        // Arrange: 3 elegiveis; pedido de 4
        var cenario = await SeedCenarioAsync();

        // Act
        var response = await PostAntecipacaoAsync(cenario, quantidade: 4);

        // Assert: rejeicao inteira
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("erro", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var elegiveisResponse = await _fixture.Client.GetAsync(
            $"/api/contas/{cenario.ContaId}/compras-parceladas/{cenario.CompraId}/antecipacoes/elegiveis");
        var elegiveis = await LerAsync<List<ParcelaElegivelResponse>>(elegiveisResponse);
        Assert.Equal(new[] { 4, 3, 2 }, elegiveis.Select(p => p.ParcelaNumero).ToArray());

        var faturasResponse = await _fixture.Client.GetAsync($"/api/contas/{cenario.ContaId}/faturas");
        var faturas = await LerAsync<List<FaturaResponse>>(faturasResponse);
        Assert.Equal(ValorParcela, faturas.Single(f => f.Id == cenario.FaturaAbertaId).ValorTotal);
    }

    [Fact]
    public async Task Antecipar_QuantidadeZero_Retorna400()
    {
        // Arrange
        var cenario = await SeedCenarioAsync();

        // Act
        var response = await PostAntecipacaoAsync(cenario, quantidade: 0);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Antecipar_Repetida_CadaChamadaMoveAProximaParcela_EsgotaElegiveis()
    {
        // Arrange
        var cenario = await SeedCenarioAsync();

        // Act 1: antecipa 1 -> 4/4
        var primeira = await PostAntecipacaoAsync(cenario, quantidade: 1);
        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        Assert.Equal(4, (await LerAsync<AnteciparParcelasResponse>(primeira)).Parcelas.Single().ParcelaNumero);

        // Act 2: antecipa 1 -> 3/4
        var segunda = await PostAntecipacaoAsync(cenario, quantidade: 1);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(3, (await LerAsync<AnteciparParcelasResponse>(segunda)).Parcelas.Single().ParcelaNumero);

        // Act 3: restam so 2/4 elegivel -> pedir 2 deve ser rejeitado
        var terceira = await PostAntecipacaoAsync(cenario, quantidade: 2);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, terceira.StatusCode);
    }

    [Fact]
    public async Task Antecipar_CompraInexistente_Retorna404()
    {
        // Arrange
        var cenario = await SeedCenarioAsync();
        var url = $"/api/contas/{cenario.ContaId}/compras-parceladas/{Guid.NewGuid()}/antecipacoes";

        // Act
        var response = await PostJsonAsync(url, new AnteciparParcelasRequest { Quantidade = 1 });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Antecipar_CompraDeOutraConta_Retorna400()
    {
        // Arrange: compra pertence a conta A; requisicao vai para conta B
        var cenario = await SeedCenarioAsync();
        var contaB = Guid.NewGuid();
        await _fixture.AddContaAsync(new Conta
        {
            Id = contaB,
            Nome = "Cartao B",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        });

        // Act
        var response = await PostJsonAsync(
            $"/api/contas/{contaB}/compras-parceladas/{cenario.CompraId}/antecipacoes",
            new AnteciparParcelasRequest { Quantidade = 1 });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Helpers

    private sealed record Cenario(
        Guid ContaId,
        Guid CompraId,
        Guid FaturaAbertaId,
        Guid FaturaF2Id,
        Guid FaturaF3Id,
        Guid FaturaF4Id);

    // Compra 4x de 100 (ValorTotal 400). Parcela 1 na aberta (10/2026 - venc 11/2026);
    // parcelas 2, 3 e 4 nas faturas futuras (venc 12/2026, 01/2027, 02/2027).
    private async Task<Cenario> SeedCenarioAsync()
    {
        var contaId = Guid.NewGuid();
        var compraId = Guid.NewGuid();

        await _fixture.AddContaAsync(new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        });

        var faturaAberta = NovaFatura(contaId, new DateOnly(2026, 10, 31), new DateOnly(2026, 11, 15), StatusFatura.Aberta);
        var faturaF2 = NovaFatura(contaId, new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 15), StatusFatura.Fechada);
        var faturaF3 = NovaFatura(contaId, new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 15), StatusFatura.Fechada);
        var faturaF4 = NovaFatura(contaId, new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 15), StatusFatura.Fechada);

        foreach (var fatura in new[] { faturaAberta, faturaF2, faturaF3, faturaF4 })
        {
            await _fixture.AddFaturaAsync(fatura);
        }

        var compra = new CompraParcelada
        {
            Id = compraId,
            Descricao = "Notebook Dell",
            ValorTotal = 4 * ValorParcela,
            QuantidadeParcelas = 4,
            DataCompra = new DateOnly(2026, 10, 5),
            Lancamentos = new List<Lancamento>()
        };
        await _fixture.AddCompraParceladaAsync(compra);

        var parcelas = new[]
        {
            (Numero: 1, Fatura: faturaAberta, Data: new DateOnly(2026, 10, 5)),
            (Numero: 2, Fatura: faturaF2, Data: new DateOnly(2026, 11, 5)),
            (Numero: 3, Fatura: faturaF3, Data: new DateOnly(2026, 12, 5)),
            (Numero: 4, Fatura: faturaF4, Data: new DateOnly(2027, 1, 5))
        };

        foreach (var parcela in parcelas)
        {
            await _fixture.AddLancamentoAsync(new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                CompraParceladaId = compraId,
                ParcelaNumero = parcela.Numero,
                FaturaId = parcela.Fatura.Id,
                Valor = ValorParcela,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pendente,
                Data = parcela.Data
            });
        }

        return new Cenario(contaId, compraId, faturaAberta.Id, faturaF2.Id, faturaF3.Id, faturaF4.Id);
    }

    private static Fatura NovaFatura(Guid contaId, DateOnly fechamento, DateOnly vencimento, StatusFatura status)
    {
        return new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = fechamento,
            DataVencimento = vencimento,
            Status = status
        };
    }

    private Task<HttpResponseMessage> PostAntecipacaoAsync(Cenario cenario, int quantidade)
    {
        return PostJsonAsync(
            $"/api/contas/{cenario.ContaId}/compras-parceladas/{cenario.CompraId}/antecipacoes",
            new AnteciparParcelasRequest { Quantidade = quantidade });
    }

    private Task<HttpResponseMessage> PostJsonAsync(string url, AnteciparParcelasRequest request)
    {
        var json = JsonSerializer.Serialize(request, CartaoComprasParceladasControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return _fixture.Client.PostAsync(url, content);
    }

    private static async Task<T> LerAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(body, CartaoComprasParceladasControllerTestsFixture.JsonOptions);
        Assert.NotNull(result);
        return result!;
    }

    #endregion
}
