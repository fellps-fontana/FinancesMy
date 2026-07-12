using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyFinances.Data;
using MyFinances.DTOs.Ativo;
using MyFinances.DTOs;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Controllers;

public class AtivosControllerWebApplicationFactory : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("AtivosControllerTestDb"));
        });
    }
}

public class AtivosControllerTestsFixture : IAsyncLifetime
{
    private readonly AtivosControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public AtivosControllerTestsFixture()
    {
        _factory = new AtivosControllerWebApplicationFactory();
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

        await AutenticarClientAsync();
    }

    private async Task AutenticarClientAsync()
    {
        var registerRequest = new RegistrarUsuarioRequest
        {
            Username = $"ativos_test_{Guid.NewGuid():N}",
            Email = $"ativos_test_{Guid.NewGuid():N}@example.com",
            Senha = "SenhaForteDeTeste123!"
        };

        var registerResponse = await Client.PostAsJsonAsync("/api/auth/registrar", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = registerRequest.Username,
            Senha = registerRequest.Senha
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginData = JsonSerializer.Deserialize<LoginResponse>(loginBody, JsonOptions);

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginData!.Token);
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

    public async Task AddAtivoAsync(Ativo ativo)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Ativos.Add(ativo);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Ativo?> GetAtivoByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Ativos.FirstOrDefaultAsync(a => a.Id == id);
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
        dbContext.Ativos.RemoveRange(await dbContext.Ativos.ToListAsync());
        dbContext.Contas.RemoveRange(await dbContext.Contas.ToListAsync());
        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition("Ativos Controller Collection")]
public class AtivosControllerCollection : ICollectionFixture<AtivosControllerTestsFixture>
{
}

[Collection("Ativos Controller Collection")]
public class AtivosControllerTests
{
    private readonly AtivosControllerTestsFixture _fixture;

    public AtivosControllerTests(AtivosControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region GET /api/contas/{contaId}/ativos - Listar ativos

    [Fact]
    public async Task ListarAtivos_DeContaNaoExistente_Retorna404()
    {
        // Arrange
        var contaNaoExistenteId = Guid.NewGuid();

        // Act
        var response = await _fixture.Client.GetAsync($"/api/contas/{contaNaoExistenteId}/ativos");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListarAtivos_DeContaNaoTipoInvestimento_Retorna422()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaBanco = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaBanco);

        // Act
        var response = await _fixture.Client.GetAsync($"/api/contas/{contaBanco.Id}/ativos");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ListarAtivos_DeContaInvestimentoSemAtivos_Retorna200ComListaVazia()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 0m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaInvestimento);

        // Act
        var response = await _fixture.Client.GetAsync($"/api/contas/{contaInvestimento.Id}/ativos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var ativos = JsonSerializer.Deserialize<List<AtivoResponse>>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativos);
        Assert.Empty(ativos);
    }

    #endregion

    #region POST /api/contas/{contaId}/ativos/compras - Registrar compra

    [Fact]
    public async Task RegistrarCompra_EmContaNaoExistente_Retorna404()
    {
        // Arrange
        var contaNaoExistenteId = Guid.NewGuid();
        var request = new RegistrarCompraRequest
        {
            Ticker = "PETR4",
            Quantidade = 10m,
            PrecoUnitario = 25.50m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaNaoExistenteId}/ativos/compras",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarCompra_EmContaNaoTipoInvestimento_Retorna422()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaBanco = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaBanco);

        var request = new RegistrarCompraRequest
        {
            Ticker = "PETR4",
            Quantidade = 10m,
            PrecoUnitario = 25.50m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaBanco.Id}/ativos/compras",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarCompra_ComQuantidadeZero_Retorna400()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaInvestimento);

        var request = new RegistrarCompraRequest
        {
            Ticker = "PETR4",
            Quantidade = 0m,
            PrecoUnitario = 25.50m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/compras",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarCompra_NovoAtivo_Retorna201ComAtivoResponse()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaInvestimento);

        var request = new RegistrarCompraRequest
        {
            Ticker = "PETR4",
            Quantidade = 10m,
            PrecoUnitario = 25.50m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            Nome = "Petrobras"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/compras",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var responseBody = await response.Content.ReadAsStringAsync();
        var ativoResponse = JsonSerializer.Deserialize<AtivoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativoResponse);
        Assert.NotEqual(Guid.Empty, ativoResponse.Id);
        Assert.Equal("PETR4", ativoResponse.Ticker);
        Assert.Equal("Petrobras", ativoResponse.Nome);
        Assert.Equal(10m, ativoResponse.Quantidade);
        Assert.Equal(25.50m, ativoResponse.PrecoMedio);
        Assert.Equal(25.50m, ativoResponse.PrecoAtual);
        Assert.True(ativoResponse.Ativa);
    }

    [Fact]
    public async Task RegistrarCompra_AtivoExistenteComMesmoTicker_AtualizaPrecoMedioCorretamente()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        var ativoExistente = new Ativo
        {
            Id = Guid.NewGuid(),
            ContaId = contaInvestimento.Id,
            Ticker = "PETR4",
            Nome = "Petrobras",
            Quantidade = 10m,
            PrecoMedio = 20m,
            PrecoAtual = 20m,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddContaAsync(contaInvestimento);
        await _fixture.AddAtivoAsync(ativoExistente);

        // Segunda compra: 20 unidades a 30 reais
        // Preco medio esperado: (20 * 10 + 30 * 20) / (10 + 20) = (200 + 600) / 30 = 800 / 30 = 26.67
        var request = new RegistrarCompraRequest
        {
            Ticker = "PETR4",
            Quantidade = 20m,
            PrecoUnitario = 30m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/compras",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var ativoResponse = JsonSerializer.Deserialize<AtivoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativoResponse);
        Assert.Equal(30m, ativoResponse.Quantidade);
        Assert.Equal(26.67m, ativoResponse.PrecoMedio, 2);
        Assert.Equal(30m, ativoResponse.PrecoAtual);
        Assert.Equal(ativoExistente.Id, ativoResponse.Id);
    }

    #endregion

    #region POST /api/contas/{contaId}/ativos/{ativoId}/vendas - Registrar venda

    [Fact]
    public async Task RegistrarVenda_AtivoQueNaoPertenceAContaInformada_Retorna404()
    {
        await _fixture.ClearAsync();
        // Arrange - PRIORIDADE #1
        // Cria duas contas de investimento
        var conta1 = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira 1",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        var conta2 = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira 2",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta1);
        await _fixture.AddContaAsync(conta2);

        // Cria ativo na conta1
        var ativoNaConta1 = new Ativo
        {
            Id = Guid.NewGuid(),
            ContaId = conta1.Id,
            Ticker = "PETR4",
            Nome = "Petrobras",
            Quantidade = 10m,
            PrecoMedio = 25m,
            PrecoAtual = 25m,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativoNaConta1);

        // Tenta vender passando conta2 na URL
        var vendaRequest = new RegistrarVendaRequest
        {
            Quantidade = 5m,
            PrecoUnitario = 30m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(vendaRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{conta2.Id}/ativos/{ativoNaConta1.Id}/vendas",
            content);

        // Assert - Deve retornar 404
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verifica que o ativo NAO foi alterado (quantidade intacta)
        var ativoAposVendaFalhada = await _fixture.GetAtivoByIdAsync(ativoNaConta1.Id);
        Assert.NotNull(ativoAposVendaFalhada);
        Assert.Equal(10m, ativoAposVendaFalhada.Quantidade);
        Assert.True(ativoAposVendaFalhada.Ativa);
    }

    [Fact]
    public async Task RegistrarVenda_DeQuantidadeMaiorQuePosicao_Retorna422()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            ContaId = contaInvestimento.Id,
            Ticker = "PETR4",
            Nome = "Petrobras",
            Quantidade = 10m,
            PrecoMedio = 25m,
            PrecoAtual = 25m,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddContaAsync(contaInvestimento);
        await _fixture.AddAtivoAsync(ativo);

        var vendaRequest = new RegistrarVendaRequest
        {
            Quantidade = 15m,
            PrecoUnitario = 30m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(vendaRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/{ativo.Id}/vendas",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarVenda_ComQuantidadeZero_Retorna400()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            ContaId = contaInvestimento.Id,
            Ticker = "PETR4",
            Nome = "Petrobras",
            Quantidade = 10m,
            PrecoMedio = 25m,
            PrecoAtual = 25m,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddContaAsync(contaInvestimento);
        await _fixture.AddAtivoAsync(ativo);

        var vendaRequest = new RegistrarVendaRequest
        {
            Quantidade = 0m,
            PrecoUnitario = 30m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(vendaRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/{ativo.Id}/vendas",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarVenda_VendaParcial_AtualizaQuantidadeApenasEPrecoMedioInalterado()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            ContaId = contaInvestimento.Id,
            Ticker = "PETR4",
            Nome = "Petrobras",
            Quantidade = 20m,
            PrecoMedio = 25m,
            PrecoAtual = 25m,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddContaAsync(contaInvestimento);
        await _fixture.AddAtivoAsync(ativo);

        var vendaRequest = new RegistrarVendaRequest
        {
            Quantidade = 8m,
            PrecoUnitario = 30m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(vendaRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/{ativo.Id}/vendas",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var ativoResponse = JsonSerializer.Deserialize<AtivoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativoResponse);
        Assert.Equal(12m, ativoResponse.Quantidade);
        Assert.Equal(25m, ativoResponse.PrecoMedio);
        Assert.True(ativoResponse.Ativa);
    }

    [Fact]
    public async Task RegistrarVenda_VendaTotal_DesativaAtivo()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            ContaId = contaInvestimento.Id,
            Ticker = "PETR4",
            Nome = "Petrobras",
            Quantidade = 10m,
            PrecoMedio = 25m,
            PrecoAtual = 25m,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddContaAsync(contaInvestimento);
        await _fixture.AddAtivoAsync(ativo);

        var vendaRequest = new RegistrarVendaRequest
        {
            Quantidade = 10m,
            PrecoUnitario = 30m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(vendaRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/{ativo.Id}/vendas",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var ativoResponse = JsonSerializer.Deserialize<AtivoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativoResponse);
        Assert.Equal(0m, ativoResponse.Quantidade);
        Assert.False(ativoResponse.Ativa);
    }

    #endregion

    #region Fluxo completo - compra, compra novamente, venda parcial, venda total

    [Fact]
    public async Task FluxoCompleto_ComprarDuasVezesVenderParcialETotal_ResultadosCorretos()
    {
        await _fixture.ClearAsync();
        // Arrange - Criar conta de investimento
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira de Acoes",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaInvestimento);

        // Act 1 - Primeira compra: 10 PETR4 a 20 reais
        var compra1Request = new RegistrarCompraRequest
        {
            Ticker = "PETR4",
            Quantidade = 10m,
            PrecoUnitario = 20m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            Nome = "Petrobras"
        };

        var json1 = JsonSerializer.Serialize(compra1Request);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");
        var response1 = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/compras",
            content1);

        // Assert 1
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var responseBody1 = await response1.Content.ReadAsStringAsync();
        var ativo1 = JsonSerializer.Deserialize<AtivoResponse>(responseBody1, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativo1);
        var ativoId = ativo1.Id;
        Assert.Equal(10m, ativo1.Quantidade);
        Assert.Equal(20m, ativo1.PrecoMedio);
        Assert.Equal(20m, ativo1.PrecoAtual);

        // Act 2 - Segunda compra: 20 PETR4 a 30 reais
        // Esperado: quantidade = 30, preco_medio = (20*10 + 30*20)/(10+20) = 26.67
        var compra2Request = new RegistrarCompraRequest
        {
            Ticker = "PETR4",
            Quantidade = 20m,
            PrecoUnitario = 30m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json2 = JsonSerializer.Serialize(compra2Request);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");
        var response2 = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/compras",
            content2);

        // Assert 2
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var responseBody2 = await response2.Content.ReadAsStringAsync();
        var ativo2 = JsonSerializer.Deserialize<AtivoResponse>(responseBody2, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativo2);
        Assert.Equal(30m, ativo2.Quantidade);
        Assert.Equal(26.67m, ativo2.PrecoMedio, 2);
        Assert.Equal(30m, ativo2.PrecoAtual);

        // Act 3 - Venda parcial: vender 12 unidades a 35 reais
        // Esperado: quantidade = 18, preco_medio = 26.67 (nao muda em venda), ativa = true
        var vendaParcialRequest = new RegistrarVendaRequest
        {
            Quantidade = 12m,
            PrecoUnitario = 35m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json3 = JsonSerializer.Serialize(vendaParcialRequest);
        var content3 = new StringContent(json3, Encoding.UTF8, "application/json");
        var response3 = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/{ativoId}/vendas",
            content3);

        // Assert 3
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
        var responseBody3 = await response3.Content.ReadAsStringAsync();
        var ativo3 = JsonSerializer.Deserialize<AtivoResponse>(responseBody3, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativo3);
        Assert.Equal(18m, ativo3.Quantidade);
        Assert.Equal(26.67m, ativo3.PrecoMedio, 2);
        Assert.True(ativo3.Ativa);

        // Act 4 - Venda total: vender 18 unidades a 35 reais
        // Esperado: quantidade = 0, ativa = false
        var vendaTotalRequest = new RegistrarVendaRequest
        {
            Quantidade = 18m,
            PrecoUnitario = 35m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json4 = JsonSerializer.Serialize(vendaTotalRequest);
        var content4 = new StringContent(json4, Encoding.UTF8, "application/json");
        var response4 = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/{ativoId}/vendas",
            content4);

        // Assert 4
        Assert.Equal(HttpStatusCode.OK, response4.StatusCode);
        var responseBody4 = await response4.Content.ReadAsStringAsync();
        var ativo4 = JsonSerializer.Deserialize<AtivoResponse>(responseBody4, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativo4);
        Assert.Equal(0m, ativo4.Quantidade);
        Assert.False(ativo4.Ativa);

        // Act 5 - Listar ativos: deve estar vazio porque o ativo foi desativado
        var responseListar = await _fixture.Client.GetAsync($"/api/contas/{contaInvestimento.Id}/ativos");

        // Assert 5
        Assert.Equal(HttpStatusCode.OK, responseListar.StatusCode);
        var responseBodyListar = await responseListar.Content.ReadAsStringAsync();
        var ativosListados = JsonSerializer.Deserialize<List<AtivoResponse>>(responseBodyListar, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativosListados);
        Assert.Empty(ativosListados);
    }

    #endregion

    #region Shape do AtivoResponse

    [Fact]
    public async Task AtivoResponse_ContemTodosCamposCorretos()
    {
        await _fixture.ClearAsync();
        // Arrange
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Carteira",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaInvestimento);

        var compraRequest = new RegistrarCompraRequest
        {
            Ticker = "VALE3",
            Quantidade = 5m,
            PrecoUnitario = 50m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            Nome = "Vale"
        };

        var json = JsonSerializer.Serialize(compraRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaInvestimento.Id}/ativos/compras",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        // Verifica campos no JSON bruto
        Assert.Contains("\"id\":", responseBody);
        Assert.Contains("\"ticker\":", responseBody);
        Assert.Contains("\"nome\":", responseBody);
        Assert.Contains("\"quantidade\":", responseBody);
        Assert.Contains("\"precoMedio\":", responseBody);
        Assert.Contains("\"precoAtual\":", responseBody);
        Assert.Contains("\"ativa\":", responseBody);

        var ativoResponse = JsonSerializer.Deserialize<AtivoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativoResponse);
        Assert.NotEqual(Guid.Empty, ativoResponse.Id);
        Assert.Equal("VALE3", ativoResponse.Ticker);
        Assert.Equal("Vale", ativoResponse.Nome);
        Assert.Equal(5m, ativoResponse.Quantidade);
        Assert.Equal(50m, ativoResponse.PrecoMedio);
        Assert.Equal(50m, ativoResponse.PrecoAtual);
        Assert.True(ativoResponse.Ativa);
    }

    #endregion
}
