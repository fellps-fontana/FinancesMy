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
using MyFinances.DTOs.ContaReceber;
using MyFinances.DTOs;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Controllers;

public class ContaReceberControllerWebApplicationFactory : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("ContaReceberControllerTestDb"));
        });
    }
}

public class ContaReceberControllerTestsFixture : IAsyncLifetime
{
    private readonly ContaReceberControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public ContaReceberControllerTestsFixture()
    {
        _factory = new ContaReceberControllerWebApplicationFactory();
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
            Username = $"contas_receber_test_{Guid.NewGuid():N}",
            Email = $"contas_receber_test_{Guid.NewGuid():N}@example.com",
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

    public async Task<ContaReceber?> GetContaReceberByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.ContasReceber.FirstOrDefaultAsync(cr => cr.Id == id);
    }

    public async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.RemoveRange(await dbContext.Lancamentos.ToListAsync());
        dbContext.Transferencias.RemoveRange(await dbContext.Transferencias.ToListAsync());
        dbContext.ContasReceber.RemoveRange(await dbContext.ContasReceber.ToListAsync());
        dbContext.Contas.RemoveRange(await dbContext.Contas.ToListAsync());
        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition("ContaReceber Controller Collection")]
public class ContaReceberControllerCollection : ICollectionFixture<ContaReceberControllerTestsFixture>
{
}

[Collection("ContaReceber Controller Collection")]
public class ContaReceberControllerTests
{
    private readonly ContaReceberControllerTestsFixture _fixture;

    public ContaReceberControllerTests(ContaReceberControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region POST /api/contas-receber/recebiveis

    [Fact]
    public async Task RegistrarRecebivel_ComDadosValidos_Retorna201ComTipoRecebivelStatusPendente()
    {
        await _fixture.ClearAsync();

        var request = new RegistrarRecebivelRequest
        {
            Descricao = "Venda de produto",
            ValorTotal = 1000m,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow),
            DataPrevista = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/contas-receber/recebiveis", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resultado = JsonSerializer.Deserialize<ContaReceberResponse>(responseBody, ContaReceberControllerTestsFixture.JsonOptions);

        Assert.NotNull(resultado);
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal("RECEBIVEL", resultado.Tipo);
        Assert.Equal("Venda de produto", resultado.Descricao);
        Assert.Equal(1000m, resultado.ValorTotal);
        Assert.Equal(1000m, resultado.SaldoPendente);
        Assert.Equal("PENDENTE", resultado.Status);
    }

    #endregion

    #region POST /api/contas-receber/emprestimos

    [Fact]
    public async Task RegistrarEmprestimo_ComDadosValidosContaExistente_Retorna201ComTipoEmprestimoePessoa()
    {
        await _fixture.ClearAsync();

        var contaOrigem = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Corrente",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaOrigem);

        var request = new RegistrarEmprestimoRequest
        {
            Descricao = "Emprestimo para amigo",
            Pessoa = "João Silva",
            ValorTotal = 500m,
            ContaOrigemId = contaOrigem.Id,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow),
            DataPrevista = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60))
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/contas-receber/emprestimos", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var resultado = JsonSerializer.Deserialize<ContaReceberResponse>(responseBody, ContaReceberControllerTestsFixture.JsonOptions);

        Assert.NotNull(resultado);
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal("EMPRESTIMO", resultado.Tipo);
        Assert.Equal("João Silva", resultado.Pessoa);
        Assert.Equal(500m, resultado.ValorTotal);
        Assert.Equal(500m, resultado.SaldoPendente);
        Assert.Equal("PENDENTE", resultado.Status);
    }

    [Fact]
    public async Task RegistrarEmprestimo_SemPessoa_Retorna422()
    {
        await _fixture.ClearAsync();

        var contaOrigem = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Corrente",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaOrigem);

        var request = new RegistrarEmprestimoRequest
        {
            Descricao = "Emprestimo",
            Pessoa = "",
            ValorTotal = 500m,
            ContaOrigemId = contaOrigem.Id,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/contas-receber/emprestimos", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarEmprestimo_ComContaOrigemInexistente_Retorna404()
    {
        await _fixture.ClearAsync();

        var contaNaoExistenteId = Guid.NewGuid();

        var request = new RegistrarEmprestimoRequest
        {
            Descricao = "Emprestimo",
            Pessoa = "João Silva",
            ValorTotal = 500m,
            ContaOrigemId = contaNaoExistenteId,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/contas-receber/emprestimos", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/contas-receber/{id}/recebimentos

    [Fact]
    public async Task RegistrarRecebimento_Parcial_Retorna200eStatusMudaParaPARCIAL()
    {
        await _fixture.ClearAsync();

        var contaRecebimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Recebimento",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 10000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaRecebimento);

        var recebivelRequest = new RegistrarRecebivelRequest
        {
            Descricao = "Venda de produto",
            ValorTotal = 1000m,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json1 = JsonSerializer.Serialize(recebivelRequest);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        var responseRecebivel = await _fixture.Client.PostAsync("/api/contas-receber/recebiveis", content1);
        Assert.Equal(HttpStatusCode.Created, responseRecebivel.StatusCode);

        var bodyRecebivel = await responseRecebivel.Content.ReadAsStringAsync();
        var recebivel = JsonSerializer.Deserialize<ContaReceberResponse>(bodyRecebivel, ContaReceberControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivel);

        var recebimentoRequest = new RegistrarRecebimentoRequest
        {
            Valor = 600m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            ContaDestinoId = contaRecebimento.Id
        };

        var json2 = JsonSerializer.Serialize(recebimentoRequest);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

        var responseRecebimento = await _fixture.Client.PostAsync($"/api/contas-receber/{recebivel.Id}/recebimentos", content2);

        Assert.Equal(HttpStatusCode.OK, responseRecebimento.StatusCode);

        var getResponse = await _fixture.Client.GetAsync($"/api/contas-receber/{recebivel.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var bodyGet = await getResponse.Content.ReadAsStringAsync();
        var recebivelAtualized = JsonSerializer.Deserialize<ContaReceberResponse>(bodyGet, ContaReceberControllerTestsFixture.JsonOptions);

        Assert.NotNull(recebivelAtualized);
        Assert.Equal("PARCIAL", recebivelAtualized.Status);
        Assert.Equal(400m, recebivelAtualized.SaldoPendente);
    }

    [Fact]
    public async Task RegistrarRecebimento_ZeraSaldoPendente_Retorna200eStatusMudaParaRECEBIDO()
    {
        await _fixture.ClearAsync();

        var contaRecebimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Recebimento",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 10000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaRecebimento);

        var recebivelRequest = new RegistrarRecebivelRequest
        {
            Descricao = "Venda de produto",
            ValorTotal = 1000m,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json1 = JsonSerializer.Serialize(recebivelRequest);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        var responseRecebivel = await _fixture.Client.PostAsync("/api/contas-receber/recebiveis", content1);
        Assert.Equal(HttpStatusCode.Created, responseRecebivel.StatusCode);

        var bodyRecebivel = await responseRecebivel.Content.ReadAsStringAsync();
        var recebivel = JsonSerializer.Deserialize<ContaReceberResponse>(bodyRecebivel, ContaReceberControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivel);

        var recebimentoRequest = new RegistrarRecebimentoRequest
        {
            Valor = 1000m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            ContaDestinoId = contaRecebimento.Id
        };

        var json2 = JsonSerializer.Serialize(recebimentoRequest);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

        var responseRecebimento = await _fixture.Client.PostAsync($"/api/contas-receber/{recebivel.Id}/recebimentos", content2);

        Assert.Equal(HttpStatusCode.OK, responseRecebimento.StatusCode);

        var getResponse = await _fixture.Client.GetAsync($"/api/contas-receber/{recebivel.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var bodyGet = await getResponse.Content.ReadAsStringAsync();
        var recebivelAtualizado = JsonSerializer.Deserialize<ContaReceberResponse>(bodyGet, ContaReceberControllerTestsFixture.JsonOptions);

        Assert.NotNull(recebivelAtualizado);
        Assert.Equal("RECEBIDO", recebivelAtualizado.Status);
        Assert.Equal(0m, recebivelAtualizado.SaldoPendente);
    }

    [Fact]
    public async Task RegistrarRecebimento_ValorExcedeSaldoPendente_Retorna422eEstadoNaoMuda()
    {
        await _fixture.ClearAsync();

        var contaRecebimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Recebimento",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 10000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaRecebimento);

        var recebivelRequest = new RegistrarRecebivelRequest
        {
            Descricao = "Venda de produto",
            ValorTotal = 1000m,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json1 = JsonSerializer.Serialize(recebivelRequest);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        var responseRecebivel = await _fixture.Client.PostAsync("/api/contas-receber/recebiveis", content1);
        Assert.Equal(HttpStatusCode.Created, responseRecebivel.StatusCode);

        var bodyRecebivel = await responseRecebivel.Content.ReadAsStringAsync();
        var recebivel = JsonSerializer.Deserialize<ContaReceberResponse>(bodyRecebivel, ContaReceberControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivel);

        var recebimentoRequest = new RegistrarRecebimentoRequest
        {
            Valor = 1500m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            ContaDestinoId = contaRecebimento.Id
        };

        var json2 = JsonSerializer.Serialize(recebimentoRequest);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

        var responseRecebimento = await _fixture.Client.PostAsync($"/api/contas-receber/{recebivel.Id}/recebimentos", content2);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, responseRecebimento.StatusCode);

        var getResponse = await _fixture.Client.GetAsync($"/api/contas-receber/{recebivel.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var bodyGet = await getResponse.Content.ReadAsStringAsync();
        var recebivelVerificacao = JsonSerializer.Deserialize<ContaReceberResponse>(bodyGet, ContaReceberControllerTestsFixture.JsonOptions);

        Assert.NotNull(recebivelVerificacao);
        Assert.Equal("PENDENTE", recebivelVerificacao.Status);
        Assert.Equal(1000m, recebivelVerificacao.SaldoPendente);
    }

    [Fact]
    public async Task RegistrarRecebimento_ContaReceberInexistente_Retorna404()
    {
        await _fixture.ClearAsync();

        var contaRecebimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Recebimento",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 10000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaRecebimento);

        var contaReceberNaoExistenteId = Guid.NewGuid();

        var recebimentoRequest = new RegistrarRecebimentoRequest
        {
            Valor = 100m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            ContaDestinoId = contaRecebimento.Id
        };

        var json = JsonSerializer.Serialize(recebimentoRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync($"/api/contas-receber/{contaReceberNaoExistenteId}/recebimentos", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RegistrarRecebimento_ContaDestinoInexistente_Retorna404()
    {
        await _fixture.ClearAsync();

        var recebivelRequest = new RegistrarRecebivelRequest
        {
            Descricao = "Venda de produto",
            ValorTotal = 1000m,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json1 = JsonSerializer.Serialize(recebivelRequest);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        var responseRecebivel = await _fixture.Client.PostAsync("/api/contas-receber/recebiveis", content1);
        Assert.Equal(HttpStatusCode.Created, responseRecebivel.StatusCode);

        var bodyRecebivel = await responseRecebivel.Content.ReadAsStringAsync();
        var recebivel = JsonSerializer.Deserialize<ContaReceberResponse>(bodyRecebivel, ContaReceberControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivel);

        var contaDestinoNaoExistenteId = Guid.NewGuid();

        var recebimentoRequest = new RegistrarRecebimentoRequest
        {
            Valor = 100m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            ContaDestinoId = contaDestinoNaoExistenteId
        };

        var json2 = JsonSerializer.Serialize(recebimentoRequest);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync($"/api/contas-receber/{recebivel.Id}/recebimentos", content2);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GET /api/contas-receber

    [Fact]
    public async Task Listar_SemFiltro_Retorna200comListaCompleta()
    {
        await _fixture.ClearAsync();

        var recebivelRequest1 = new RegistrarRecebivelRequest
        {
            Descricao = "Venda 1",
            ValorTotal = 1000m,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var recebivelRequest2 = new RegistrarRecebivelRequest
        {
            Descricao = "Venda 2",
            ValorTotal = 500m,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json1 = JsonSerializer.Serialize(recebivelRequest1);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");
        var response1 = await _fixture.Client.PostAsync("/api/contas-receber/recebiveis", content1);

        var json2 = JsonSerializer.Serialize(recebivelRequest2);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");
        var response2 = await _fixture.Client.PostAsync("/api/contas-receber/recebiveis", content2);

        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        var getResponse = await _fixture.Client.GetAsync("/api/contas-receber");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var bodyGet = await getResponse.Content.ReadAsStringAsync();
        var lista = JsonSerializer.Deserialize<List<ContaReceberResponse>>(bodyGet, ContaReceberControllerTestsFixture.JsonOptions);

        Assert.NotNull(lista);
        Assert.True(lista.Count >= 2);
    }

    [Fact]
    public async Task Listar_ComFiltroStatus_Retorna200eFiltraCorretamente()
    {
        await _fixture.ClearAsync();

        var contaRecebimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Recebimento",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 10000m,
            Ativa = true
        };

        await _fixture.AddContaAsync(contaRecebimento);

        var recebivelRequest1 = new RegistrarRecebivelRequest
        {
            Descricao = "Venda 1",
            ValorTotal = 1000m,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json1 = JsonSerializer.Serialize(recebivelRequest1);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");
        var responseRecebivel1 = await _fixture.Client.PostAsync("/api/contas-receber/recebiveis", content1);

        var bodyRecebivel1 = await responseRecebivel1.Content.ReadAsStringAsync();
        var recebivel1 = JsonSerializer.Deserialize<ContaReceberResponse>(bodyRecebivel1, ContaReceberControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivel1);

        var recebivelRequest2 = new RegistrarRecebivelRequest
        {
            Descricao = "Venda 2",
            ValorTotal = 500m,
            DataRegistro = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var json2 = JsonSerializer.Serialize(recebivelRequest2);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");
        var responseRecebivel2 = await _fixture.Client.PostAsync("/api/contas-receber/recebiveis", content2);

        var bodyRecebivel2 = await responseRecebivel2.Content.ReadAsStringAsync();
        var recebivel2 = JsonSerializer.Deserialize<ContaReceberResponse>(bodyRecebivel2, ContaReceberControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivel2);

        var recebimentoRequest = new RegistrarRecebimentoRequest
        {
            Valor = 300m,
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            ContaDestinoId = contaRecebimento.Id
        };

        var jsonRecebimento = JsonSerializer.Serialize(recebimentoRequest);
        var contentRecebimento = new StringContent(jsonRecebimento, Encoding.UTF8, "application/json");
        await _fixture.Client.PostAsync($"/api/contas-receber/{recebivel2.Id}/recebimentos", contentRecebimento);

        var getResponse = await _fixture.Client.GetAsync("/api/contas-receber?status=PARCIAL");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var bodyGet = await getResponse.Content.ReadAsStringAsync();
        var listaFiltrada = JsonSerializer.Deserialize<List<ContaReceberResponse>>(bodyGet, ContaReceberControllerTestsFixture.JsonOptions);

        Assert.NotNull(listaFiltrada);
        Assert.All(listaFiltrada, item => Assert.Equal("PARCIAL", item.Status));
    }

    #endregion

    #region GET /api/contas-receber/{id}

    [Fact]
    public async Task ObterPorId_IdInexistente_Retorna404()
    {
        await _fixture.ClearAsync();

        var idInexistente = Guid.NewGuid();

        var response = await _fixture.Client.GetAsync($"/api/contas-receber/{idInexistente}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
