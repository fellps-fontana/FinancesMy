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
using MyFinances.DTOs.ContaFixa;
using MyFinances.DTOs;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Controllers;

public class ContaFixaControllerWebApplicationFactory : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("ContaFixaControllerTestDb"));
        });
    }
}

public class ContaFixaControllerTestsFixture : IAsyncLifetime
{
    private readonly ContaFixaControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public ContaFixaControllerTestsFixture()
    {
        _factory = new ContaFixaControllerWebApplicationFactory();
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
            Username = $"conta_fixa_test_{Guid.NewGuid():N}",
            Email = $"conta_fixa_test_{Guid.NewGuid():N}@example.com",
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

    public async Task AddCategoriaAsync(Categoria categoria)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Categorias.Add(categoria);
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

    public async Task<List<Lancamento>> GetLancamentosByContaFixaIdAsync(Guid contaFixaId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Lancamentos
            .Where(l => l.ContaFixaId == contaFixaId)
            .ToListAsync();
    }

    public async Task<List<ContaFixa>> GetAllContasFixasAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.ContasFixas.ToListAsync();
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

[CollectionDefinition("ContaFixa Controller Collection")]
public class ContaFixaControllerCollection : ICollectionFixture<ContaFixaControllerTestsFixture>
{
}

[Collection("ContaFixa Controller Collection")]
public class ContaFixaControllerTests
{
    private readonly ContaFixaControllerTestsFixture _fixture;

    public ContaFixaControllerTests(ContaFixaControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region POST /api/contas-fixas

    [Fact]
    public async Task Criar_ComDadosValidos_Retorna201eGerados2LancamentospendentesVinculados()
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

        var request = new CriarContaFixaRequest
        {
            ContaId = contaConta.Id,
            Descricao = "Aluguel",
            Valor = 1500m,
            DiaVencimento = 15,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/contas-fixas", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resultado = JsonSerializer.Deserialize<ContaFixaResponse>(responseBody, ContaFixaControllerTestsFixture.JsonOptions);

        Assert.NotNull(resultado);
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(contaConta.Id, resultado.ContaId);
        Assert.Equal("Aluguel", resultado.Descricao);
        Assert.Equal(1500m, resultado.Valor);
        Assert.Equal(15, resultado.DiaVencimento);
        Assert.True(resultado.Ativa);

        var lancamentosVinculados = await _fixture.GetLancamentosByContaFixaIdAsync(resultado.Id);

        Assert.Equal(2, lancamentosVinculados.Count);
        Assert.All(lancamentosVinculados, l => Assert.Equal(StatusLancamento.Pendente, l.Status));
        Assert.All(lancamentosVinculados, l => Assert.Equal(resultado.Id, l.ContaFixaId));
    }

    [Fact]
    public async Task Criar_ComContaIdInexistente_Retorna400()
    {
        await _fixture.ClearAsync();

        var request = new CriarContaFixaRequest
        {
            ContaId = Guid.NewGuid(),
            Descricao = "Aluguel",
            Valor = 1500m,
            DiaVencimento = 15,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/contas-fixas", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PUT /api/contas-fixas/{id}

    [Fact]
    public async Task Editar_ComIdExistente_Retorna200eAtualizaValorCategoriaLancamentosPendentes()
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

        var categoria1 = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Aluguel",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        var categoria2 = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Moradia",
            Tipo = TipoCategoria.Despesa,
            Arquivada = false
        };

        await _fixture.AddContaAsync(contaConta);
        await _fixture.AddCategoriaAsync(categoria1);
        await _fixture.AddCategoriaAsync(categoria2);

        var criarRequest = new CriarContaFixaRequest
        {
            ContaId = contaConta.Id,
            Descricao = "Aluguel",
            Valor = 1500m,
            DiaVencimento = 15,
            CategoriaId = categoria1.Id
        };

        var json1 = JsonSerializer.Serialize(criarRequest);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        var responseCriar = await _fixture.Client.PostAsync("/api/contas-fixas", content1);
        Assert.Equal(HttpStatusCode.Created, responseCriar.StatusCode);

        var bodyResponse = await responseCriar.Content.ReadAsStringAsync();
        var contaFixaCriada = JsonSerializer.Deserialize<ContaFixaResponse>(bodyResponse, ContaFixaControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaFixaCriada);

        var lancamentosPendentes = await _fixture.GetLancamentosByContaFixaIdAsync(contaFixaCriada.Id);
        Assert.Equal(2, lancamentosPendentes.Count);

        var lancamentoPago = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaConta.Id,
            ContaFixaId = contaFixaCriada.Id,
            CategoriaId = categoria1.Id,
            Descricao = "Aluguel - Pago",
            Valor = 1500m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            Status = StatusLancamento.Pago,
            Manual = true
        };

        await _fixture.AddLancamentoAsync(lancamentoPago);

        var editarRequest = new EditarContaFixaRequest
        {
            Valor = 2000m,
            DiaVencimento = 20,
            CategoriaId = categoria2.Id
        };

        var json2 = JsonSerializer.Serialize(editarRequest);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

        var responseEditar = await _fixture.Client.PutAsync($"/api/contas-fixas/{contaFixaCriada.Id}", content2);

        Assert.Equal(HttpStatusCode.OK, responseEditar.StatusCode);

        var bodyEditarResponse = await responseEditar.Content.ReadAsStringAsync();
        var contaFixaEditada = JsonSerializer.Deserialize<ContaFixaResponse>(bodyEditarResponse, ContaFixaControllerTestsFixture.JsonOptions);

        Assert.NotNull(contaFixaEditada);
        Assert.Equal(2000m, contaFixaEditada.Valor);
        Assert.Equal(20, contaFixaEditada.DiaVencimento);
        Assert.Equal(categoria2.Id, contaFixaEditada.CategoriaId);

        var lancamentosApos = await _fixture.GetLancamentosByContaFixaIdAsync(contaFixaCriada.Id);
        var pendentes = lancamentosApos.Where(l => l.Status == StatusLancamento.Pendente).ToList();
        var pagos = lancamentosApos.Where(l => l.Status == StatusLancamento.Pago).ToList();

        Assert.Equal(2, pendentes.Count);
        Assert.All(pendentes, l => Assert.Equal(2000m, l.Valor));
        Assert.All(pendentes, l => Assert.Equal(categoria2.Id, l.CategoriaId));

        Assert.Single(pagos);
        Assert.All(pagos, l => Assert.Equal(1500m, l.Valor));
        Assert.All(pagos, l => Assert.Equal(categoria1.Id, l.CategoriaId));
    }

    [Fact]
    public async Task Editar_ComIdInexistente_Retorna404()
    {
        await _fixture.ClearAsync();

        var editarRequest = new EditarContaFixaRequest
        {
            Valor = 2000m,
            DiaVencimento = 20,
            CategoriaId = null
        };

        var json = JsonSerializer.Serialize(editarRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PutAsync($"/api/contas-fixas/{Guid.NewGuid()}", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/contas-fixas/{id}/desativar

    [Fact]
    public async Task Desativar_ComIdExistente_Retorna204eDeletaLancamentosPendentesMantendoPagos()
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

        var json1 = JsonSerializer.Serialize(criarRequest);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        var responseCriar = await _fixture.Client.PostAsync("/api/contas-fixas", content1);
        Assert.Equal(HttpStatusCode.Created, responseCriar.StatusCode);

        var bodyResponse = await responseCriar.Content.ReadAsStringAsync();
        var contaFixaCriada = JsonSerializer.Deserialize<ContaFixaResponse>(bodyResponse, ContaFixaControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaFixaCriada);

        var lancamentosPendentes = await _fixture.GetLancamentosByContaFixaIdAsync(contaFixaCriada.Id);
        Assert.Equal(2, lancamentosPendentes.Count);

        var lancamentoPago = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaConta.Id,
            ContaFixaId = contaFixaCriada.Id,
            Descricao = "Aluguel - Pago",
            Valor = 1500m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            Status = StatusLancamento.Pago,
            Manual = true
        };

        await _fixture.AddLancamentoAsync(lancamentoPago);

        var responseDesativar = await _fixture.Client.PostAsync($"/api/contas-fixas/{contaFixaCriada.Id}/desativar", null);

        Assert.Equal(HttpStatusCode.NoContent, responseDesativar.StatusCode);

        var contaFixaAposDesativar = await _fixture.GetContaFixaByIdAsync(contaFixaCriada.Id);
        Assert.NotNull(contaFixaAposDesativar);
        Assert.False(contaFixaAposDesativar.Ativa);

        var lancamentosApos = await _fixture.GetLancamentosByContaFixaIdAsync(contaFixaCriada.Id);
        var pendentes = lancamentosApos.Where(l => l.Status == StatusLancamento.Pendente).ToList();
        var pagos = lancamentosApos.Where(l => l.Status == StatusLancamento.Pago).ToList();

        Assert.Empty(pendentes);
        Assert.Single(pagos);
    }

    #endregion

    #region POST /api/contas-fixas/{id}/reativar

    [Fact]
    public async Task Reativar_ComIdExistente_Retorna204eGeraNovosPendentesIdempotente()
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

        var json1 = JsonSerializer.Serialize(criarRequest);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        var responseCriar = await _fixture.Client.PostAsync("/api/contas-fixas", content1);
        Assert.Equal(HttpStatusCode.Created, responseCriar.StatusCode);

        var bodyResponse = await responseCriar.Content.ReadAsStringAsync();
        var contaFixaCriada = JsonSerializer.Deserialize<ContaFixaResponse>(bodyResponse, ContaFixaControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaFixaCriada);

        var responseDesativar = await _fixture.Client.PostAsync($"/api/contas-fixas/{contaFixaCriada.Id}/desativar", null);
        Assert.Equal(HttpStatusCode.NoContent, responseDesativar.StatusCode);

        var contaFixaAposDesativar = await _fixture.GetContaFixaByIdAsync(contaFixaCriada.Id);
        Assert.NotNull(contaFixaAposDesativar);
        Assert.False(contaFixaAposDesativar.Ativa);

        var responseReativar = await _fixture.Client.PostAsync($"/api/contas-fixas/{contaFixaCriada.Id}/reativar", null);
        Assert.Equal(HttpStatusCode.NoContent, responseReativar.StatusCode);

        var contaFixaAposReativar = await _fixture.GetContaFixaByIdAsync(contaFixaCriada.Id);
        Assert.NotNull(contaFixaAposReativar);
        Assert.True(contaFixaAposReativar.Ativa);

        var lancamentosAposReativar = await _fixture.GetLancamentosByContaFixaIdAsync(contaFixaCriada.Id);
        var pendentes = lancamentosAposReativar.Where(l => l.Status == StatusLancamento.Pendente).ToList();

        Assert.Equal(2, pendentes.Count);

        var responseReativarNovamente = await _fixture.Client.PostAsync($"/api/contas-fixas/{contaFixaCriada.Id}/reativar", null);
        Assert.Equal(HttpStatusCode.NoContent, responseReativarNovamente.StatusCode);

        var lancamentosAposDuplaReativacao = await _fixture.GetLancamentosByContaFixaIdAsync(contaFixaCriada.Id);
        var pendentesApos = lancamentosAposDuplaReativacao.Where(l => l.Status == StatusLancamento.Pendente).ToList();

        Assert.Equal(2, pendentesApos.Count);
    }

    #endregion

    #region GET /api/contas-fixas

    [Fact]
    public async Task Listar_ComFiltroAtiva_FiltraCorretamente()
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

        var criarRequest1 = new CriarContaFixaRequest
        {
            ContaId = contaConta.Id,
            Descricao = "Aluguel",
            Valor = 1500m,
            DiaVencimento = 15,
            CategoriaId = null
        };

        var json1 = JsonSerializer.Serialize(criarRequest1);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        var responseCriar1 = await _fixture.Client.PostAsync("/api/contas-fixas", content1);
        Assert.Equal(HttpStatusCode.Created, responseCriar1.StatusCode);

        var body1 = await responseCriar1.Content.ReadAsStringAsync();
        var contaFixa1 = JsonSerializer.Deserialize<ContaFixaResponse>(body1, ContaFixaControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaFixa1);

        var criarRequest2 = new CriarContaFixaRequest
        {
            ContaId = contaConta.Id,
            Descricao = "Internet",
            Valor = 100m,
            DiaVencimento = 10,
            CategoriaId = null
        };

        var json2 = JsonSerializer.Serialize(criarRequest2);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

        var responseCriar2 = await _fixture.Client.PostAsync("/api/contas-fixas", content2);
        Assert.Equal(HttpStatusCode.Created, responseCriar2.StatusCode);

        var body2 = await responseCriar2.Content.ReadAsStringAsync();
        var contaFixa2 = JsonSerializer.Deserialize<ContaFixaResponse>(body2, ContaFixaControllerTestsFixture.JsonOptions);
        Assert.NotNull(contaFixa2);

        var responseDesativar = await _fixture.Client.PostAsync($"/api/contas-fixas/{contaFixa2.Id}/desativar", null);
        Assert.Equal(HttpStatusCode.NoContent, responseDesativar.StatusCode);

        var responseListarAtivas = await _fixture.Client.GetAsync("/api/contas-fixas?ativa=true");
        Assert.Equal(HttpStatusCode.OK, responseListarAtivas.StatusCode);

        var bodyListarAtivas = await responseListarAtivas.Content.ReadAsStringAsync();
        var contasAtivas = JsonSerializer.Deserialize<List<ContaFixaResponse>>(bodyListarAtivas, ContaFixaControllerTestsFixture.JsonOptions);

        Assert.NotNull(contasAtivas);
        Assert.All(contasAtivas, c => Assert.True(c.Ativa));

        var responseListarInativas = await _fixture.Client.GetAsync("/api/contas-fixas?ativa=false");
        Assert.Equal(HttpStatusCode.OK, responseListarInativas.StatusCode);

        var bodyListarInativas = await responseListarInativas.Content.ReadAsStringAsync();
        var contasInativas = JsonSerializer.Deserialize<List<ContaFixaResponse>>(bodyListarInativas, ContaFixaControllerTestsFixture.JsonOptions);

        Assert.NotNull(contasInativas);
        Assert.All(contasInativas, c => Assert.False(c.Ativa));
    }

    #endregion

    #region GET /api/contas-fixas/{id}

    [Fact]
    public async Task ObterPorId_ComIdInexistente_Retorna404()
    {
        await _fixture.ClearAsync();

        var response = await _fixture.Client.GetAsync($"/api/contas-fixas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
