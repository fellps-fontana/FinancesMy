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
using MyFinances.DTOs;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Controllers;

public class TransferenciasControllerWebApplicationFactory : WebApplicationFactory<Program>
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
                    options.UseInMemoryDatabase("TransferenciasControllerTestDb"));
            });
    }
}

public class TransferenciasControllerTestsFixture : IAsyncLifetime
{
    private readonly TransferenciasControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public TransferenciasControllerTestsFixture()
    {
        _factory = new TransferenciasControllerWebApplicationFactory();
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
            Username = $"transferencias_test_{Guid.NewGuid():N}",
            Email = $"transferencias_test_{Guid.NewGuid():N}@example.com",
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

    public async Task<Conta?> GetContaByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Contas.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Lancamento>> GetLancamentosByTransferenciaIdAsync(Guid transferenciaId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Lancamentos
            .Where(l => l.TransferenciaId == transferenciaId)
            .ToListAsync();
    }

    public async Task<Transferencia?> GetTransferenciaByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Transferencias.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.RemoveRange(await dbContext.Lancamentos.ToListAsync());
        dbContext.Transferencias.RemoveRange(await dbContext.Transferencias.ToListAsync());
        dbContext.Contas.RemoveRange(await dbContext.Contas.ToListAsync());
        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition("Transferencias Controller Collection")]
public class TransferenciasControllerCollection : ICollectionFixture<TransferenciasControllerTestsFixture>
{
}

[Collection("Transferencias Controller Collection")]
public class TransferenciasControllerTests
{
    private readonly TransferenciasControllerTestsFixture _fixture;

    public TransferenciasControllerTests(TransferenciasControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region POST /api/transferencias - Registrar transferencia

    [Fact]
    public async Task RegistrarTransferencia_ComCorpoValido_Retorna201ComLocationHeader()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 500m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaOrigem);
        await _fixture.AddContaAsync(contaDestino);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 250m,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Descricao = "Transferencia para investimentos"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/transferencias", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/api/transferencias/", response.Headers.Location?.ToString());

        var responseBody = await response.Content.ReadAsStringAsync();
        var pagamentoResponse = JsonSerializer.Deserialize<PagamentoResponse>(responseBody, TransferenciasControllerTestsFixture.JsonOptions);

        Assert.NotNull(pagamentoResponse);
        Assert.NotEqual(Guid.Empty, pagamentoResponse.Id);
        Assert.Equal(contaOrigemId, pagamentoResponse.ContaOrigemId);
        Assert.Equal(contaDestinoId, pagamentoResponse.ContaDestinoId);
        Assert.Equal(250m, pagamentoResponse.Valor);
    }

    [Fact]
    public async Task RegistrarTransferencia_ComCorpoValido_CriaDoisLancamentosComMesmoTransferenciaId()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 500m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaOrigem);
        await _fixture.AddContaAsync(contaDestino);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 250m,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Descricao = "Transferencia para investimentos"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/transferencias", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var pagamentoResponse = JsonSerializer.Deserialize<PagamentoResponse>(responseBody, TransferenciasControllerTestsFixture.JsonOptions);

        Assert.NotNull(pagamentoResponse);
        var transferenciaId = pagamentoResponse.Id;

        // Verificar que existem 2 lancamentos no banco com o mesmo TransferenciaId
        var lancamentos = await _fixture.GetLancamentosByTransferenciaIdAsync(transferenciaId);
        Assert.Equal(2, lancamentos.Count);

        // Um deve ser saida na origem, outro entrada no destino
        var lancamentoOrigem = lancamentos.FirstOrDefault(l => l.ContaId == contaOrigemId);
        var lancamentoDestino = lancamentos.FirstOrDefault(l => l.ContaId == contaDestinoId);

        Assert.NotNull(lancamentoOrigem);
        Assert.NotNull(lancamentoDestino);

        // Ambos com mesmo valor, mas tipos diferentes (DEBIT na origem, CREDIT no destino)
        Assert.Equal(250m, lancamentoOrigem.Valor);
        Assert.Equal(250m, lancamentoDestino.Valor);
        Assert.Equal(TipoLancamento.Debit, lancamentoOrigem.Tipo);
        Assert.Equal(TipoLancamento.Credit, lancamentoDestino.Tipo);

        // Ambos relacionados a mesma transferencia
        Assert.Equal(transferenciaId, lancamentoOrigem.TransferenciaId);
        Assert.Equal(transferenciaId, lancamentoDestino.TransferenciaId);
    }

    [Fact]
    public async Task RegistrarTransferencia_ComContaOrigemInexistente_Retorna400()
    {
        // Arrange
        var contaOrigemIdInexistente = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 500m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaDestino);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemIdInexistente,
            ContaDestinoId = contaDestinoId,
            Valor = 250m,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Descricao = "Transferencia teste"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/transferencias", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, TransferenciasControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
    }

    [Fact]
    public async Task RegistrarTransferencia_ComContaDestinoInexistente_Retorna400()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoIdInexistente = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaOrigem);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoIdInexistente,
            Valor = 250m,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Descricao = "Transferencia teste"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/transferencias", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, TransferenciasControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
    }

    [Fact]
    public async Task RegistrarTransferencia_ComValorZero_Retorna400()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 500m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaOrigem);
        await _fixture.AddContaAsync(contaDestino);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 0m,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Descricao = "Transferencia com valor zero"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/transferencias", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, TransferenciasControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
    }

    [Fact]
    public async Task RegistrarTransferencia_ComValorNegativo_Retorna400()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            SaldoManual = 500m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaOrigem);
        await _fixture.AddContaAsync(contaDestino);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = -250m,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Descricao = "Transferencia com valor negativo"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/transferencias", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, TransferenciasControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
    }

    [Fact]
    public async Task RegistrarTransferencia_ComContasIguais_Retorna400()
    {
        // Arrange
        var contaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaId,
            ContaDestinoId = contaId,
            Valor = 250m,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Descricao = "Transferencia para a mesma conta"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/transferencias", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, TransferenciasControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
    }

    #endregion
}
