using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyFinances.Data;
using MyFinances.Dtos.Conta;
using MyFinances.Models;
using Xunit;

namespace MyFinances.Tests.Controllers;

public class ContasControllerWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
            {
                var dbContextDescriptor = services.FirstOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<MyFinancesDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                var efServiceDescriptors = services
                    .Where(d => d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true
                                || d.ServiceType.FullName?.StartsWith("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in efServiceDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<MyFinancesDbContext>(options =>
                    options.UseInMemoryDatabase("ContasControllerTestDb"));
            });
    }
}

public class ContasControllerTestsFixture : IAsyncLifetime
{
    private readonly ContasControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ContasControllerTestsFixture()
    {
        _factory = new ContasControllerWebApplicationFactory();
    }

    public async Task InitializeAsync()
    {
        Client = _factory.CreateClient();
        try
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize test database: {ex.Message}", ex);
        }
    }

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        Client.Dispose();
        _factory.Dispose();
    }

    public async Task AddContaAsync(Conta conta)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Contas.Add(conta);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Conta?> GetContaByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Contas.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Contas.RemoveRange(await dbContext.Contas.ToListAsync());
        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition("Contas Controller Collection")]
public class ContasControllerCollection : ICollectionFixture<ContasControllerTestsFixture>
{
}

[Collection("Contas Controller Collection")]
public class ContasControllerTests
{
    private readonly ContasControllerTestsFixture _fixture;

    public ContasControllerTests(ContasControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region POST /api/contas - Criar conta de investimento

    [Fact]
    public async Task CriarContaInvestimento_ComCorpoValido_Retorna201ComLocationHeader()
    {
        // Arrange
        var request = new CriarContaInvestimentoRequest
        {
            Nome = "Cofrinho Mercado Pago",
            SaldoInicial = 1000m
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/contas", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/api/contas/", response.Headers.Location?.ToString());

        var responseBody = await response.Content.ReadAsStringAsync();
        var contaResponse = JsonSerializer.Deserialize<ContaResponse>(responseBody, ContasControllerTestsFixture.JsonOptions);

        Assert.NotNull(contaResponse);
        Assert.NotEqual(Guid.Empty, contaResponse.Id);
        Assert.Equal("Cofrinho Mercado Pago", contaResponse.Nome);
        Assert.Equal(TipoConta.Investimento, contaResponse.Tipo);
        Assert.Equal(OrigemConta.Manual, contaResponse.Origem);
        Assert.Equal(1000m, contaResponse.SaldoManual);
        Assert.True(contaResponse.Ativa);
    }

    [Fact]
    public async Task CriarContaInvestimento_ComSaldoZero_Retorna201()
    {
        // Arrange
        var request = new CriarContaInvestimentoRequest
        {
            Nome = "Investimentos XP",
            SaldoInicial = 0m
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/contas", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contaResponse = JsonSerializer.Deserialize<ContaResponse>(responseBody, ContasControllerTestsFixture.JsonOptions);

        Assert.NotNull(contaResponse);
        Assert.Equal(0m, contaResponse.SaldoManual);
    }

    [Fact]
    public async Task CriarContaInvestimento_ComSaldoNegativo_Retorna201()
    {
        // Arrange
        var request = new CriarContaInvestimentoRequest
        {
            Nome = "Carteira de Acoes",
            SaldoInicial = -500m
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/contas", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contaResponse = JsonSerializer.Deserialize<ContaResponse>(responseBody, ContasControllerTestsFixture.JsonOptions);

        Assert.NotNull(contaResponse);
        Assert.Equal(-500m, contaResponse.SaldoManual);
    }

    #endregion

    #region GET /api/contas - Listar contas

    [Fact]
    public async Task ListarContas_ComTipoInvestimento_Retorna200ComListaDeContas()
    {
        await _fixture.ClearAsync();
        // Arrange
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Cofrinho Mercado Pago",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta);

        // Act
        var response = await _fixture.Client.GetAsync("/api/contas?tipo=investimento");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contas = JsonSerializer.Deserialize<List<ContaResponse>>(responseBody, ContasControllerTestsFixture.JsonOptions);

        Assert.NotNull(contas);
        Assert.Single(contas);
        Assert.Equal(conta.Id, contas[0].Id);
        Assert.Equal("Cofrinho Mercado Pago", contas[0].Nome);
        Assert.Equal(TipoConta.Investimento, contas[0].Tipo);
        Assert.Equal(OrigemConta.Manual, contas[0].Origem);
    }

    [Fact]
    public async Task ListarContas_SemParametroTipo_Retorna200ComListaDeContas()
    {
        await _fixture.ClearAsync();
        // Arrange
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Cofrinho Mercado Pago",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta);

        // Act
        var response = await _fixture.Client.GetAsync("/api/contas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contas = JsonSerializer.Deserialize<List<ContaResponse>>(responseBody, ContasControllerTestsFixture.JsonOptions);

        Assert.NotNull(contas);
        Assert.Single(contas);
    }

    [Fact]
    public async Task ListarContas_ComTipoInvalido_Retorna400()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/api/contas?tipo=banco");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, ContasControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
        Assert.NotEmpty(errorResponse["erro"]);
    }

    [Fact]
    public async Task ListarContas_NaoRetornaContasDesativadas()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaAuditada = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Ativa",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        var contaDesativada = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Desativada",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 500m,
            Ativa = false
        };

        await _fixture.AddContaAsync(contaAuditada);
        await _fixture.AddContaAsync(contaDesativada);

        // Act
        var response = await _fixture.Client.GetAsync("/api/contas?tipo=investimento");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contas = JsonSerializer.Deserialize<List<ContaResponse>>(responseBody, ContasControllerTestsFixture.JsonOptions);

        Assert.NotNull(contas);
        Assert.Single(contas);
        Assert.Equal("Conta Ativa", contas[0].Nome);
    }

    [Fact]
    public async Task ListarContas_Vazio_Retorna200ComListaVazia()
    {
        await _fixture.ClearAsync();
        // Act
        var response = await _fixture.Client.GetAsync("/api/contas?tipo=investimento");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var contas = JsonSerializer.Deserialize<List<ContaResponse>>(responseBody, ContasControllerTestsFixture.JsonOptions);

        Assert.NotNull(contas);
        Assert.Empty(contas);
    }

    #endregion

    #region PATCH /api/contas/{id}/saldo - Atualizar saldo

    [Fact]
    public async Task AtualizarSaldo_ComIdExistente_Retorna204()
    {
        // Arrange
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Cofrinho",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta);

        var request = new AtualizarSaldoRequest { NovoSaldo = 2000m };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PatchAsync($"/api/contas/{conta.Id}/saldo", content);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verificar que o saldo foi atualizado no banco
        var contaAposAtualizacao = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaAposAtualizacao);
        Assert.Equal(2000m, contaAposAtualizacao.SaldoManual);
    }

    [Fact]
    public async Task AtualizarSaldo_ComIdInexistente_Retorna404()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        var request = new AtualizarSaldoRequest { NovoSaldo = 1000m };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PatchAsync($"/api/contas/{idInexistente}/saldo", content);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, ContasControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
        Assert.NotEmpty(errorResponse["erro"]);
    }

    [Fact]
    public async Task AtualizarSaldo_ComNovoSaldoZero_Retorna204()
    {
        // Arrange
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Cofrinho",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta);

        var request = new AtualizarSaldoRequest { NovoSaldo = 0m };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PatchAsync($"/api/contas/{conta.Id}/saldo", content);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var contaAposAtualizacao = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaAposAtualizacao);
        Assert.Equal(0m, contaAposAtualizacao.SaldoManual);
    }

    #endregion

    #region PATCH /api/contas/{id}/desativar - Desativar conta

    [Fact]
    public async Task DesativarConta_ComIdExistente_Retorna204()
    {
        // Arrange
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Cofrinho",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta);

        // Act
        var response = await _fixture.Client.PatchAsync($"/api/contas/{conta.Id}/desativar", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verificar que a conta foi desativada
        var contaAposDesativacao = await _fixture.GetContaByIdAsync(conta.Id);
        Assert.NotNull(contaAposDesativacao);
        Assert.False(contaAposDesativacao.Ativa);
    }

    [Fact]
    public async Task DesativarConta_ComIdExistente_NaoApareceMaisEmListagem()
    {
        await _fixture.ClearAsync();
        // Arrange
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Cofrinho",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta);

        // Verificar que aparece na listagem antes de desativar
        var responseAntes = await _fixture.Client.GetAsync("/api/contas?tipo=investimento");
        var contasAntes = JsonSerializer.Deserialize<List<ContaResponse>>(
            await responseAntes.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(contasAntes);
        Assert.Single(contasAntes);

        // Act - Desativar
        await _fixture.Client.PatchAsync($"/api/contas/{conta.Id}/desativar", null);

        // Assert - Verificar que nao aparece mais na listagem
        var responseDepois = await _fixture.Client.GetAsync("/api/contas?tipo=investimento");
        var contasDepois = JsonSerializer.Deserialize<List<ContaResponse>>(
            await responseDepois.Content.ReadAsStringAsync(),
            ContasControllerTestsFixture.JsonOptions);
        Assert.NotNull(contasDepois);
        Assert.Empty(contasDepois);
    }

    [Fact]
    public async Task DesativarConta_ComIdInexistente_Retorna404()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await _fixture.Client.PatchAsync($"/api/contas/{idInexistente}/desativar", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, ContasControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
        Assert.NotEmpty(errorResponse["erro"]);
    }

    #endregion
}
