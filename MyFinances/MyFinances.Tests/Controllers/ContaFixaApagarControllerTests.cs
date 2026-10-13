using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyFinances.Data;
using MyFinances.DTOs.ContaFixa;
using MyFinances.DTOs;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Controllers;

public class ContaFixaApagarControllerWebApplicationFactory : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("ContaFixaApagarControllerTestDb"));
        });
    }
}

public class ContaFixaApagarControllerTestsFixture : IAsyncLifetime
{
    private readonly ContaFixaApagarControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public ContaFixaApagarControllerTestsFixture()
    {
        _factory = new ContaFixaApagarControllerWebApplicationFactory();
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
            Username = $"conta_fixa_apagar_test_{Guid.NewGuid():N}",
            Email = $"conta_fixa_apagar_test_{Guid.NewGuid():N}@example.com",
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

    public async Task<ContaFixa?> GetContaFixaByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.ContasFixas.FirstOrDefaultAsync(cf => cf.Id == id);
    }

    public async Task<Lancamento?> GetLancamentoByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Lancamentos.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<Lancamento>> GetLancamentosByContaFixaIdAsync(Guid contaFixaId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Lancamentos
            .Where(l => l.ContaFixaId == contaFixaId)
            .ToListAsync();
    }

    public async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.RemoveRange(await dbContext.Lancamentos.ToListAsync());
        dbContext.ContasFixas.RemoveRange(await dbContext.ContasFixas.ToListAsync());
        dbContext.Contas.RemoveRange(await dbContext.Contas.ToListAsync());
        dbContext.Categorias.RemoveRange(await dbContext.Categorias.ToListAsync());
        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition("ContaFixa Apagar Controller Collection")]
public class ContaFixaApagarControllerCollection : ICollectionFixture<ContaFixaApagarControllerTestsFixture>
{
}

[Collection("ContaFixa Apagar Controller Collection")]
public class ContaFixaApagarControllerTests
{
    private readonly ContaFixaApagarControllerTestsFixture _fixture;

    public ContaFixaApagarControllerTests(ContaFixaApagarControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region DELETE /api/contas-fixas/{id}

    [Fact]
    public async Task Apagar_ComIdExistente_Retorna204eGetPosteriorRetorna404()
    {
        await _fixture.ClearAsync();

        var contaConta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Corrente Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 10000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaConta);

        var criarRequest = new CriarContaFixaRequest
        {
            ContaId = contaConta.Id,
            Descricao = "Aluguel",
            Valor = 1500m,
            DiaVencimento = 15,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(criarRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var responseCriar = await _fixture.Client.PostAsync("/api/contas-fixas", content);
        Assert.Equal(HttpStatusCode.Created, responseCriar.StatusCode);

        var bodyCriar = await responseCriar.Content.ReadAsStringAsync();
        var contaFixaCriada = JsonSerializer.Deserialize<ContaFixaResponse>(bodyCriar, ContaFixaApagarControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaFixaCriada);

        var responseApagar = await _fixture.Client.DeleteAsync($"/api/contas-fixas/{contaFixaCriada.Id}");

        Assert.Equal(HttpStatusCode.NoContent, responseApagar.StatusCode);

        var responseGet = await _fixture.Client.GetAsync($"/api/contas-fixas/{contaFixaCriada.Id}");
        Assert.Equal(HttpStatusCode.NotFound, responseGet.StatusCode);

        var contaFixaDb = await _fixture.GetContaFixaByIdAsync(contaFixaCriada.Id);
        Assert.Null(contaFixaDb);
    }

    [Fact]
    public async Task Apagar_ComIdInexistente_Retorna404ComErro()
    {
        await _fixture.ClearAsync();

        var response = await _fixture.Client.DeleteAsync($"/api/contas-fixas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resultado = JsonSerializer.Deserialize<JsonElement>(responseBody, ContaFixaApagarControllerTestsFixture.JsonOptions);
        Assert.True(resultado.TryGetProperty("erro", out var erro));
        Assert.Equal("Conta fixa nao encontrada", erro.GetString());
    }

    [Fact]
    public async Task Apagar_ComLancamentoPago_MantemLancamentoListadoSemContaFixa()
    {
        await _fixture.ClearAsync();

        var contaConta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Corrente Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 10000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaConta);

        var criarRequest = new CriarContaFixaRequest
        {
            ContaId = contaConta.Id,
            Descricao = "Aluguel",
            Valor = 1500m,
            DiaVencimento = 15,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(criarRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var responseCriar = await _fixture.Client.PostAsync("/api/contas-fixas", content);
        Assert.Equal(HttpStatusCode.Created, responseCriar.StatusCode);

        var bodyCriar = await responseCriar.Content.ReadAsStringAsync();
        var contaFixaCriada = JsonSerializer.Deserialize<ContaFixaResponse>(bodyCriar, ContaFixaApagarControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaFixaCriada);

        var lancamentoPago = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaConta.Id,
            ContaFixaId = contaFixaCriada.Id,
            Descricao = "Aluguel - Pago",
            Valor = 1500m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            Status = StatusLancamento.Pago,
            Manual = true
        };

        await _fixture.AddLancamentoAsync(lancamentoPago);

        var responseApagar = await _fixture.Client.DeleteAsync($"/api/contas-fixas/{contaFixaCriada.Id}");
        Assert.Equal(HttpStatusCode.NoContent, responseApagar.StatusCode);

        var responseFluxo = await _fixture.Client.GetAsync($"/api/contas/{contaConta.Id}/lancamentos/fluxo-caixa");
        Assert.Equal(HttpStatusCode.OK, responseFluxo.StatusCode);

        var bodyFluxo = await responseFluxo.Content.ReadAsStringAsync();
        var lancamentos = JsonSerializer.Deserialize<List<LancamentoResponseDto>>(bodyFluxo, ContaFixaApagarControllerTestsFixture.JsonOptions);
        Assert.NotNull(lancamentos);
        Assert.Contains(lancamentos, l => l.Id == lancamentoPago.Id && l.Status == StatusLancamento.Pago.ToStorageValue());

        var lancamentoDb = await _fixture.GetLancamentoByIdAsync(lancamentoPago.Id);
        Assert.NotNull(lancamentoDb);
        Assert.Null(lancamentoDb.ContaFixaId);
        Assert.Equal(StatusLancamento.Pago, lancamentoDb.Status);
        Assert.Equal(1500m, lancamentoDb.Valor);
    }

    [Fact]
    public async Task Apagar_ComOcorrenciaPendente_RemoveOcorrenciaDaListagemEDoBanco()
    {
        await _fixture.ClearAsync();

        var contaConta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Corrente Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 10000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaConta);

        var criarRequest = new CriarContaFixaRequest
        {
            ContaId = contaConta.Id,
            Descricao = "Aluguel",
            Valor = 1500m,
            DiaVencimento = 15,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(criarRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var responseCriar = await _fixture.Client.PostAsync("/api/contas-fixas", content);
        Assert.Equal(HttpStatusCode.Created, responseCriar.StatusCode);

        var bodyCriar = await responseCriar.Content.ReadAsStringAsync();
        var contaFixaCriada = JsonSerializer.Deserialize<ContaFixaResponse>(bodyCriar, ContaFixaApagarControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaFixaCriada);

        var pendentesAntes = await _fixture.GetLancamentosByContaFixaIdAsync(contaFixaCriada.Id);
        Assert.Equal(2, pendentesAntes.Count);
        Assert.All(pendentesAntes, l => Assert.Equal(StatusLancamento.Pendente, l.Status));
        var idOcorrencia = pendentesAntes[0].Id;

        var responseApagar = await _fixture.Client.DeleteAsync($"/api/contas-fixas/{contaFixaCriada.Id}");
        Assert.Equal(HttpStatusCode.NoContent, responseApagar.StatusCode);

        var responseFluxo = await _fixture.Client.GetAsync($"/api/contas/{contaConta.Id}/lancamentos/fluxo-caixa");
        Assert.Equal(HttpStatusCode.OK, responseFluxo.StatusCode);

        var bodyFluxo = await responseFluxo.Content.ReadAsStringAsync();
        var lancamentos = JsonSerializer.Deserialize<List<LancamentoResponseDto>>(bodyFluxo, ContaFixaApagarControllerTestsFixture.JsonOptions);
        Assert.NotNull(lancamentos);
        Assert.DoesNotContain(lancamentos, l => l.Id == idOcorrencia);

        var lancamentoDb = await _fixture.GetLancamentoByIdAsync(idOcorrencia);
        Assert.Null(lancamentoDb);

        var vinculadosApos = await _fixture.GetLancamentosByContaFixaIdAsync(contaFixaCriada.Id);
        Assert.Empty(vinculadosApos);
    }

    #endregion
}
