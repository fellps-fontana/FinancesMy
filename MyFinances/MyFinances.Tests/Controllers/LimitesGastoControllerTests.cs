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
using MyFinances.DTOs.LimiteGasto;
using MyFinances.DTOs;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Controllers;

public class LimitesGastoControllerWebApplicationFactory : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("LimitesGastoControllerTestDb"));
        });
    }
}

public class LimitesGastoControllerTestsFixture : IAsyncLifetime
{
    private readonly LimitesGastoControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public LimitesGastoControllerTestsFixture()
    {
        _factory = new LimitesGastoControllerWebApplicationFactory();
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
            Username = $"limites_gasto_test_{Guid.NewGuid():N}",
            Email = $"limites_gasto_test_{Guid.NewGuid():N}@example.com",
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

    public async Task AddCategoriaAsync(Categoria categoria)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Categorias.Add(categoria);
        await dbContext.SaveChangesAsync();
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

    public async Task<Categoria?> GetCategoriaByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Categorias.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<LimiteGasto?> GetLimiteGastoByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.LimitesGasto.FirstOrDefaultAsync(l => l.CategoriaId == id);
    }

    public async Task<List<LimiteGasto>> GetAllLimitesGastoAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.LimitesGasto.ToListAsync();
    }

    public async Task<List<Lancamento>> GetLancamentosAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Lancamentos.ToListAsync();
    }

    public async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.RemoveRange(await dbContext.Lancamentos.ToListAsync());
        dbContext.LimitesGasto.RemoveRange(await dbContext.LimitesGasto.ToListAsync());
        dbContext.Categorias.RemoveRange(await dbContext.Categorias.ToListAsync());
        dbContext.Contas.RemoveRange(await dbContext.Contas.ToListAsync());
        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition("LimitesGasto Controller Collection")]
public class LimitesGastoControllerCollection : ICollectionFixture<LimitesGastoControllerTestsFixture>
{
}

[Collection("LimitesGasto Controller Collection")]
public class LimitesGastoControllerTests
{
    private readonly LimitesGastoControllerTestsFixture _fixture;

    public LimitesGastoControllerTests(LimitesGastoControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region POST /api/limites-gasto

    [Fact]
    public async Task DefinirLimite_CategoriaDespesaNova_Retorna201CreatedComCategoriaNomePreenchido()
    {
        await _fixture.ClearAsync();

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Alimentacao",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        await _fixture.AddCategoriaAsync(categoria);

        var request = new DefinirLimiteGastoRequest
        {
            CategoriaId = categoria.Id,
            ValorLimite = 500m
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/limites-gasto", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resultado = JsonSerializer.Deserialize<LimiteGastoResponse>(responseBody, LimitesGastoControllerTestsFixture.JsonOptions);

        Assert.NotNull(resultado);
        Assert.Equal(categoria.Id, resultado.CategoriaId);
        Assert.Equal("Alimentacao", resultado.CategoriaNome);
        Assert.NotEmpty(resultado.CategoriaNome);
        Assert.Equal(500m, resultado.ValorLimite);
    }

    [Fact]
    public async Task DefinirLimite_MesmaCategoriaValorDiferente_Retorna200OkEListaContemApenasUmRegistro()
    {
        await _fixture.ClearAsync();

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Transporte",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        await _fixture.AddCategoriaAsync(categoria);

        var request1 = new DefinirLimiteGastoRequest
        {
            CategoriaId = categoria.Id,
            ValorLimite = 200m
        };

        var json1 = JsonSerializer.Serialize(request1);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        var response1 = await _fixture.Client.PostAsync("/api/limites-gasto", content1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var request2 = new DefinirLimiteGastoRequest
        {
            CategoriaId = categoria.Id,
            ValorLimite = 300m
        };

        var json2 = JsonSerializer.Serialize(request2);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

        var response2 = await _fixture.Client.PostAsync("/api/limites-gasto", content2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var getResponse = await _fixture.Client.GetAsync("/api/limites-gasto");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var bodyGet = await getResponse.Content.ReadAsStringAsync();
        var listaLimites = JsonSerializer.Deserialize<List<LimiteGastoResponse>>(bodyGet, LimitesGastoControllerTestsFixture.JsonOptions);

        Assert.NotNull(listaLimites);
        var limitesDestaCat = listaLimites.Where(l => l.CategoriaId == categoria.Id).ToList();
        Assert.Single(limitesDestaCat);
        Assert.Equal(300m, limitesDestaCat[0].ValorLimite);
    }

    [Fact]
    public async Task DefinirLimite_CategoriaReceita_Retorna422UnprocessableEntity()
    {
        await _fixture.ClearAsync();

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Salario",
            Tipo = TipoCategoria.Receita,
            Arquivada = false
        };

        await _fixture.AddCategoriaAsync(categoria);

        var request = new DefinirLimiteGastoRequest
        {
            CategoriaId = categoria.Id,
            ValorLimite = 5000m
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/limites-gasto", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    #endregion

    #region DELETE /api/limites-gasto/{categoriaId}

    [Fact]
    public async Task RemoverLimite_CategoriaSemLimite_Retorna404NotFound()
    {
        await _fixture.ClearAsync();

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Saude",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        await _fixture.AddCategoriaAsync(categoria);

        var response = await _fixture.Client.DeleteAsync($"/api/limites-gasto/{categoria.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /api/limites-gasto/gasto-vs-limite

    [Fact]
    public async Task ObterGastoVsLimiteTodasCategorias_ComLimiteEGastoEstourado_RetornaEstouradoTrue()
    {
        await _fixture.ClearAsync();

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Lazer",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        await _fixture.AddCategoriaAsync(categoria);

        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Corrente Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta);

        var request = new DefinirLimiteGastoRequest
        {
            CategoriaId = categoria.Id,
            ValorLimite = 100m
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var responseDefinir = await _fixture.Client.PostAsync("/api/limites-gasto", content);
        Assert.Equal(HttpStatusCode.Created, responseDefinir.StatusCode);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            CategoriaId = categoria.Id,
            Valor = 150m,
            Tipo = TipoLancamento.Debit,
            Data = hoje,
            Status = StatusLancamento.Pago,
            Manual = true,
            Oculto = false
        };

        await _fixture.AddLancamentoAsync(lancamento);

        var ano = hoje.Year;
        var mes = hoje.Month;

        var getResponse = await _fixture.Client.GetAsync($"/api/limites-gasto/gasto-vs-limite?ano={ano}&mes={mes}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var bodyGet = await getResponse.Content.ReadAsStringAsync();
        var listaGastoVsLimite = JsonSerializer.Deserialize<List<GastoVsLimiteResponse>>(bodyGet, LimitesGastoControllerTestsFixture.JsonOptions);

        Assert.NotNull(listaGastoVsLimite);
        var resultado = listaGastoVsLimite.FirstOrDefault(g => g.CategoriaId == categoria.Id);
        Assert.NotNull(resultado);
        Assert.True(resultado.Estourado);
        Assert.Equal(150m, resultado.GastoRealizado);
        Assert.Equal(100m, resultado.ValorLimite);
    }

    [Fact]
    public async Task ObterGastoVsLimitePorCategoria_CategoriaSemLimite_Retorna404NotFound()
    {
        await _fixture.ClearAsync();

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Educacao",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        await _fixture.AddCategoriaAsync(categoria);

        var ano = DateTime.UtcNow.Year;
        var mes = DateTime.UtcNow.Month;

        var response = await _fixture.Client.GetAsync($"/api/limites-gasto/gasto-vs-limite/{categoria.Id}?ano={ano}&mes={mes}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
