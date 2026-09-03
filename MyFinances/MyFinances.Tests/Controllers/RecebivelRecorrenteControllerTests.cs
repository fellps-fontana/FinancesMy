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
using MyFinances.DTOs.RecebivelRecorrente;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Controllers;

public class RecebivelRecorrenteControllerWebApplicationFactory : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("RecebivelRecorrenteControllerTestDb"));
        });
    }
}

public class RecebivelRecorrenteControllerTestsFixture : IAsyncLifetime
{
    private readonly RecebivelRecorrenteControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public RecebivelRecorrenteControllerTestsFixture()
    {
        _factory = new RecebivelRecorrenteControllerWebApplicationFactory();
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
            Username = $"recebivel_recorrente_test_{Guid.NewGuid():N}",
            Email = $"recebivel_recorrente_test_{Guid.NewGuid():N}@example.com",
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

    public async Task<RecebivelRecorrente?> GetRecebivelRecorrenteByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.RecebiveisRecorrentes
            .Include(rr => rr.Ocorrencias)
            .FirstOrDefaultAsync(rr => rr.Id == id);
    }

    public async Task<List<ContaReceber>> GetOcorrenciasByRecebivelRecorrenteIdAsync(Guid recebivelRecorrenteId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.ContasReceber
            .Where(cr => cr.RecebivelRecorrenteId == recebivelRecorrenteId)
            .ToListAsync();
    }

    public async Task<List<RecebivelRecorrente>> GetAllRecebivelRecorrentesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.RecebiveisRecorrentes.ToListAsync();
    }

    public async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.ContasReceber.RemoveRange(await dbContext.ContasReceber.ToListAsync());
        dbContext.RecebiveisRecorrentes.RemoveRange(await dbContext.RecebiveisRecorrentes.ToListAsync());
        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition("RecebivelRecorrente Controller Collection")]
public class RecebivelRecorrenteControllerCollection : ICollectionFixture<RecebivelRecorrenteControllerTestsFixture>
{
}

[Collection("RecebivelRecorrente Controller Collection")]
public class RecebivelRecorrenteControllerTests
{
    private readonly RecebivelRecorrenteControllerTestsFixture _fixture;

    public RecebivelRecorrenteControllerTests(RecebivelRecorrenteControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region POST /api/recebiveis-recorrentes

    [Fact]
    public async Task Criar_MensalComDadosValidos_Retorna201eGeradoUmaOuMaisOcorrenciasPendentes()
    {
        await _fixture.ClearAsync();

        var request = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Aluguel Recebivel",
            Valor = 2000m,
            Periodicidade = "MENSAL",
            DiaVencimento = 15,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resultado = JsonSerializer.Deserialize<RecebivelRecorrenteResponse>(responseBody, RecebivelRecorrenteControllerTestsFixture.JsonOptions);

        Assert.NotNull(resultado);
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal("Aluguel Recebivel", resultado.Descricao);
        Assert.Equal(2000m, resultado.Valor);
        Assert.Equal("MENSAL", resultado.Periodicidade);
        Assert.Equal(15, resultado.DiaVencimento);
        Assert.True(resultado.Ativa);

        var ocorrencias = await _fixture.GetOcorrenciasByRecebivelRecorrenteIdAsync(resultado.Id);
        Assert.NotEmpty(ocorrencias);
        Assert.All(ocorrencias, o => Assert.Equal(StatusContaReceber.Pendente, o.Status));
    }

    [Fact]
    public async Task Criar_SemanalComDadosValidos_Retorna201eDiaDaSemanaNoResponse()
    {
        await _fixture.ClearAsync();

        var request = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Consultoria Semanal",
            Valor = 500m,
            Periodicidade = "SEMANAL",
            DiaVencimento = null,
            MesReferencia = null,
            DiaDaSemana = "QUA",
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resultado = JsonSerializer.Deserialize<RecebivelRecorrenteResponse>(responseBody, RecebivelRecorrenteControllerTestsFixture.JsonOptions);

        Assert.NotNull(resultado);
        Assert.Equal("SEMANAL", resultado.Periodicidade);
        Assert.Equal("QUA", resultado.DiaDaSemana);
        Assert.Null(resultado.DiaVencimento);
    }

    [Fact]
    public async Task Criar_MensalSemDiaVencimento_Retorna400()
    {
        await _fixture.ClearAsync();

        var request = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Aluguel Recebivel",
            Valor = 2000m,
            Periodicidade = "MENSAL",
            DiaVencimento = null,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.True(errorResponse.TryGetProperty("erro", out _));
    }

    [Fact]
    public async Task Criar_AnualSemMesReferencia_Retorna400()
    {
        await _fixture.ClearAsync();

        var request = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Bonus Anual",
            Valor = 5000m,
            Periodicidade = "ANUAL",
            DiaVencimento = 31,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.True(errorResponse.TryGetProperty("erro", out _));
    }

    [Fact]
    public async Task Criar_SemanalSemDiaDaSemana_Retorna400()
    {
        await _fixture.ClearAsync();

        var request = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Consultoria Semanal",
            Valor = 500m,
            Periodicidade = "SEMANAL",
            DiaVencimento = null,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.True(errorResponse.TryGetProperty("erro", out _));
    }

    [Fact]
    public async Task Criar_ComValorMenorOuIgualZero_Retorna400()
    {
        await _fixture.ClearAsync();

        var request = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Recebivel Invalido",
            Valor = 0m,
            Periodicidade = "MENSAL",
            DiaVencimento = 15,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.True(errorResponse.TryGetProperty("erro", out _));
    }

    #endregion

    #region PUT /api/recebiveis-recorrentes/{id}

    [Fact]
    public async Task Editar_AlterandoValor_Retorna200eValorNovoNoResponse()
    {
        await _fixture.ClearAsync();

        var criarRequest = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Aluguel Recebivel",
            Valor = 2000m,
            Periodicidade = "MENSAL",
            DiaVencimento = 15,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var jsonCriar = JsonSerializer.Serialize(criarRequest);
        var contentCriar = new StringContent(jsonCriar, Encoding.UTF8, "application/json");

        var responseCriar = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", contentCriar);
        Assert.Equal(HttpStatusCode.Created, responseCriar.StatusCode);

        var bodyResponseCriar = await responseCriar.Content.ReadAsStringAsync();
        var recebivelCriado = JsonSerializer.Deserialize<RecebivelRecorrenteResponse>(bodyResponseCriar, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivelCriado);

        var editarRequest = new EditarRecebivelRecorrenteRequest
        {
            Valor = 2500m,
            Periodicidade = "MENSAL",
            DiaVencimento = 15,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var jsonEditar = JsonSerializer.Serialize(editarRequest);
        var contentEditar = new StringContent(jsonEditar, Encoding.UTF8, "application/json");

        var responseEditar = await _fixture.Client.PutAsync($"/api/recebiveis-recorrentes/{recebivelCriado.Id}", contentEditar);

        Assert.Equal(HttpStatusCode.OK, responseEditar.StatusCode);

        var bodyResponseEditar = await responseEditar.Content.ReadAsStringAsync();
        var recebivelEditado = JsonSerializer.Deserialize<RecebivelRecorrenteResponse>(bodyResponseEditar, RecebivelRecorrenteControllerTestsFixture.JsonOptions);

        Assert.NotNull(recebivelEditado);
        Assert.Equal(2500m, recebivelEditado.Valor);
    }

    [Fact]
    public async Task Editar_ComIdInexistente_Retorna404()
    {
        await _fixture.ClearAsync();

        var request = new EditarRecebivelRecorrenteRequest
        {
            Valor = 2500m,
            Periodicidade = "MENSAL",
            DiaVencimento = 15,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var idInexistente = Guid.NewGuid();
        var response = await _fixture.Client.PutAsync($"/api/recebiveis-recorrentes/{idInexistente}", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.True(errorResponse.TryGetProperty("erro", out _));
    }

    #endregion

    #region POST /api/recebiveis-recorrentes/{id}/desativar

    [Fact]
    public async Task Desativar_ComIdExistente_Retorna204eRecebivelInativo()
    {
        await _fixture.ClearAsync();

        var criarRequest = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Aluguel Recebivel",
            Valor = 2000m,
            Periodicidade = "MENSAL",
            DiaVencimento = 15,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var jsonCriar = JsonSerializer.Serialize(criarRequest);
        var contentCriar = new StringContent(jsonCriar, Encoding.UTF8, "application/json");

        var responseCriar = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", contentCriar);
        Assert.Equal(HttpStatusCode.Created, responseCriar.StatusCode);

        var bodyResponseCriar = await responseCriar.Content.ReadAsStringAsync();
        var recebivelCriado = JsonSerializer.Deserialize<RecebivelRecorrenteResponse>(bodyResponseCriar, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivelCriado);

        var responseDesativar = await _fixture.Client.PostAsync($"/api/recebiveis-recorrentes/{recebivelCriado.Id}/desativar", new StringContent(""));

        Assert.Equal(HttpStatusCode.NoContent, responseDesativar.StatusCode);

        var responseGet = await _fixture.Client.GetAsync($"/api/recebiveis-recorrentes/{recebivelCriado.Id}");
        Assert.Equal(HttpStatusCode.OK, responseGet.StatusCode);

        var bodyGet = await responseGet.Content.ReadAsStringAsync();
        var recebivelAtualizado = JsonSerializer.Deserialize<RecebivelRecorrenteResponse>(bodyGet, RecebivelRecorrenteControllerTestsFixture.JsonOptions);

        Assert.NotNull(recebivelAtualizado);
        Assert.False(recebivelAtualizado.Ativa);
    }

    #endregion

    #region POST /api/recebiveis-recorrentes/{id}/reativar

    [Fact]
    public async Task Reativar_ComIdExistente_Retorna204eRecebivelAtivo()
    {
        await _fixture.ClearAsync();

        var criarRequest = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Aluguel Recebivel",
            Valor = 2000m,
            Periodicidade = "MENSAL",
            DiaVencimento = 15,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var jsonCriar = JsonSerializer.Serialize(criarRequest);
        var contentCriar = new StringContent(jsonCriar, Encoding.UTF8, "application/json");

        var responseCriar = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", contentCriar);
        Assert.Equal(HttpStatusCode.Created, responseCriar.StatusCode);

        var bodyResponseCriar = await responseCriar.Content.ReadAsStringAsync();
        var recebivelCriado = JsonSerializer.Deserialize<RecebivelRecorrenteResponse>(bodyResponseCriar, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivelCriado);

        await _fixture.Client.PostAsync($"/api/recebiveis-recorrentes/{recebivelCriado.Id}/desativar", new StringContent(""));

        var responseReativar = await _fixture.Client.PostAsync($"/api/recebiveis-recorrentes/{recebivelCriado.Id}/reativar", new StringContent(""));

        Assert.Equal(HttpStatusCode.NoContent, responseReativar.StatusCode);

        var responseGet = await _fixture.Client.GetAsync($"/api/recebiveis-recorrentes/{recebivelCriado.Id}");
        Assert.Equal(HttpStatusCode.OK, responseGet.StatusCode);

        var bodyGet = await responseGet.Content.ReadAsStringAsync();
        var recebivelAtualizado = JsonSerializer.Deserialize<RecebivelRecorrenteResponse>(bodyGet, RecebivelRecorrenteControllerTestsFixture.JsonOptions);

        Assert.NotNull(recebivelAtualizado);
        Assert.True(recebivelAtualizado.Ativa);
    }

    #endregion

    #region GET /api/recebiveis-recorrentes

    [Fact]
    public async Task Listar_ComFiltroAtivaTrue_RetornaApenasRecebivelAtivos()
    {
        await _fixture.ClearAsync();

        var request1 = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Recebivel Ativo 1",
            Valor = 1000m,
            Periodicidade = "MENSAL",
            DiaVencimento = 10,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var request2 = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Recebivel Ativo 2",
            Valor = 2000m,
            Periodicidade = "MENSAL",
            DiaVencimento = 20,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var json1 = JsonSerializer.Serialize(request1);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");
        var response1 = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", content1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var json2 = JsonSerializer.Serialize(request2);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");
        var response2 = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", content2);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        var body1 = await response1.Content.ReadAsStringAsync();
        var recebivel1 = JsonSerializer.Deserialize<RecebivelRecorrenteResponse>(body1, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivel1);

        await _fixture.Client.PostAsync($"/api/recebiveis-recorrentes/{recebivel1.Id}/desativar", new StringContent(""));

        var responseListar = await _fixture.Client.GetAsync("/api/recebiveis-recorrentes?ativa=true");

        Assert.Equal(HttpStatusCode.OK, responseListar.StatusCode);

        var bodyListar = await responseListar.Content.ReadAsStringAsync();
        var recebiveisAtivos = JsonSerializer.Deserialize<List<RecebivelRecorrenteResponse>>(bodyListar, RecebivelRecorrenteControllerTestsFixture.JsonOptions);

        Assert.NotNull(recebiveisAtivos);
        Assert.All(recebiveisAtivos, r => Assert.True(r.Ativa));
        Assert.Single(recebiveisAtivos);
    }

    [Fact]
    public async Task Listar_ComFiltroAtivaFalse_RetornaApenasRecebivelInativos()
    {
        await _fixture.ClearAsync();

        var request1 = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Recebivel Inativo",
            Valor = 1000m,
            Periodicidade = "MENSAL",
            DiaVencimento = 10,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var request2 = new CriarRecebivelRecorrenteRequest
        {
            Descricao = "Recebivel Ativo",
            Valor = 2000m,
            Periodicidade = "MENSAL",
            DiaVencimento = 20,
            MesReferencia = null,
            DiaDaSemana = null,
            CategoriaId = null
        };

        var json1 = JsonSerializer.Serialize(request1);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");
        var response1 = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", content1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var json2 = JsonSerializer.Serialize(request2);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");
        var response2 = await _fixture.Client.PostAsync("/api/recebiveis-recorrentes", content2);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        var body1 = await response1.Content.ReadAsStringAsync();
        var recebivel1 = JsonSerializer.Deserialize<RecebivelRecorrenteResponse>(body1, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivel1);

        await _fixture.Client.PostAsync($"/api/recebiveis-recorrentes/{recebivel1.Id}/desativar", new StringContent(""));

        var responseListar = await _fixture.Client.GetAsync("/api/recebiveis-recorrentes?ativa=false");

        Assert.Equal(HttpStatusCode.OK, responseListar.StatusCode);

        var bodyListar = await responseListar.Content.ReadAsStringAsync();
        var recebiveisInativos = JsonSerializer.Deserialize<List<RecebivelRecorrenteResponse>>(bodyListar, RecebivelRecorrenteControllerTestsFixture.JsonOptions);

        Assert.NotNull(recebiveisInativos);
        Assert.All(recebiveisInativos, r => Assert.False(r.Ativa));
        Assert.Single(recebiveisInativos);
    }

    #endregion

    #region GET /api/recebiveis-recorrentes/{id}

    [Fact]
    public async Task ObterPorId_ComIdInexistente_Retorna404()
    {
        await _fixture.ClearAsync();

        var idInexistente = Guid.NewGuid();
        var response = await _fixture.Client.GetAsync($"/api/recebiveis-recorrentes/{idInexistente}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody, RecebivelRecorrenteControllerTestsFixture.JsonOptions);
        Assert.True(errorResponse.TryGetProperty("erro", out _));
    }

    #endregion
}
