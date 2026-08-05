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
using MyFinances.DTOs.Ativo;
using MyFinances.DTOs.Rendimento;
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

    public async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Ativos.RemoveRange(await dbContext.Ativos.ToListAsync());
        await dbContext.SaveChangesAsync();
    }

    public async Task ClearRendimentosAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Rendimentos.RemoveRange(await dbContext.Rendimentos.ToListAsync());
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

    #region POST /api/ativos - Criar ativo

    [Fact]
    public async Task CriarAtivo_ComCorpoValido_Retorna201ComLocationHeader()
    {
        // Arrange
        await _fixture.ClearAsync();

        var request = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 1m,
            PrecoUnitario = 1000m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/ativos", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/api/ativos/", response.Headers.Location?.ToString());

        var responseBody = await response.Content.ReadAsStringAsync();
        var ativoResponse = JsonSerializer.Deserialize<AtivoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativoResponse);
        Assert.NotEqual(Guid.Empty, ativoResponse.Id);
        Assert.Equal("Tesouro Direto IPCA", ativoResponse.Nome);
        Assert.Equal(TipoAtivo.RendaFixa, ativoResponse.Tipo);
        Assert.Equal("B3", ativoResponse.Instituicao);
        Assert.Equal(1000m, ativoResponse.ValorInvestido);
        Assert.Equal(1000m, ativoResponse.ValorAtual); // NO FIRST DAY, ALWAYS EQUAL
        Assert.Equal(0m, ativoResponse.EvolucaoPercentual); // NO FIRST DAY, ALWAYS ZERO
        Assert.True(ativoResponse.Ativa);

        // Verify raw JSON contains enum as string, not as integer
        Assert.Contains("\"tipo\":\"RendaFixa\"", responseBody);
    }

    [Fact]
    public async Task CriarAtivo_ComTipoRendaVariavel_Retorna201()
    {
        // Arrange
        await _fixture.ClearAsync();

        var request = new CriarAtivoRequest
        {
            Nome = "ETF IBOV",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "XP",
            Quantidade = 1m,
            PrecoUnitario = 5000m,
            DataCompra = new DateOnly(2024, 2, 20)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/ativos", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var ativoResponse = JsonSerializer.Deserialize<AtivoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativoResponse);
        Assert.Equal(TipoAtivo.RendaVariavel, ativoResponse.Tipo);
        Assert.Equal(5000m, ativoResponse.ValorAtual);
    }

    [Fact]
    public async Task CriarAtivo_ComValorInvestidoNegativo_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var request = new CriarAtivoRequest
        {
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 1m,
            PrecoUnitario = -1000m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/ativos", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarAtivo_ComValorInvestidoZero_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var request = new CriarAtivoRequest
        {
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 1m,
            PrecoUnitario = 0m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/ativos", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarAtivo_ComNomeVazio_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var request = new CriarAtivoRequest
        {
            Nome = string.Empty,
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 1m,
            PrecoUnitario = 1000m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/ativos", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarAtivo_ComNomeApenasBranco_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var request = new CriarAtivoRequest
        {
            Nome = "   ",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 1m,
            PrecoUnitario = 1000m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/ativos", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarAtivo_ComInstituicaoVazia_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var request = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = string.Empty,
            Quantidade = 1m,
            PrecoUnitario = 1000m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/ativos", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarAtivo_ComInstituicaoApenasBranco_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var request = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "  \t\n  ",
            Quantidade = 1m,
            PrecoUnitario = 1000m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/ativos", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GET /api/ativos - Listar ativos ativos

    [Fact]
    public async Task ListarAtivos_ComMultiplosAtivos_RetornaSoAtivos()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoAtivo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro 1",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        var ativoInativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro Inativo",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 500m,
            ValorAtual = 500m,
            DataCompra = new DateOnly(2024, 1, 10),
            Ativa = false,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativoAtivo);
        await _fixture.AddAtivoAsync(ativoInativo);

        // Act
        var response = await _fixture.Client.GetAsync("/api/ativos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var ativos = JsonSerializer.Deserialize<List<AtivoResponse>>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativos);
        Assert.Single(ativos);
        Assert.Equal("Tesouro 1", ativos.First().Nome);
        Assert.True(ativos.First().Ativa);
    }

    [Fact]
    public async Task ListarAtivos_SemAtivosAtivos_RetornaListaVazia()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoInativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Ativo Desativado",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = false,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativoInativo);

        // Act
        var response = await _fixture.Client.GetAsync("/api/ativos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var ativos = JsonSerializer.Deserialize<List<AtivoResponse>>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativos);
        Assert.Empty(ativos);
    }

    #endregion

    #region PATCH /api/ativos/{id}/valor-atual - Atualizar valor_atual

    [Fact]
    public async Task AtualizarValorAtual_ComNovoValorValido_Retorna200()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativo);

        var request = new AtualizarValorAtualRequest
        {
            NovoValorAtual = 1200m
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PatchAsync($"/api/ativos/{ativo.Id}/valor-atual", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ativoAtualizado = await _fixture.GetAtivoByIdAsync(ativo.Id);
        Assert.NotNull(ativoAtualizado);
        Assert.Equal(1200m, ativoAtualizado.ValorAtual);
    }

    [Fact]
    public async Task AtualizarValorAtual_ComNovoValorNegativo_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "ETF",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "XP",
            ValorInvestido = 5000m,
            ValorAtual = 5000m,
            DataCompra = new DateOnly(2024, 2, 20),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativo);

        var request = new AtualizarValorAtualRequest
        {
            NovoValorAtual = -100m
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PatchAsync($"/api/ativos/{ativo.Id}/valor-atual", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AtualizarValorAtual_ComAtivoInexistente_Retorna404()
    {
        // Arrange
        var ativoIdInexistente = Guid.NewGuid();

        var request = new AtualizarValorAtualRequest
        {
            NovoValorAtual = 1000m
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PatchAsync($"/api/ativos/{ativoIdInexistente}/valor-atual", content);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region PATCH /api/ativos/{id}/desativar - Desativar ativo

    [Fact]
    public async Task DesativarAtivo_ComAtivoExistente_Retorna200EDesativa()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativo);

        // Act
        var response = await _fixture.Client.PatchAsync($"/api/ativos/{ativo.Id}/desativar", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ativoDesativado = await _fixture.GetAtivoByIdAsync(ativo.Id);
        Assert.NotNull(ativoDesativado);
        Assert.False(ativoDesativado.Ativa);
    }

    [Fact]
    public async Task DesativarAtivo_ComAtivoInexistente_Retorna404()
    {
        // Arrange
        var ativoIdInexistente = Guid.NewGuid();

        // Act
        var response = await _fixture.Client.PatchAsync($"/api/ativos/{ativoIdInexistente}/desativar", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /api/ativos/resumo - Resumo por tipo

    [Fact]
    public async Task ObterResumo_ComMultiplosAtivos_CalculaTotaisEPercentuais()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRendaFixa1 = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1100m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        var ativoRendaVariavel = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "ETF IBOV",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "XP",
            ValorInvestido = 5000m,
            ValorAtual = 5400m,
            DataCompra = new DateOnly(2024, 2, 20),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        var ativoRendaFixa2 = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "LCI",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "Banco",
            ValorInvestido = 2000m,
            ValorAtual = 2000m,
            DataCompra = new DateOnly(2024, 3, 10),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativoRendaFixa1);
        await _fixture.AddAtivoAsync(ativoRendaVariavel);
        await _fixture.AddAtivoAsync(ativoRendaFixa2);

        // Act
        var response = await _fixture.Client.GetAsync("/api/ativos/resumo");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resumo = JsonSerializer.Deserialize<AtivosResumoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(resumo);
        Assert.Equal(8000m, resumo.TotalInvestido); // 1000 + 5000 + 2000
        Assert.Equal(8500m, resumo.TotalAtual); // 1100 + 5400 + 2000

        Assert.Equal(2, resumo.PorTipo.Count());

        var rendaFixa = resumo.PorTipo.FirstOrDefault(t => t.Tipo == "RENDA_FIXA");
        Assert.NotNull(rendaFixa);
        Assert.Equal(3100m, rendaFixa.ValorAtual); // 1100 + 2000
        Assert.True(rendaFixa.PercentualDaCarteira > 36m && rendaFixa.PercentualDaCarteira < 37m);

        var rendaVariavel = resumo.PorTipo.FirstOrDefault(t => t.Tipo == "RENDA_VARIAVEL");
        Assert.NotNull(rendaVariavel);
        Assert.Equal(5400m, rendaVariavel.ValorAtual);
        Assert.True(rendaVariavel.PercentualDaCarteira > 63m && rendaVariavel.PercentualDaCarteira < 64m);
    }

    [Fact]
    public async Task ObterResumo_SemAtivos_RetornaTotaisZero()
    {
        // Arrange
        await _fixture.ClearAsync();

        // Act
        var response = await _fixture.Client.GetAsync("/api/ativos/resumo");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resumo = JsonSerializer.Deserialize<AtivosResumoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(resumo);
        Assert.Equal(0m, resumo.TotalInvestido);
        Assert.Equal(0m, resumo.TotalAtual);
        Assert.Empty(resumo.PorTipo);
    }

    [Fact]
    public async Task ObterResumo_ApenasUmTipo_RetornaComPercentual100()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativo1 = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro 1",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1100m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        var ativo2 = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro 2",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 2000m,
            ValorAtual = 2200m,
            DataCompra = new DateOnly(2024, 2, 10),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativo1);
        await _fixture.AddAtivoAsync(ativo2);

        // Act
        var response = await _fixture.Client.GetAsync("/api/ativos/resumo");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resumo = JsonSerializer.Deserialize<AtivosResumoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(resumo);
        Assert.Single(resumo.PorTipo);

        var rendaFixa = resumo.PorTipo.First();
        Assert.Equal("RENDA_FIXA", rendaFixa.Tipo);
        Assert.Equal(3300m, rendaFixa.ValorAtual); // 1100 + 2200
        Assert.Equal(100m, rendaFixa.PercentualDaCarteira);
    }

    #endregion

    #region POST /api/ativos/{id}/aportes - Registrar Aporte

    [Fact]
    public async Task RegistrarAporte_ComDadosValidos_Retorna201ComAporteResponse()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 2m,
            PrecoUnitario = 500m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Act - registra novo aporte
        var aporteRequest = new RegistrarAporteRequest
        {
            Quantidade = 3m,
            PrecoUnitario = 600m,
            Data = new DateOnly(2024, 2, 20)
        };

        var aporteJson = JsonSerializer.Serialize(aporteRequest, AtivosControllerTestsFixture.JsonOptions);
        var aporteContent = new StringContent(aporteJson, Encoding.UTF8, "application/json");
        var aporteResponse = await _fixture.Client.PostAsync($"/api/ativos/{ativo!.Id}/aportes", aporteContent);

        // Assert
        Assert.Equal(HttpStatusCode.Created, aporteResponse.StatusCode);
        Assert.NotNull(aporteResponse.Headers.Location);

        var aporteBody = await aporteResponse.Content.ReadAsStringAsync();
        var aporteDto = JsonSerializer.Deserialize<AtivoAporteResponse>(aporteBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(aporteDto);
        Assert.NotEqual(Guid.Empty, aporteDto.Id);
        Assert.Equal(ativo.Id, aporteDto.AtivoId);
        Assert.Equal(new DateOnly(2024, 2, 20), aporteDto.Data);
        Assert.Equal(3m, aporteDto.Quantidade);
        Assert.Equal(600m, aporteDto.PrecoUnitario);
        Assert.Equal(1800m, aporteDto.ValorTotal); // 3 * 600
    }

    [Fact]
    public async Task RegistrarAporte_AtualizaQuantidadeEValorInvestidoDoAtivo()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 2m,
            PrecoUnitario = 500m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Valor inicial esperado: 2 * 500 = 1000
        Assert.Equal(1000m, ativo!.ValorInvestido);
        Assert.Equal(2m, ativo.Quantidade);

        // Act - registra novo aporte
        var aporteRequest = new RegistrarAporteRequest
        {
            Quantidade = 3m,
            PrecoUnitario = 600m,
            Data = new DateOnly(2024, 2, 20)
        };

        var aporteJson = JsonSerializer.Serialize(aporteRequest, AtivosControllerTestsFixture.JsonOptions);
        var aporteContent = new StringContent(aporteJson, Encoding.UTF8, "application/json");
        await _fixture.Client.PostAsync($"/api/ativos/{ativo.Id}/aportes", aporteContent);

        // Busca ativo novamente
        var listarResponse = await _fixture.Client.GetAsync("/api/ativos");
        var listarBody = await listarResponse.Content.ReadAsStringAsync();
        var ativos = JsonSerializer.Deserialize<List<AtivoResponse>>(listarBody, AtivosControllerTestsFixture.JsonOptions);

        // Assert
        var ativoAtualizado = ativos!.First(a => a.Id == ativo.Id);
        Assert.Equal(5m, ativoAtualizado.Quantidade); // 2 + 3
        Assert.Equal(2800m, ativoAtualizado.ValorInvestido); // 1000 + (3 * 600)
    }

    [Fact]
    public async Task RegistrarAporte_CalculaPrecoMedioPonderado()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 2m,
            PrecoUnitario = 500m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Preco medio inicial: 1000 / 2 = 500
        Assert.Equal(500m, ativo!.PrecoMedio);

        // Act
        var aporteRequest = new RegistrarAporteRequest
        {
            Quantidade = 3m,
            PrecoUnitario = 600m,
            Data = new DateOnly(2024, 2, 20)
        };

        var aporteJson = JsonSerializer.Serialize(aporteRequest, AtivosControllerTestsFixture.JsonOptions);
        var aporteContent = new StringContent(aporteJson, Encoding.UTF8, "application/json");
        await _fixture.Client.PostAsync($"/api/ativos/{ativo.Id}/aportes", aporteContent);

        // Busca ativo novamente
        var listarResponse = await _fixture.Client.GetAsync("/api/ativos");
        var listarBody = await listarResponse.Content.ReadAsStringAsync();
        var ativos = JsonSerializer.Deserialize<List<AtivoResponse>>(listarBody, AtivosControllerTestsFixture.JsonOptions);

        // Assert
        var ativoAtualizado = ativos!.First(a => a.Id == ativo.Id);
        // Preco medio = (500 * 2 + 600 * 3) / (2 + 3) = (1000 + 1800) / 5 = 2800 / 5 = 560
        Assert.Equal(560m, ativoAtualizado.PrecoMedio);
    }

    [Fact]
    public async Task RegistrarAporte_EmAtivoInexistente_Retorna404()
    {
        // Arrange
        var ativoIdInexistente = Guid.NewGuid();

        var aporteRequest = new RegistrarAporteRequest
        {
            Quantidade = 1m,
            PrecoUnitario = 500m,
            Data = new DateOnly(2024, 2, 20)
        };

        var aporteJson = JsonSerializer.Serialize(aporteRequest, AtivosControllerTestsFixture.JsonOptions);
        var aporteContent = new StringContent(aporteJson, Encoding.UTF8, "application/json");

        // Act
        var aporteResponse = await _fixture.Client.PostAsync($"/api/ativos/{ativoIdInexistente}/aportes", aporteContent);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, aporteResponse.StatusCode);
    }

    [Fact]
    public async Task RegistrarAporte_EmAtivoDesativado_Retorna404()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 2m,
            PrecoUnitario = 500m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Desativa o ativo
        await _fixture.Client.PatchAsync($"/api/ativos/{ativo!.Id}/desativar", null);

        // Act - tenta registrar aporte em ativo desativado
        var aporteRequest = new RegistrarAporteRequest
        {
            Quantidade = 1m,
            PrecoUnitario = 500m,
            Data = new DateOnly(2024, 2, 20)
        };

        var aporteJson = JsonSerializer.Serialize(aporteRequest, AtivosControllerTestsFixture.JsonOptions);
        var aporteContent = new StringContent(aporteJson, Encoding.UTF8, "application/json");
        var aporteResponse = await _fixture.Client.PostAsync($"/api/ativos/{ativo.Id}/aportes", aporteContent);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, aporteResponse.StatusCode);
    }

    [Fact]
    public async Task RegistrarAporte_ComQuantidadeZero_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 2m,
            PrecoUnitario = 500m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Act
        var aporteRequest = new RegistrarAporteRequest
        {
            Quantidade = 0m,
            PrecoUnitario = 500m,
            Data = new DateOnly(2024, 2, 20)
        };

        var aporteJson = JsonSerializer.Serialize(aporteRequest, AtivosControllerTestsFixture.JsonOptions);
        var aporteContent = new StringContent(aporteJson, Encoding.UTF8, "application/json");
        var aporteResponse = await _fixture.Client.PostAsync($"/api/ativos/{ativo!.Id}/aportes", aporteContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, aporteResponse.StatusCode);
    }

    [Fact]
    public async Task RegistrarAporte_ComQuantidadeNegativa_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 2m,
            PrecoUnitario = 500m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Act
        var aporteRequest = new RegistrarAporteRequest
        {
            Quantidade = -1m,
            PrecoUnitario = 500m,
            Data = new DateOnly(2024, 2, 20)
        };

        var aporteJson = JsonSerializer.Serialize(aporteRequest, AtivosControllerTestsFixture.JsonOptions);
        var aporteContent = new StringContent(aporteJson, Encoding.UTF8, "application/json");
        var aporteResponse = await _fixture.Client.PostAsync($"/api/ativos/{ativo!.Id}/aportes", aporteContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, aporteResponse.StatusCode);
    }

    [Fact]
    public async Task RegistrarAporte_ComPrecoZero_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 2m,
            PrecoUnitario = 500m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Act
        var aporteRequest = new RegistrarAporteRequest
        {
            Quantidade = 1m,
            PrecoUnitario = 0m,
            Data = new DateOnly(2024, 2, 20)
        };

        var aporteJson = JsonSerializer.Serialize(aporteRequest, AtivosControllerTestsFixture.JsonOptions);
        var aporteContent = new StringContent(aporteJson, Encoding.UTF8, "application/json");
        var aporteResponse = await _fixture.Client.PostAsync($"/api/ativos/{ativo!.Id}/aportes", aporteContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, aporteResponse.StatusCode);
    }

    [Fact]
    public async Task RegistrarAporte_ComPrecoNegativo_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 2m,
            PrecoUnitario = 500m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Act
        var aporteRequest = new RegistrarAporteRequest
        {
            Quantidade = 1m,
            PrecoUnitario = -500m,
            Data = new DateOnly(2024, 2, 20)
        };

        var aporteJson = JsonSerializer.Serialize(aporteRequest, AtivosControllerTestsFixture.JsonOptions);
        var aporteContent = new StringContent(aporteJson, Encoding.UTF8, "application/json");
        var aporteResponse = await _fixture.Client.PostAsync($"/api/ativos/{ativo!.Id}/aportes", aporteContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, aporteResponse.StatusCode);
    }

    #endregion

    #region GET /api/ativos/{id}/aportes - Listar Aportes

    [Fact]
    public async Task ListarAportes_ComMultiplosAportes_RetornaEmOrdenCronologica()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 1m,
            PrecoUnitario = 500m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Registra 3 aportes em datas diferentes
        var aporte1Request = new RegistrarAporteRequest
        {
            Quantidade = 2m,
            PrecoUnitario = 600m,
            Data = new DateOnly(2024, 2, 10)
        };

        var aporte2Request = new RegistrarAporteRequest
        {
            Quantidade = 1m,
            PrecoUnitario = 700m,
            Data = new DateOnly(2024, 3, 15)
        };

        var aporte3Request = new RegistrarAporteRequest
        {
            Quantidade = 3m,
            PrecoUnitario = 550m,
            Data = new DateOnly(2024, 1, 20)
        };

        var aporte1Json = JsonSerializer.Serialize(aporte1Request, AtivosControllerTestsFixture.JsonOptions);
        var aporte2Json = JsonSerializer.Serialize(aporte2Request, AtivosControllerTestsFixture.JsonOptions);
        var aporte3Json = JsonSerializer.Serialize(aporte3Request, AtivosControllerTestsFixture.JsonOptions);

        await _fixture.Client.PostAsync($"/api/ativos/{ativo!.Id}/aportes", new StringContent(aporte1Json, Encoding.UTF8, "application/json"));
        await _fixture.Client.PostAsync($"/api/ativos/{ativo.Id}/aportes", new StringContent(aporte2Json, Encoding.UTF8, "application/json"));
        await _fixture.Client.PostAsync($"/api/ativos/{ativo.Id}/aportes", new StringContent(aporte3Json, Encoding.UTF8, "application/json"));

        // Act
        var listarResponse = await _fixture.Client.GetAsync($"/api/ativos/{ativo.Id}/aportes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listarResponse.StatusCode);

        var listarBody = await listarResponse.Content.ReadAsStringAsync();
        var aportes = JsonSerializer.Deserialize<List<AtivoAporteResponse>>(listarBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(aportes);
        Assert.Equal(4, aportes.Count); // 1 aporte inicial (CriarAtivo) + 3 registrados

        // Verifica ordem cronologica (data do aporte)
        Assert.True(aportes[0].Data <= aportes[1].Data);
        Assert.True(aportes[1].Data <= aportes[2].Data);
        Assert.True(aportes[2].Data <= aportes[3].Data);
    }

    [Fact]
    public async Task ListarAportes_ComUnicoAporte_RetornaLista()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "Tesouro Direto IPCA",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            Quantidade = 1m,
            PrecoUnitario = 500m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Act
        var listarResponse = await _fixture.Client.GetAsync($"/api/ativos/{ativo!.Id}/aportes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listarResponse.StatusCode);

        var listarBody = await listarResponse.Content.ReadAsStringAsync();
        var aportes = JsonSerializer.Deserialize<List<AtivoAporteResponse>>(listarBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(aportes);
        Assert.Single(aportes); // Apenas o aporte inicial (CriarAtivo)
        Assert.Equal(ativo.Id, aportes[0].AtivoId);
        Assert.Equal(1m, aportes[0].Quantidade);
        Assert.Equal(500m, aportes[0].PrecoUnitario);
    }

    #endregion

    #region Verificar expolicao de Quantidade e PrecoMedio

    [Fact]
    public async Task CriarAtivo_VerificaQuantidadeEPrecoMedioNaResposta()
    {
        // Arrange
        await _fixture.ClearAsync();

        var request = new CriarAtivoRequest
        {
            Nome = "ETF IBOV",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "XP",
            Quantidade = 5m,
            PrecoUnitario = 150m,
            DataCompra = new DateOnly(2024, 2, 20)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync("/api/ativos", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var ativoResponse = JsonSerializer.Deserialize<AtivoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativoResponse);
        // Valida que AtivoResponse contem Quantidade e PrecoMedio
        Assert.Equal(5m, ativoResponse.Quantidade);
        Assert.Equal(150m, ativoResponse.PrecoMedio); // 750 / 5
        Assert.Equal(750m, ativoResponse.ValorInvestido); // 5 * 150

        // Verifica que JSON contem os campos
        Assert.Contains("\"quantidade\":", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"precoMedio\":", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListarAtivos_ExpoeQuantidadeEPrecoMedioAposAportes()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativoRequest = new CriarAtivoRequest
        {
            Nome = "ETF IBOV",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "XP",
            Quantidade = 2m,
            PrecoUnitario = 100m,
            DataCompra = new DateOnly(2024, 1, 15)
        };

        var json = JsonSerializer.Serialize(ativoRequest, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var ativoResponse = await _fixture.Client.PostAsync("/api/ativos", content);
        var ativoBody = await ativoResponse.Content.ReadAsStringAsync();
        var ativo = JsonSerializer.Deserialize<AtivoResponse>(ativoBody, AtivosControllerTestsFixture.JsonOptions);

        // Registra novo aporte
        var aporteRequest = new RegistrarAporteRequest
        {
            Quantidade = 3m,
            PrecoUnitario = 120m,
            Data = new DateOnly(2024, 2, 20)
        };

        var aporteJson = JsonSerializer.Serialize(aporteRequest, AtivosControllerTestsFixture.JsonOptions);
        var aporteContent = new StringContent(aporteJson, Encoding.UTF8, "application/json");
        await _fixture.Client.PostAsync($"/api/ativos/{ativo!.Id}/aportes", aporteContent);

        // Act
        var listarResponse = await _fixture.Client.GetAsync("/api/ativos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listarResponse.StatusCode);

        var listarBody = await listarResponse.Content.ReadAsStringAsync();
        var ativos = JsonSerializer.Deserialize<List<AtivoResponse>>(listarBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(ativos);
        Assert.Single(ativos);

        var ativoAtualizado = ativos[0];
        // Quantidade = 2 + 3 = 5
        Assert.Equal(5m, ativoAtualizado.Quantidade);
        // ValorInvestido = 200 + 360 = 560
        Assert.Equal(560m, ativoAtualizado.ValorInvestido);
        // PrecoMedio = (100 * 2 + 120 * 3) / 5 = (200 + 360) / 5 = 560 / 5 = 112
        Assert.Equal(112m, ativoAtualizado.PrecoMedio);

        // Verifica JSON
        Assert.Contains("\"quantidade\":", listarBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"precoMedio\":", listarBody, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region POST /api/ativos/{id}/rendimentos - Registrar dividendo

    [Fact]
    public async Task RegistrarDividendo_ComSucesso_Retorna201ComDadosCorretos()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro Direto",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativo);

        var request = new RegistrarDividendoRequest
        {
            Valor = 50.50m,
            Data = new DateOnly(2026, 8, 5)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync($"/api/ativos/{ativo.Id}/rendimentos", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var responseBody = await response.Content.ReadAsStringAsync();
        var rendimentoResponse = JsonSerializer.Deserialize<RendimentoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(rendimentoResponse);
        Assert.NotEqual(Guid.Empty, rendimentoResponse.Id);
        Assert.Equal(ativo.Id, rendimentoResponse.AtivoId);
        Assert.Equal(50.50m, rendimentoResponse.Valor);
        Assert.Equal(new DateOnly(2026, 8, 5), rendimentoResponse.Data);
        Assert.Equal("DIVIDENDO", rendimentoResponse.Tipo);
        Assert.Equal("MANUAL", rendimentoResponse.Origem);
    }

    [Fact]
    public async Task RegistrarDividendo_ComValorMenorOuIgualAZero_Retorna400()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro Direto",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativo);

        var requestZero = new RegistrarDividendoRequest
        {
            Valor = 0m,
            Data = new DateOnly(2026, 8, 5)
        };

        var json = JsonSerializer.Serialize(requestZero, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync($"/api/ativos/{ativo.Id}/rendimentos", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Test with negative value as well
        var requestNegative = new RegistrarDividendoRequest
        {
            Valor = -100m,
            Data = new DateOnly(2026, 8, 5)
        };

        json = JsonSerializer.Serialize(requestNegative, AtivosControllerTestsFixture.JsonOptions);
        content = new StringContent(json, Encoding.UTF8, "application/json");

        response = await _fixture.Client.PostAsync($"/api/ativos/{ativo.Id}/rendimentos", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarDividendo_ComAtivoInexistente_Retorna404()
    {
        // Arrange
        var ativoIdInexistente = Guid.NewGuid();

        var request = new RegistrarDividendoRequest
        {
            Valor = 50m,
            Data = new DateOnly(2026, 8, 5)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync($"/api/ativos/{ativoIdInexistente}/rendimentos", content);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarDividendo_ComAtivoDesativado_Retorna404()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro Desativado",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = false,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativo);

        var request = new RegistrarDividendoRequest
        {
            Valor = 50m,
            Data = new DateOnly(2026, 8, 5)
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync($"/api/ativos/{ativo.Id}/rendimentos", content);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /api/ativos/{id}/rendimentos - Obter historico de rendimentos

    [Fact]
    public async Task ObterHistoricoRendimentos_ComDividendoEValorizacao_RetornaAmbosordenadosPorData()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro Direto",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativo);

        // Registrar dividendo no dia 2026-08-01
        var dividendoRequest = new RegistrarDividendoRequest
        {
            Valor = 50m,
            Data = new DateOnly(2026, 8, 1)
        };

        var json = JsonSerializer.Serialize(dividendoRequest, AtivosControllerTestsFixture.JsonOptions);
        var dividendoContent = new StringContent(json, Encoding.UTF8, "application/json");
        await _fixture.Client.PostAsync($"/api/ativos/{ativo.Id}/rendimentos", dividendoContent);

        // Atualizar valor_atual para disparar valorizacao automatica no dia 2026-08-05
        var valorAtualRequest = new AtualizarValorAtualRequest
        {
            NovoValorAtual = 1100m
        };

        json = JsonSerializer.Serialize(valorAtualRequest, AtivosControllerTestsFixture.JsonOptions);
        var valorAtualContent = new StringContent(json, Encoding.UTF8, "application/json");
        await _fixture.Client.PatchAsync($"/api/ativos/{ativo.Id}/valor-atual", valorAtualContent);

        // Act
        var response = await _fixture.Client.GetAsync($"/api/ativos/{ativo.Id}/rendimentos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var rendimentos = JsonSerializer.Deserialize<List<RendimentoResponse>>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(rendimentos);
        Assert.Equal(2, rendimentos.Count);

        // Primeiro elemento deve ser o dividendo (2026-08-01)
        Assert.Equal("DIVIDENDO", rendimentos[0].Tipo);
        Assert.Equal("MANUAL", rendimentos[0].Origem);
        Assert.Equal(50m, rendimentos[0].Valor);
        Assert.Equal(new DateOnly(2026, 8, 1), rendimentos[0].Data);

        // Segundo elemento deve ser a valorizacao (2026-08-05, data de hoje no teste)
        Assert.Equal("VALORIZACAO", rendimentos[1].Tipo);
        Assert.Equal("AUTOMATICO", rendimentos[1].Origem);
        Assert.Equal(100m, rendimentos[1].Valor); // 1100 - 1000
    }

    #endregion

    #region PATCH /api/ativos/{id}/valor-atual + GET /api/ativos/{id}/rendimentos - Gatilho de valorizacao automatica

    [Fact]
    public async Task AtualizarValorAtual_CriaRendimentoValorizacao_ComOrigemAutomatica()
    {
        // Arrange
        await _fixture.ClearAsync();

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "ETF IBOV",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "XP",
            ValorInvestido = 5000m,
            ValorAtual = 5000m,
            DataCompra = new DateOnly(2024, 2, 20),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativo);

        var request = new AtualizarValorAtualRequest
        {
            NovoValorAtual = 5500m
        };

        var json = JsonSerializer.Serialize(request, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var patchResponse = await _fixture.Client.PatchAsync($"/api/ativos/{ativo.Id}/valor-atual", content);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var getResponse = await _fixture.Client.GetAsync($"/api/ativos/{ativo.Id}/rendimentos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var responseBody = await getResponse.Content.ReadAsStringAsync();
        var rendimentos = JsonSerializer.Deserialize<List<RendimentoResponse>>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(rendimentos);
        Assert.Single(rendimentos);

        var valorizacao = rendimentos[0];
        Assert.Equal("VALORIZACAO", valorizacao.Tipo);
        Assert.Equal("AUTOMATICO", valorizacao.Origem);
        Assert.Equal(500m, valorizacao.Valor); // 5500 - 5000 = 500
        Assert.Equal(ativo.Id, valorizacao.AtivoId);
    }

    #endregion

    #region GET /api/ativos/rendimentos-resumo - Resumo agregado de rendimentos

    [Fact]
    public async Task ObterResumoRendimentos_ComMultiplosAtivos_SomaTotalsDividendosEValorizacao()
    {
        // Arrange
        await _fixture.ClearAsync();
        await _fixture.ClearRendimentosAsync();

        var ativo1 = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Tesouro 1",
            Tipo = TipoAtivo.RendaFixa,
            Instituicao = "B3",
            ValorInvestido = 1000m,
            ValorAtual = 1000m,
            DataCompra = new DateOnly(2024, 1, 15),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        var ativo2 = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "ETF IBOV",
            Tipo = TipoAtivo.RendaVariavel,
            Instituicao = "XP",
            ValorInvestido = 5000m,
            ValorAtual = 5000m,
            DataCompra = new DateOnly(2024, 2, 20),
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _fixture.AddAtivoAsync(ativo1);
        await _fixture.AddAtivoAsync(ativo2);

        // Registrar dividendo no ativo1: 50
        var dividendoRequest1 = new RegistrarDividendoRequest
        {
            Valor = 50m,
            Data = new DateOnly(2026, 8, 1)
        };

        var json = JsonSerializer.Serialize(dividendoRequest1, AtivosControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var dividendoResponse1 = await _fixture.Client.PostAsync($"/api/ativos/{ativo1.Id}/rendimentos", content);
        Assert.Equal(HttpStatusCode.Created, dividendoResponse1.StatusCode);

        // Registrar dividendo no ativo2: 100
        var dividendoRequest2 = new RegistrarDividendoRequest
        {
            Valor = 100m,
            Data = new DateOnly(2026, 8, 1)
        };

        json = JsonSerializer.Serialize(dividendoRequest2, AtivosControllerTestsFixture.JsonOptions);
        content = new StringContent(json, Encoding.UTF8, "application/json");
        var dividendoResponse2 = await _fixture.Client.PostAsync($"/api/ativos/{ativo2.Id}/rendimentos", content);
        Assert.Equal(HttpStatusCode.Created, dividendoResponse2.StatusCode);

        // Atualizar valor_atual do ativo1: 1000 -> 1100 (valorizacao de 100)
        var valorAtualRequest1 = new AtualizarValorAtualRequest
        {
            NovoValorAtual = 1100m
        };

        json = JsonSerializer.Serialize(valorAtualRequest1, AtivosControllerTestsFixture.JsonOptions);
        content = new StringContent(json, Encoding.UTF8, "application/json");
        var patchResponse1 = await _fixture.Client.PatchAsync($"/api/ativos/{ativo1.Id}/valor-atual", content);
        Assert.Equal(HttpStatusCode.OK, patchResponse1.StatusCode);

        // Atualizar valor_atual do ativo2: 5000 -> 5500 (valorizacao de 500)
        var valorAtualRequest2 = new AtualizarValorAtualRequest
        {
            NovoValorAtual = 5500m
        };

        json = JsonSerializer.Serialize(valorAtualRequest2, AtivosControllerTestsFixture.JsonOptions);
        content = new StringContent(json, Encoding.UTF8, "application/json");
        var patchResponse2 = await _fixture.Client.PatchAsync($"/api/ativos/{ativo2.Id}/valor-atual", content);
        Assert.Equal(HttpStatusCode.OK, patchResponse2.StatusCode);

        // Act
        var response = await _fixture.Client.GetAsync("/api/ativos/rendimentos-resumo");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resumo = JsonSerializer.Deserialize<RendimentosResumoResponse>(responseBody, AtivosControllerTestsFixture.JsonOptions);

        Assert.NotNull(resumo);
        Assert.Equal(150m, resumo.TotalDividendos); // 50 + 100
        Assert.Equal(600m, resumo.TotalValorizacao); // 100 + 500
        Assert.Equal(4, resumo.Historico.Count()); // 2 dividendos + 2 valorizacoes
    }

    #endregion
}
