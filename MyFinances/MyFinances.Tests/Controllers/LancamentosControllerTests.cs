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

public class LancamentosControllerWebApplicationFactory : WebApplicationFactory<Program>
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
                    options.UseInMemoryDatabase("LancamentosControllerTestDb"));
            });
    }
}

public class LancamentosControllerTestsFixture : IAsyncLifetime
{
    private readonly LancamentosControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public LancamentosControllerTestsFixture()
    {
        _factory = new LancamentosControllerWebApplicationFactory();
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
            Username = $"lancamentos_test_{Guid.NewGuid():N}",
            Email = $"lancamentos_test_{Guid.NewGuid():N}@example.com",
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

    public async Task AddLancamentoAsync(Lancamento lancamento)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.Add(lancamento);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Conta?> GetContaByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Contas.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Lancamento?> GetLancamentoByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Lancamentos.FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<Lancamento>> GetLancamentosByContaIdAsync(Guid contaId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Lancamentos.Where(l => l.ContaId == contaId).ToListAsync();
    }

    public async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.RemoveRange(await dbContext.Lancamentos.ToListAsync());
        dbContext.Contas.RemoveRange(await dbContext.Contas.ToListAsync());
        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition("Lancamentos Controller Collection")]
public class LancamentosControllerCollection : ICollectionFixture<LancamentosControllerTestsFixture>
{
}

[Collection("Lancamentos Controller Collection")]
public class LancamentosControllerTests
{
    private readonly LancamentosControllerTestsFixture _fixture;

    public LancamentosControllerTests(LancamentosControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region POST /api/contas/{contaId}/lancamentos - Criar lancamento

    [Fact]
    public async Task CriarLancamento_ComCorpoValido_Retorna201ComLocationHeader()
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

        var request = new CriarLancamentoRequest
        {
            Descricao = "Compra de supermercado",
            Valor = 150.50m,
            CategoriaId = null,
            Tipo = "DEBIT",
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = "PENDENTE"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync($"/api/contas/{contaId}/lancamentos", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith($"/api/contas/{contaId}/lancamentos/", response.Headers.Location?.ToString());

        var responseBody = await response.Content.ReadAsStringAsync();
        var lancamentoResponse = JsonSerializer.Deserialize<LancamentoResponseDto>(responseBody, LancamentosControllerTestsFixture.JsonOptions);

        Assert.NotNull(lancamentoResponse);
        Assert.NotEqual(Guid.Empty, lancamentoResponse.Id);
        Assert.Equal(contaId, lancamentoResponse.ContaId);
        Assert.Equal("Compra de supermercado", lancamentoResponse.Descricao);
        Assert.Equal(150.50m, lancamentoResponse.Valor);
        Assert.Equal("DEBIT", lancamentoResponse.Tipo);
        Assert.Equal("PENDENTE", lancamentoResponse.Status);
    }

    [Fact]
    public async Task CriarLancamento_ComContaInexistente_Retorna400()
    {
        // Arrange
        var contaIdInexistente = Guid.NewGuid();

        var request = new CriarLancamentoRequest
        {
            Descricao = "Compra teste",
            Valor = 100m,
            CategoriaId = null,
            Tipo = "DEBIT",
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = "PENDENTE"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync($"/api/contas/{contaIdInexistente}/lancamentos", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, LancamentosControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
        Assert.NotEmpty(errorResponse["erro"]);
    }

    [Fact]
    public async Task CriarLancamento_ComContaInativa_Retorna400()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Desativada",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = false
        };

        await _fixture.AddContaAsync(conta);

        var request = new CriarLancamentoRequest
        {
            Descricao = "Compra teste",
            Valor = 100m,
            CategoriaId = null,
            Tipo = "DEBIT",
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = "PENDENTE"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync($"/api/contas/{contaId}/lancamentos", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, LancamentosControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
    }

    #endregion

    #region PUT /api/contas/{contaId}/lancamentos/{lancamentoId} - Editar lancamento

    [Fact]
    public async Task EditarLancamento_ComCorpoValido_Retorna200()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Descricao original",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pendente,
            Manual = true
        };

        await _fixture.AddContaAsync(conta);
        await _fixture.AddLancamentoAsync(lancamento);

        var request = new EditarLancamentoRequest
        {
            Descricao = "Descricao atualizada",
            Valor = 200m,
            CategoriaId = null,
            Tipo = "CREDIT",
            Data = DateOnly.FromDateTime(DateTime.Now.AddDays(1))
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PutAsync($"/api/contas/{contaId}/lancamentos/{lancamentoId}", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var lancamentoResponse = JsonSerializer.Deserialize<LancamentoResponseDto>(responseBody, LancamentosControllerTestsFixture.JsonOptions);

        Assert.NotNull(lancamentoResponse);
        Assert.Equal(lancamentoId, lancamentoResponse.Id);
        Assert.Equal("Descricao atualizada", lancamentoResponse.Descricao);
        Assert.Equal(200m, lancamentoResponse.Valor);
        Assert.Equal("CREDIT", lancamentoResponse.Tipo);
    }

    [Fact]
    public async Task EditarLancamento_ComLancamentoInexistente_Retorna400()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoIdInexistente = Guid.NewGuid();

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

        var request = new EditarLancamentoRequest
        {
            Descricao = "Descricao",
            Valor = 200m,
            CategoriaId = null,
            Tipo = "DEBIT",
            Data = DateOnly.FromDateTime(DateTime.Now)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PutAsync($"/api/contas/{contaId}/lancamentos/{lancamentoIdInexistente}", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, LancamentosControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
    }

    #endregion

    #region POST /api/contas/{contaId}/lancamentos/{lancamentoId}/pagamentos - Marcar como pago

    [Fact]
    public async Task MarcarComoPago_ComLancamentoPendente_TransicionaParaPago()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Conta a pagar",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pendente,
            Manual = true
        };

        await _fixture.AddContaAsync(conta);
        await _fixture.AddLancamentoAsync(lancamento);

        // Act
        var response = await _fixture.Client.PostAsync($"/api/contas/{contaId}/lancamentos/{lancamentoId}/pagamentos", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var lancamentoAposMarcar = await _fixture.GetLancamentoByIdAsync(lancamentoId);
        Assert.NotNull(lancamentoAposMarcar);
        Assert.Equal(StatusLancamento.Pago, lancamentoAposMarcar.Status);
    }

    [Fact]
    public async Task MarcarComoPago_ComLancamentoInexistente_Retorna400()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoIdInexistente = Guid.NewGuid();

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

        // Act
        var response = await _fixture.Client.PostAsync($"/api/contas/{contaId}/lancamentos/{lancamentoIdInexistente}/pagamentos", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, LancamentosControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
    }

    #endregion

    #region DELETE /api/contas/{contaId}/lancamentos/{lancamentoId} - Remover lancamento

    [Fact]
    public async Task RemoverLancamento_ComIdValido_Retorna200ERemoveLancamentoDoDb()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Lancamento a deletar",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pendente,
            Manual = true
        };

        await _fixture.AddContaAsync(conta);
        await _fixture.AddLancamentoAsync(lancamento);

        // Verificar que lancamento existe antes de deletar
        var lancamentoAntes = await _fixture.GetLancamentoByIdAsync(lancamentoId);
        Assert.NotNull(lancamentoAntes);

        // Act
        var response = await _fixture.Client.DeleteAsync($"/api/contas/{contaId}/lancamentos/{lancamentoId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verificar que lancamento foi deletado do banco
        var lancamentoDepois = await _fixture.GetLancamentoByIdAsync(lancamentoId);
        Assert.Null(lancamentoDepois);
    }

    [Fact]
    public async Task RemoverLancamento_ComLancamentoInexistente_Retorna400()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoIdInexistente = Guid.NewGuid();

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

        // Act
        var response = await _fixture.Client.DeleteAsync($"/api/contas/{contaId}/lancamentos/{lancamentoIdInexistente}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(responseBody, LancamentosControllerTestsFixture.JsonOptions);

        Assert.NotNull(errorResponse);
        Assert.True(errorResponse.ContainsKey("erro"));
    }

    #endregion

    #region GET /api/contas/{contaId}/lancamentos/fluxo-caixa - Listar fluxo de caixa

    [Fact]
    public async Task ListarFluxoCaixa_ComLancamentoManualComum_IncluiNaResposta()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoManualId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        var lancamentoManual = new Lancamento
        {
            Id = lancamentoManualId,
            ContaId = contaId,
            Descricao = "Lancamento manual comum",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pendente,
            Manual = true,
            FaturaId = null
        };

        await _fixture.AddContaAsync(conta);
        await _fixture.AddLancamentoAsync(lancamentoManual);

        // Act
        var response = await _fixture.Client.GetAsync($"/api/contas/{contaId}/lancamentos/fluxo-caixa");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var lancamentos = JsonSerializer.Deserialize<List<LancamentoResponseDto>>(responseBody, LancamentosControllerTestsFixture.JsonOptions);

        Assert.NotNull(lancamentos);
        Assert.Single(lancamentos);
        Assert.Equal(lancamentoManualId, lancamentos[0].Id);
        Assert.Equal("Lancamento manual comum", lancamentos[0].Descricao);
    }

    [Fact]
    public async Task ListarFluxoCaixa_ComLancamentoDeFatura_ExcluiDaResposta()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var faturaId = Guid.NewGuid();
        var lancamentoFaturaId = Guid.NewGuid();
        var lancamentoManualId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        var lancamentoComFatura = new Lancamento
        {
            Id = lancamentoFaturaId,
            ContaId = contaId,
            Descricao = "Compra no cartao",
            Valor = 500m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pendente,
            Manual = true,
            FaturaId = faturaId
        };

        var lancamentoSemFatura = new Lancamento
        {
            Id = lancamentoManualId,
            ContaId = contaId,
            Descricao = "Lancamento manual",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pendente,
            Manual = true,
            FaturaId = null
        };

        await _fixture.AddContaAsync(conta);
        await _fixture.AddLancamentoAsync(lancamentoComFatura);
        await _fixture.AddLancamentoAsync(lancamentoSemFatura);

        // Act
        var response = await _fixture.Client.GetAsync($"/api/contas/{contaId}/lancamentos/fluxo-caixa");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var lancamentos = JsonSerializer.Deserialize<List<LancamentoResponseDto>>(responseBody, LancamentosControllerTestsFixture.JsonOptions);

        Assert.NotNull(lancamentos);
        Assert.Single(lancamentos);
        Assert.Equal(lancamentoManualId, lancamentos[0].Id);
        Assert.Equal("Lancamento manual", lancamentos[0].Descricao);

        // Confirmar que lancamento com fatura nao aparece
        Assert.DoesNotContain(lancamentos, l => l.Id == lancamentoFaturaId);
    }

    [Fact]
    public async Task ListarFluxoCaixa_ContaVazia_Retorna200ComListaVazia()
    {
        // Arrange
        var contaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Vazia",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta);

        // Act
        var response = await _fixture.Client.GetAsync($"/api/contas/{contaId}/lancamentos/fluxo-caixa");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var lancamentos = JsonSerializer.Deserialize<List<LancamentoResponseDto>>(responseBody, LancamentosControllerTestsFixture.JsonOptions);

        Assert.NotNull(lancamentos);
        Assert.Empty(lancamentos);
    }

    #endregion
}
