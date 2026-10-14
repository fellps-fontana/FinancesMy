using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.DTOs.ContaReceber;
using Xunit;

namespace MyFinances.Tests.Controllers;

// Regra-de-negocio.md item 13 (edicao de Contas a Receber) — testes HTTP do PUT api/contas-receber/{id}.
public class ContaReceberEditarControllerWebApplicationFactory : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("ContaReceberEditarControllerTestDb"));
        });
    }
}

public class ContaReceberEditarControllerTestsFixture : IAsyncLifetime
{
    private readonly ContaReceberEditarControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public ContaReceberEditarControllerTestsFixture()
    {
        _factory = new ContaReceberEditarControllerWebApplicationFactory();
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
            Username = $"contas_receber_editar_test_{Guid.NewGuid():N}",
            Email = $"contas_receber_editar_test_{Guid.NewGuid():N}@example.com",
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

    public async Task AddContaReceberAsync(ContaReceber contaReceber)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.ContasReceber.Add(contaReceber);
        await dbContext.SaveChangesAsync();
    }

    public async Task<ContaReceber?> GetContaReceberByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.ContasReceber.AsNoTracking().FirstOrDefaultAsync(cr => cr.Id == id);
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

[CollectionDefinition("ContaReceberEditar Controller Collection")]
public class ContaReceberEditarControllerCollection : ICollectionFixture<ContaReceberEditarControllerTestsFixture>
{
}

[Collection("ContaReceberEditar Controller Collection")]
public class ContaReceberEditarControllerTests
{
    private readonly ContaReceberEditarControllerTestsFixture _fixture;

    public ContaReceberEditarControllerTests(ContaReceberEditarControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    private static StringContent ComoJson<T>(T body)
    {
        var json = JsonSerializer.Serialize(body, ContaReceberEditarControllerTestsFixture.JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private async Task<ContaReceberResponse> RegistrarRecebivelAsync(string descricao, decimal valorTotal, DateOnly? dataPrevista = null)
    {
        var request = new RegistrarRecebivelRequest
        {
            Descricao = descricao,
            ValorTotal = valorTotal,
            DataRegistro = new DateOnly(2026, 1, 1),
            DataPrevista = dataPrevista
        };

        var response = await _fixture.Client.PostAsync("/api/contas-receber/recebiveis", ComoJson(request));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var recebivel = JsonSerializer.Deserialize<ContaReceberResponse>(body, ContaReceberEditarControllerTestsFixture.JsonOptions);
        Assert.NotNull(recebivel);
        return recebivel;
    }

    private async Task<ContaReceberResponse> RegistrarEmprestimoAsync(string pessoa, decimal valorTotal)
    {
        var contaOrigem = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Corrente Editar",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 5000m,
            Ativa = true
        };
        await _fixture.AddContaAsync(contaOrigem);

        var request = new RegistrarEmprestimoRequest
        {
            Descricao = "Emprestimo editavel",
            Pessoa = pessoa,
            ValorTotal = valorTotal,
            ContaOrigemId = contaOrigem.Id,
            DataRegistro = new DateOnly(2026, 1, 1)
        };

        var response = await _fixture.Client.PostAsync("/api/contas-receber/emprestimos", ComoJson(request));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var emprestimo = JsonSerializer.Deserialize<ContaReceberResponse>(body, ContaReceberEditarControllerTestsFixture.JsonOptions);
        Assert.NotNull(emprestimo);
        return emprestimo;
    }

    private async Task RegistrarRecebimentoParcialAsync(Guid contaReceberId, decimal valor)
    {
        var contaDestino = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Destino Editar",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 10000m,
            Ativa = true
        };
        await _fixture.AddContaAsync(contaDestino);

        var request = new RegistrarRecebimentoRequest
        {
            Valor = valor,
            Data = new DateOnly(2026, 2, 1),
            ContaDestinoId = contaDestino.Id
        };

        var response = await _fixture.Client.PostAsync($"/api/contas-receber/{contaReceberId}/recebimentos", ComoJson(request));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<ContaReceberResponse> ObterAsync(Guid id)
    {
        var response = await _fixture.Client.GetAsync($"/api/contas-receber/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var resultado = JsonSerializer.Deserialize<ContaReceberResponse>(body, ContaReceberEditarControllerTestsFixture.JsonOptions);
        Assert.NotNull(resultado);
        return resultado;
    }

    #region PUT /api/contas-receber/{id} — valor_total

    [Fact]
    public async Task Editar_RecebivelPendenteComNovoValor_Retorna200ComNovoValorTotal()
    {
        await _fixture.ClearAsync();

        var recebivel = await RegistrarRecebivelAsync("Venda original", 1000m);

        var request = new EditarContaReceberRequest
        {
            Descricao = "Venda original",
            ValorTotal = 1500m
        };

        var response = await _fixture.Client.PutAsync($"/api/contas-receber/{recebivel.Id}", ComoJson(request));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var resultado = JsonSerializer.Deserialize<ContaReceberResponse>(body, ContaReceberEditarControllerTestsFixture.JsonOptions);
        Assert.NotNull(resultado);
        Assert.Equal(1500m, resultado.ValorTotal);
        Assert.Equal(1500m, resultado.SaldoPendente);

        var persistido = await ObterAsync(recebivel.Id);
        Assert.Equal(1500m, persistido.ValorTotal);
    }

    [Fact]
    public async Task Editar_RecebivelAposRecebimentoComValorDiferente_Retorna409ENaoAlteraValor()
    {
        await _fixture.ClearAsync();

        var recebivel = await RegistrarRecebivelAsync("Venda parcial", 1000m);
        await RegistrarRecebimentoParcialAsync(recebivel.Id, 400m);

        var request = new EditarContaReceberRequest
        {
            Descricao = "Venda parcial",
            ValorTotal = 2000m
        };

        var response = await _fixture.Client.PutAsync($"/api/contas-receber/{recebivel.Id}", ComoJson(request));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var persistido = await ObterAsync(recebivel.Id);
        Assert.Equal(1000m, persistido.ValorTotal);
        Assert.Equal("PARCIAL", persistido.Status);
    }

    [Fact]
    public async Task Editar_EmprestimoComValorDiferente_Retorna409ENaoAlteraValor()
    {
        await _fixture.ClearAsync();

        var emprestimo = await RegistrarEmprestimoAsync("Joao Silva", 500m);

        var request = new EditarContaReceberRequest
        {
            Descricao = "Emprestimo editavel",
            Pessoa = "Joao Silva",
            ValorTotal = 800m
        };

        var response = await _fixture.Client.PutAsync($"/api/contas-receber/{emprestimo.Id}", ComoJson(request));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var persistido = await ObterAsync(emprestimo.Id);
        Assert.Equal(500m, persistido.ValorTotal);
    }

    #endregion

    #region PUT /api/contas-receber/{id} — descricao, pessoa e data_prevista editaveis em qualquer status

    [Fact]
    public async Task Editar_RecebivelParcialAlterandoDescricaoEDataPrevista_Retorna200()
    {
        await _fixture.ClearAsync();

        var recebivel = await RegistrarRecebivelAsync("Venda antiga", 1000m, new DateOnly(2026, 3, 15));
        await RegistrarRecebimentoParcialAsync(recebivel.Id, 400m);

        var request = new EditarContaReceberRequest
        {
            Descricao = "Venda nova",
            DataPrevista = new DateOnly(2026, 4, 10)
        };

        var response = await _fixture.Client.PutAsync($"/api/contas-receber/{recebivel.Id}", ComoJson(request));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var persistido = await ObterAsync(recebivel.Id);
        Assert.Equal("Venda nova", persistido.Descricao);
        Assert.Equal(new DateOnly(2026, 4, 10), persistido.DataPrevista);
        Assert.Equal("PARCIAL", persistido.Status);
        Assert.Equal(1000m, persistido.ValorTotal);
    }

    [Fact]
    public async Task Editar_EmprestimoPendenteAlterandoPessoa_Retorna200ComNovaPessoa()
    {
        await _fixture.ClearAsync();

        var emprestimo = await RegistrarEmprestimoAsync("Joao Silva", 500m);

        var request = new EditarContaReceberRequest
        {
            Descricao = "Emprestimo editavel",
            Pessoa = "Maria Souza",
            ValorTotal = 500m
        };

        var response = await _fixture.Client.PutAsync($"/api/contas-receber/{emprestimo.Id}", ComoJson(request));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var persistido = await ObterAsync(emprestimo.Id);
        Assert.Equal("Maria Souza", persistido.Pessoa);
        Assert.Equal(500m, persistido.ValorTotal);
    }

    #endregion

    #region PUT /api/contas-receber/{id} — ocorrencia de recebivel recorrente

    [Fact]
    public async Task Editar_OcorrenciaDeRecebivelRecorrente_Retorna409ENaoAlteraNada()
    {
        await _fixture.ClearAsync();

        var recorrenteId = Guid.NewGuid();
        var ocorrencia = new ContaReceber
        {
            Id = Guid.NewGuid(),
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Ocorrencia recorrente",
            ValorTotal = 300m,
            DataRegistro = new DateOnly(2026, 1, 1),
            DataPrevista = new DateOnly(2026, 2, 5),
            Status = StatusContaReceber.Pendente,
            RecebivelRecorrenteId = recorrenteId
        };
        await _fixture.AddContaReceberAsync(ocorrencia);

        var request = new EditarContaReceberRequest
        {
            Descricao = "Tentativa de edicao",
            DataPrevista = new DateOnly(2026, 6, 1),
            ValorTotal = 999m
        };

        var response = await _fixture.Client.PutAsync($"/api/contas-receber/{ocorrencia.Id}", ComoJson(request));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var persistido = await _fixture.GetContaReceberByIdAsync(ocorrencia.Id);
        Assert.NotNull(persistido);
        Assert.Equal("Ocorrencia recorrente", persistido.Descricao);
        Assert.Equal(300m, persistido.ValorTotal);
        Assert.Equal(new DateOnly(2026, 2, 5), persistido.DataPrevista);
    }

    #endregion

    #region PUT /api/contas-receber/{id} — inexistente e validacao

    [Fact]
    public async Task Editar_IdInexistente_Retorna404()
    {
        await _fixture.ClearAsync();

        var idInexistente = Guid.NewGuid();
        var request = new EditarContaReceberRequest
        {
            Descricao = "Qualquer",
            ValorTotal = 100m
        };

        var response = await _fixture.Client.PutAsync($"/api/contas-receber/{idInexistente}", ComoJson(request));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Editar_DescricaoVazia_Retorna400ENaoAlteraNada()
    {
        await _fixture.ClearAsync();

        var recebivel = await RegistrarRecebivelAsync("Venda original", 1000m);

        var request = new EditarContaReceberRequest
        {
            Descricao = "",
            ValorTotal = 1500m
        };

        var response = await _fixture.Client.PutAsync($"/api/contas-receber/{recebivel.Id}", ComoJson(request));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var persistido = await ObterAsync(recebivel.Id);
        Assert.Equal("Venda original", persistido.Descricao);
        Assert.Equal(1000m, persistido.ValorTotal);
    }

    #endregion
}
