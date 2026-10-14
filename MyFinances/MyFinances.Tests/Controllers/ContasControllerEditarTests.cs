using System.Net;
using System.Text;
using System.Text.Json;
using MyFinances.Domain;
using MyFinances.DTOs.Conta;
using Xunit;

namespace MyFinances.Tests.Controllers;

// Item 10.2: PUT /api/contas/{id}. Reusa a fixture de ContasControllerTests (mesma collection).
[Collection("Contas Controller Collection")]
public class ContasControllerEditarTests
{
    private readonly ContasControllerTestsFixture _fixture;

    public ContasControllerEditarTests(ContasControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region PUT /api/contas/{id} - Edicao de nome, icone, cor e subtipo

    [Fact]
    public async Task EditarConta_ContaBanco_Retorna200ComNomeNovoEAplicaSubtipoPoupanca()
    {
        // Arrange
        var conta = CriarContaBanco(subtipo: SubtipoConta.Corrente);
        await _fixture.AddContaAsync(conta);

        var request = new EditarContaRequest
        {
            Nome = "Nubank Poupanca",
            Subtipo = "Poupanca",
            Icone = "piggy",
            Cor = "#7C3AED"
        };

        // Act
        var response = await PutEditarAsync(conta.Id, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contaResponse = JsonSerializer.Deserialize<ContaResponse>(responseBody, ContasControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaResponse);
        Assert.Equal("Nubank Poupanca", contaResponse.Nome);
        Assert.Equal(SubtipoConta.Poupanca, contaResponse.Subtipo);
        Assert.Equal("piggy", contaResponse.Icone);
        Assert.Equal("#7C3AED", contaResponse.Cor);

        var contaPersistida = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaPersistida);
        Assert.Equal("Nubank Poupanca", contaPersistida.Nome);
        Assert.Equal(SubtipoConta.Poupanca, contaPersistida.Subtipo);
        Assert.Equal("piggy", contaPersistida.Icone);
        Assert.Equal("#7C3AED", contaPersistida.Cor);
    }

    [Fact]
    public async Task EditarConta_ContaBancoSemSubtipo_FicaComSubtipoNull()
    {
        // Arrange
        var conta = CriarContaBanco(subtipo: SubtipoConta.Corrente);
        await _fixture.AddContaAsync(conta);

        var request = new EditarContaRequest { Nome = "Banco sem subtipo" };

        // Act
        var response = await PutEditarAsync(conta.Id, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contaPersistida = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaPersistida);
        Assert.Null(contaPersistida.Subtipo);
    }

    [Fact]
    public async Task EditarConta_ContaCartaoComSubtipo_SubtipoIgnoradoERespostaOk()
    {
        // Arrange
        var conta = CriarContaCartao();
        await _fixture.AddContaAsync(conta);

        var request = new EditarContaRequest { Nome = "Cartao Renomeado", Subtipo = "Poupanca" };

        // Act
        var response = await PutEditarAsync(conta.Id, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contaResponse = JsonSerializer.Deserialize<ContaResponse>(responseBody, ContasControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaResponse);
        Assert.Null(contaResponse.Subtipo);

        var contaPersistida = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaPersistida);
        Assert.Equal("Cartao Renomeado", contaPersistida.Nome);
        Assert.Null(contaPersistida.Subtipo);
    }

    [Fact]
    public async Task EditarConta_ContaInvestimentoComSubtipo_SubtipoIgnoradoERespostaOk()
    {
        // Arrange
        var conta = CriarContaInvestimento();
        await _fixture.AddContaAsync(conta);

        var request = new EditarContaRequest { Nome = "Corretora Renomeada", Subtipo = "Corrente" };

        // Act
        var response = await PutEditarAsync(conta.Id, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contaResponse = JsonSerializer.Deserialize<ContaResponse>(responseBody, ContasControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaResponse);
        Assert.Null(contaResponse.Subtipo);

        var contaPersistida = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaPersistida);
        Assert.Equal("Corretora Renomeada", contaPersistida.Nome);
        Assert.Null(contaPersistida.Subtipo);
    }

    #endregion

    #region PUT /api/contas/{id} - Tipo imutavel

    [Fact]
    public async Task EditarConta_JsonComTipoCartaoEmContaBanco_TipoPermaneceBanco()
    {
        // Arrange
        var conta = CriarContaBanco(subtipo: SubtipoConta.Corrente);
        await _fixture.AddContaAsync(conta);

        var body = new { Nome = "Tentando trocar tipo", Tipo = "Cartao" };

        // Act
        var response = await PutEditarAsync(conta.Id, body);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contaResponse = JsonSerializer.Deserialize<ContaResponse>(responseBody, ContasControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaResponse);
        Assert.Equal(TipoConta.Banco, contaResponse.Tipo);

        var contaPersistida = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaPersistida);
        Assert.Equal(TipoConta.Banco, contaPersistida.Tipo);
    }

    #endregion

    #region PUT /api/contas/{id} - Validacoes (400)

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EditarConta_NomeVazioOuSoEspacos_Retorna400ENomeInalterado(string nome)
    {
        // Arrange
        var conta = CriarContaBanco(subtipo: SubtipoConta.Corrente);
        await _fixture.AddContaAsync(conta);

        var request = new EditarContaRequest { Nome = nome, Subtipo = "Poupanca" };

        // Act
        var response = await PutEditarAsync(conta.Id, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, ContasControllerTestsFixture.JsonOptions);
        Assert.NotNull(errorResponse);
        Assert.Contains("Nome", errorResponse["erro"]);

        var contaPersistida = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaPersistida);
        Assert.Equal(conta.Nome, contaPersistida.Nome);
        Assert.Equal(SubtipoConta.Corrente, contaPersistida.Subtipo);
    }

    [Fact]
    public async Task EditarConta_SubtipoNaoReconhecidoEmBanco_Retorna400ComErro()
    {
        // Arrange
        var conta = CriarContaBanco(subtipo: SubtipoConta.Corrente);
        await _fixture.AddContaAsync(conta);

        var request = new EditarContaRequest { Nome = "Nome novo", Subtipo = "SubtipoDesconhecido" };

        // Act
        var response = await PutEditarAsync(conta.Id, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, ContasControllerTestsFixture.JsonOptions);
        Assert.NotNull(errorResponse);
        Assert.Contains("Subtipo", errorResponse["erro"]);
        Assert.Contains("nao e valido", errorResponse["erro"]);

        var contaPersistida = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaPersistida);
        Assert.Equal(conta.Nome, contaPersistida.Nome);
    }

    #endregion

    #region PUT /api/contas/{id} - Conta inexistente (404) e inativa (409)

    [Fact]
    public async Task EditarConta_ContaInexistente_Retorna404()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        var request = new EditarContaRequest { Nome = "Qualquer" };

        // Act
        var response = await PutEditarAsync(idInexistente, request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EditarConta_ContaInativa_Retorna409ENaoAlteraConta()
    {
        // Arrange
        var conta = CriarContaBanco(subtipo: SubtipoConta.Corrente, ativa: false);
        await _fixture.AddContaAsync(conta);

        var request = new EditarContaRequest { Nome = "Tentativa em conta inativa", Subtipo = "Poupanca" };

        // Act
        var response = await PutEditarAsync(conta.Id, request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var contaPersistida = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaPersistida);
        Assert.Equal(conta.Nome, contaPersistida.Nome);
        Assert.Equal(SubtipoConta.Corrente, contaPersistida.Subtipo);
    }

    #endregion

    #region PUT /api/contas/{id} - Campos fora da edicao

    [Fact]
    public async Task EditarConta_NaoAlteraSaldoManualDiaFechamentoNemDiaVencimento()
    {
        // Arrange
        var conta = CriarContaCartao(saldoManual: -250m, diaFechamento: 10, diaVencimento: 17);
        await _fixture.AddContaAsync(conta);

        var request = new EditarContaRequest { Nome = "Cartao Novo Nome", Icone = "card", Cor = "#111111" };

        // Act
        var response = await PutEditarAsync(conta.Id, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contaResponse = JsonSerializer.Deserialize<ContaResponse>(responseBody, ContasControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaResponse);
        Assert.Equal(-250m, contaResponse.SaldoManual);
        Assert.Equal(10, contaResponse.DiaFechamento);
        Assert.Equal(17, contaResponse.DiaVencimento);

        var contaPersistida = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaPersistida);
        Assert.Equal(-250m, contaPersistida.SaldoManual);
        Assert.Equal(10, contaPersistida.DiaFechamento);
        Assert.Equal(17, contaPersistida.DiaVencimento);
    }

    #endregion

    #region Helpers

    private async Task<HttpResponseMessage> PutEditarAsync(Guid id, object body)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _fixture.Client.PutAsync($"/api/contas/{id}", content);
    }

    private static Conta CriarContaBanco(SubtipoConta? subtipo = null, bool ativa = true)
    {
        return new Conta
        {
            Id = Guid.NewGuid(),
            Nome = $"Banco {Guid.NewGuid():N}",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Subtipo = subtipo,
            Icone = "home",
            Cor = "#000000",
            SaldoManual = 1000m,
            Ativa = ativa
        };
    }

    private static Conta CriarContaCartao(decimal? saldoManual = 0m, int? diaFechamento = 5, int? diaVencimento = 12)
    {
        return new Conta
        {
            Id = Guid.NewGuid(),
            Nome = $"Cartao {Guid.NewGuid():N}",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            SaldoManual = saldoManual,
            DiaFechamento = diaFechamento,
            DiaVencimento = diaVencimento,
            Ativa = true
        };
    }

    private static Conta CriarContaInvestimento()
    {
        return new Conta
        {
            Id = Guid.NewGuid(),
            Nome = $"Corretora {Guid.NewGuid():N}",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 500m,
            Ativa = true
        };
    }

    #endregion
}
