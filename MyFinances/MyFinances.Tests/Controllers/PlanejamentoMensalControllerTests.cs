using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.DTOs.PlanejamentoMensal;
using MyFinances.Tests.Helpers;
using Xunit;

namespace MyFinances.Tests.Controllers;

// TASK-253 / item 20 e item 14: testes HTTP end-to-end dos endpoints do planejamento mensal.
// Relogio controlado (RelogioFake) fixado em 2026-10-14: mes corrente = outubro, passado = setembro,
// futuro = novembro. Banco InMemory com nome proprio.
public class PlanejamentoMensalControllerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly RelogioFake _relogio;

    public PlanejamentoMensalControllerWebApplicationFactory(RelogioFake relogio)
    {
        _relogio = relogio;
    }

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
                options.UseInMemoryDatabase("PlanejamentoMensalControllerTestDb"));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(_relogio);
        });
    }
}

public class PlanejamentoMensalControllerTestsFixture : IAsyncLifetime
{
    public static readonly DateOnly Hoje = new(2026, 10, 14);

    private readonly RelogioFake _relogio;
    private readonly PlanejamentoMensalControllerWebApplicationFactory _factory;

    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public PlanejamentoMensalControllerTestsFixture()
    {
        _relogio = new RelogioFake(Hoje);
        _factory = new PlanejamentoMensalControllerWebApplicationFactory(_relogio);
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
            Username = $"planejamento_test_{Guid.NewGuid():N}",
            Email = $"planejamento_test_{Guid.NewGuid():N}@example.com",
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

    public async Task AddEntidadeAsync<T>(T entidade) where T : class
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Set<T>().Add(entidade);
        await dbContext.SaveChangesAsync();
    }

    public async Task<int> ContarPlanosAsync(int ano, int mes)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.PlanosMensais.CountAsync(p => p.Ano == ano && p.Mes == mes);
    }

    public async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.RemoveRange(await dbContext.Lancamentos.ToListAsync());
        dbContext.PlanoMensalSublimites.RemoveRange(await dbContext.PlanoMensalSublimites.ToListAsync());
        dbContext.PlanosMensais.RemoveRange(await dbContext.PlanosMensais.ToListAsync());
        dbContext.AssinaturasCartao.RemoveRange(await dbContext.AssinaturasCartao.ToListAsync());
        dbContext.ContasFixas.RemoveRange(await dbContext.ContasFixas.ToListAsync());
        dbContext.Categorias.RemoveRange(await dbContext.Categorias.ToListAsync());
        dbContext.Contas.RemoveRange(await dbContext.Contas.ToListAsync());
        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition("PlanejamentoMensal Controller Collection")]
public class PlanejamentoMensalControllerCollection : ICollectionFixture<PlanejamentoMensalControllerTestsFixture>
{
}

[Collection("PlanejamentoMensal Controller Collection")]
public class PlanejamentoMensalControllerTests
{
    private readonly PlanejamentoMensalControllerTestsFixture _fixture;

    // Mes corrente, passado e futuro derivados do relogio controlado (nao da data real).
    private static DateOnly HojeDoRelogio => PlanejamentoMensalControllerTestsFixture.Hoje;
    private static int AnoAtual => HojeDoRelogio.Year;
    private static int MesAtual => HojeDoRelogio.Month;
    private static DateOnly MesPassadoData => HojeDoRelogio.AddMonths(-1);
    private static DateOnly MesFuturoData => HojeDoRelogio.AddMonths(1);

    public PlanejamentoMensalControllerTests(PlanejamentoMensalControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region Cenario e helpers

    private sealed record Cenario(
        Guid Alimentacao,
        Guid Subcategoria,
        Guid Transporte,
        Guid Lazer,
        Guid Salario,
        Guid Arquivada);

    private async Task<Cenario> CriarCenarioAsync()
    {
        var alimentacao = await CriarCategoriaAsync("Alimentacao", TipoCategoria.Despesa);
        var subcategoria = await CriarCategoriaAsync("Restaurantes", TipoCategoria.Despesa, alimentacao);
        var transporte = await CriarCategoriaAsync("Transporte", TipoCategoria.Despesa);
        var lazer = await CriarCategoriaAsync("Lazer", TipoCategoria.Despesa);
        var salario = await CriarCategoriaAsync("Salario", TipoCategoria.Receita);
        var arquivada = await CriarCategoriaAsync("Antiga", TipoCategoria.Despesa, arquivada: true);

        return new Cenario(alimentacao, subcategoria, transporte, lazer, salario, arquivada);
    }

    private async Task<Guid> CriarCategoriaAsync(
        string nome,
        TipoCategoria tipo,
        Guid? parentId = null,
        bool arquivada = false)
    {
        var id = Guid.NewGuid();
        await _fixture.AddEntidadeAsync(new Categoria
        {
            Id = id,
            Nome = nome,
            Tipo = tipo,
            ParentId = parentId,
            Arquivada = arquivada
        });
        return id;
    }

    private async Task<Conta> CriarContaAsync(TipoConta tipo = TipoConta.Banco)
    {
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = $"Conta {tipo}",
            Tipo = tipo,
            Origem = OrigemConta.Manual,
            SaldoManual = 0m,
            Ativa = true
        };
        await _fixture.AddEntidadeAsync(conta);
        return conta;
    }

    private async Task AddLancamentoDebitoAsync(
        Conta conta,
        decimal valor,
        Guid? categoriaId,
        DateOnly data,
        Guid? contaFixaId = null,
        Guid? assinaturaCartaoId = null)
    {
        await _fixture.AddEntidadeAsync(new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            CategoriaId = categoriaId,
            Valor = valor,
            Tipo = TipoLancamento.Debit,
            Data = data,
            Status = StatusLancamento.Pago,
            Manual = true,
            Oculto = false,
            ContaFixaId = contaFixaId,
            AssinaturaCartaoId = assinaturaCartaoId
        });
    }

    private async Task<(HttpStatusCode Status, string Body)> GetPlanoAsync(int ano, int mes)
    {
        var response = await _fixture.Client.GetAsync($"/api/planejamento-mensal/{ano}/{mes}");
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpResponseMessage> PutPlanoAsync(int ano, int mes, object corpo) =>
        await _fixture.Client.PutAsJsonAsync($"/api/planejamento-mensal/{ano}/{mes}", corpo);

    private static PlanoMensalResponse? Desserializar(string body) =>
        JsonSerializer.Deserialize<PlanoMensalResponse>(body, PlanejamentoMensalControllerTestsFixture.JsonOptions);

    private static object CorpoValido(decimal renda, decimal percentualInvestimento, params (Guid Id, decimal Pct)[] sublimites) =>
        new
        {
            rendaPrevista = renda,
            percentualInvestimento,
            sublimites = sublimites.Select(s => new { categoriaId = s.Id, percentual = s.Pct }).ToList()
        };

    // Verifica que a violacao nao persistiu nada: GET antes e depois devem ser identicos.
    private async Task AssertViolacaoNaoPersisteAsync(int ano, int mes, object corpoInvalido, HttpStatusCode esperado)
    {
        var antes = await GetPlanoAsync(ano, mes);

        var response = await PutPlanoAsync(ano, mes, corpoInvalido);
        Assert.Equal(esperado, response.StatusCode);

        var depois = await GetPlanoAsync(ano, mes);
        Assert.Equal(antes.Status, depois.Status);
        Assert.Equal(antes.Body, depois.Body);
    }

    #endregion

    #region GET /api/planejamento-mensal/{ano}/{mes}

    [Fact]
    public async Task Obter_MesCorrenteSemPlano_Retorna200ComPlanoGerado()
    {
        await _fixture.ClearAsync();

        var (status, body) = await GetPlanoAsync(AnoAtual, MesAtual);

        Assert.Equal(HttpStatusCode.OK, status);
        var plano = Desserializar(body);
        Assert.NotNull(plano);
        Assert.Equal(AnoAtual, plano.Ano);
        Assert.Equal(MesAtual, plano.Mes);
        Assert.False(plano.SomenteLeitura);
        Assert.Equal(1, await _fixture.ContarPlanosAsync(AnoAtual, MesAtual));
    }

    [Fact]
    public async Task Obter_MesCorrenteSegundaChamada_EIdempotenteNaoDuplicaPlano()
    {
        await _fixture.ClearAsync();

        var primeira = await GetPlanoAsync(AnoAtual, MesAtual);
        var segunda = await GetPlanoAsync(AnoAtual, MesAtual);

        Assert.Equal(HttpStatusCode.OK, primeira.Status);
        Assert.Equal(HttpStatusCode.OK, segunda.Status);
        Assert.Equal(primeira.Body, segunda.Body);
        Assert.Equal(1, await _fixture.ContarPlanosAsync(AnoAtual, MesAtual));
    }

    [Fact]
    public async Task Obter_MesFuturoSemPlano_Retorna200ComPlanoGerado()
    {
        await _fixture.ClearAsync();

        var (status, body) = await GetPlanoAsync(MesFuturoData.Year, MesFuturoData.Month);

        Assert.Equal(HttpStatusCode.OK, status);
        var plano = Desserializar(body);
        Assert.NotNull(plano);
        Assert.Equal(MesFuturoData.Month, plano.Mes);
        Assert.False(plano.SomenteLeitura);
        Assert.Equal(1, await _fixture.ContarPlanosAsync(MesFuturoData.Year, MesFuturoData.Month));
    }

    [Fact]
    public async Task Obter_MesPassadoSemPlano_Retorna404SemCriarPlano()
    {
        await _fixture.ClearAsync();

        var (status, _) = await GetPlanoAsync(MesPassadoData.Year, MesPassadoData.Month);

        Assert.Equal(HttpStatusCode.NotFound, status);
        Assert.Equal(0, await _fixture.ContarPlanosAsync(MesPassadoData.Year, MesPassadoData.Month));
    }

    [Fact]
    public async Task Obter_MesPassadoComPlano_Retorna200SomenteLeitura()
    {
        await _fixture.ClearAsync();
        await _fixture.AddEntidadeAsync(new PlanoMensal
        {
            Id = Guid.NewGuid(),
            Ano = MesPassadoData.Year,
            Mes = MesPassadoData.Month,
            RendaPrevista = 1000m,
            PercentualInvestimento = 0m
        });

        var (status, body) = await GetPlanoAsync(MesPassadoData.Year, MesPassadoData.Month);

        Assert.Equal(HttpStatusCode.OK, status);
        var plano = Desserializar(body);
        Assert.NotNull(plano);
        Assert.True(plano.SomenteLeitura);
    }

    [Fact]
    public async Task Obter_AnoDez_Mil_Retorna400()
    {
        await _fixture.ClearAsync();

        var (status, _) = await GetPlanoAsync(10000, MesAtual);

        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task Obter_Mes13_Retorna400()
    {
        await _fixture.ClearAsync();

        var (status, _) = await GetPlanoAsync(AnoAtual, 13);

        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task Obter_Mes0_Retorna400()
    {
        await _fixture.ClearAsync();

        var (status, _) = await GetPlanoAsync(AnoAtual, 0);

        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    #endregion

    #region PUT /api/planejamento-mensal/{ano}/{mes}

    [Fact]
    public async Task Salvar_PlanoValidoMesCorrente_PersisteEGetSeguinteReflete()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await GetPlanoAsync(AnoAtual, MesAtual);

        var response = await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(
            1500m,
            20m,
            (cenario.Alimentacao, 40m),
            (cenario.Transporte, 10m)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var (status, body) = await GetPlanoAsync(AnoAtual, MesAtual);
        Assert.Equal(HttpStatusCode.OK, status);
        var plano = Desserializar(body);
        Assert.NotNull(plano);
        Assert.Equal(1500m, plano.RendaPrevista);
        Assert.Equal(20m, plano.PercentualInvestimento);
        Assert.Equal(2, plano.Sublimites.Count);
        Assert.Equal(40m, plano.Sublimites.Single(s => s.CategoriaId == cenario.Alimentacao).Percentual);
        Assert.Equal(10m, plano.Sublimites.Single(s => s.CategoriaId == cenario.Transporte).Percentual);
    }

    [Fact]
    public async Task Salvar_MesFuturoSemPlano_CriaComValoresEnviadosSemCopiarPlanoAnterior()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();

        // Plano anterior (outubro) com sublimite: nao deve ser copiado para novembro no salvamento.
        var anterior = new PlanoMensal
        {
            Id = Guid.NewGuid(),
            Ano = AnoAtual,
            Mes = MesAtual,
            RendaPrevista = 1000m,
            PercentualInvestimento = 10m
        };
        anterior.Sublimites.Add(new PlanoMensalSublimite
        {
            Id = Guid.NewGuid(),
            PlanoMensalId = anterior.Id,
            CategoriaId = cenario.Alimentacao,
            Percentual = 50m
        });
        await _fixture.AddEntidadeAsync(anterior);

        var response = await PutPlanoAsync(MesFuturoData.Year, MesFuturoData.Month, CorpoValido(2000m, 5m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var (status, body) = await GetPlanoAsync(MesFuturoData.Year, MesFuturoData.Month);
        Assert.Equal(HttpStatusCode.OK, status);
        var plano = Desserializar(body);
        Assert.NotNull(plano);
        Assert.Equal(2000m, plano.RendaPrevista);
        Assert.Equal(5m, plano.PercentualInvestimento);
        Assert.Empty(plano.Sublimites);
    }

    [Fact]
    public async Task Salvar_CorpoSemCampoSublimites_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        // Decisao de 2026-10-14: sublimites obrigatorio; cliente manda [] para limpar.
        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            new { rendaPrevista = 1200m, percentualInvestimento = 10m },
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Salvar_CorpoComSublimitesNull_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            new { rendaPrevista = 1200m, percentualInvestimento = 10m, sublimites = (object?)null },
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Salvar_CorpoSemRendaPrevista_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            new { percentualInvestimento = 10m, sublimites = Array.Empty<object>() },
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Salvar_CorpoSemPercentualInvestimento_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            new { rendaPrevista = 1000m, sublimites = Array.Empty<object>() },
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Salvar_ItemDeSublimiteSemPercentual_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            new { rendaPrevista = 1000m, percentualInvestimento = 10m, sublimites = new[] { new { categoriaId = cenario.Transporte } } },
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Salvar_ItemDeSublimiteSemCategoriaId_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            new { rendaPrevista = 1000m, percentualInvestimento = 10m, sublimites = new[] { new { percentual = 20m } } },
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Salvar_ItemDeSublimiteNull_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        // Item null na lista nao pode derrubar o servidor com 500.
        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            new { rendaPrevista = 1000m, percentualInvestimento = 10m, sublimites = new object?[] { null } },
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Salvar_RendaAcimaDoTetoNumeric_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        // numeric(18,2) aceita ate 9.999.999.999.999.999,99; 10^16 estoura a coluna.
        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(10000000000000000m, 10m, (cenario.Alimentacao, 50m)),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Obter_MesCorrentePlus12_Retorna200()
    {
        await _fixture.ClearAsync();
        var limite = HojeDoRelogio.AddMonths(12);

        var (status, body) = await GetPlanoAsync(limite.Year, limite.Month);

        Assert.Equal(HttpStatusCode.OK, status);
        var plano = Desserializar(body);
        Assert.NotNull(plano);
        Assert.Equal(limite.Month, plano.Mes);
    }

    [Fact]
    public async Task Salvar_MesCorrentePlus12_Retorna200()
    {
        await _fixture.ClearAsync();
        var limite = HojeDoRelogio.AddMonths(12);

        var response = await PutPlanoAsync(limite.Year, limite.Month, CorpoValido(1000m, 10m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Obter_MesCorrentePlus13_Retorna400SemCriarPlano()
    {
        await _fixture.ClearAsync();
        var alem = HojeDoRelogio.AddMonths(13);

        var (status, _) = await GetPlanoAsync(alem.Year, alem.Month);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(0, await _fixture.ContarPlanosAsync(alem.Year, alem.Month));
    }

    [Fact]
    public async Task Salvar_MesCorrentePlus13_Retorna400SemCriarPlano()
    {
        await _fixture.ClearAsync();
        var alem = HojeDoRelogio.AddMonths(13);

        var response = await PutPlanoAsync(alem.Year, alem.Month, CorpoValido(1000m, 10m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await _fixture.ContarPlanosAsync(alem.Year, alem.Month));
    }

    [Fact]
    public async Task Salvar_RendaNegativa_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(-1m, 10m, (cenario.Alimentacao, 50m)),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Salvar_PercentualInvestimentoAcima100_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(1000m, 101m, (cenario.Alimentacao, 50m)),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Salvar_PercentualSublimiteAcima100_Retorna400SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(1000m, 10m, (cenario.Alimentacao, 101m)),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Salvar_MesInvalido13_Retorna400()
    {
        await _fixture.ClearAsync();

        var response = await PutPlanoAsync(AnoAtual, 13, CorpoValido(1000m, 10m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Salvar_CategoriaInexistente_Retorna404SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m), (Guid.NewGuid(), 10m)),
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Salvar_CategoriaReceita_Retorna422SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m), (cenario.Salario, 10m)),
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Salvar_CategoriaArquivada_Retorna422SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m), (cenario.Arquivada, 10m)),
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Salvar_SublimiteDuplicadoMesmaCategoria_Retorna422SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(1000m, 10m, (cenario.Alimentacao, 20m), (cenario.Alimentacao, 30m)),
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Salvar_SubcategoriaSemSublimiteNoPai_Retorna422SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(1000m, 10m, (cenario.Subcategoria, 20m)),
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Salvar_SomaPrimeiroNivelAcima100_Retorna422SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(1000m, 10m, (cenario.Alimentacao, 60m), (cenario.Transporte, 50m)),
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Salvar_SomaSubcategoriasDoMesmoPaiAcima100_Retorna422SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 10m, (cenario.Alimentacao, 50m)));

        // Duas subcategorias do mesmo pai somando 110: o pai tem 50, mas o total de filhos excede 100.
        var subOutra = await CriarCategoriaAsync("Delivery", TipoCategoria.Despesa, cenario.Alimentacao);

        await AssertViolacaoNaoPersisteAsync(AnoAtual, MesAtual,
            CorpoValido(1000m, 10m,
                (cenario.Alimentacao, 50m),
                (cenario.Subcategoria, 60m),
                (subOutra, 50m)),
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Salvar_MesPassado_Retorna409SemPersistir()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await _fixture.AddEntidadeAsync(new PlanoMensal
        {
            Id = Guid.NewGuid(),
            Ano = MesPassadoData.Year,
            Mes = MesPassadoData.Month,
            RendaPrevista = 1000m,
            PercentualInvestimento = 0m
        });

        await AssertViolacaoNaoPersisteAsync(MesPassadoData.Year, MesPassadoData.Month,
            CorpoValido(9999m, 50m, (cenario.Alimentacao, 50m)),
            HttpStatusCode.Conflict);
    }

    #endregion

    #region Calculo: fixas, sobra e gasto por sublimite (item 20 e item 14)

    [Fact]
    public async Task Obter_ContaFixaLancadaNoMes_ReduzSobra()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 0m));

        var antes = Desserializar((await GetPlanoAsync(AnoAtual, MesAtual)).Body);
        Assert.NotNull(antes);
        Assert.Equal(1000m, antes.Sobra);

        var conta = await CriarContaAsync();
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Descricao = "Aluguel",
            Valor = 300m,
            DiaVencimento = 5,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            Ativa = true
        };
        await _fixture.AddEntidadeAsync(contaFixa);
        await AddLancamentoDebitoAsync(conta, 300m, cenario.Lazer,
            new DateOnly(AnoAtual, MesAtual, 5), contaFixaId: contaFixa.Id);

        var depois = Desserializar((await GetPlanoAsync(AnoAtual, MesAtual)).Body);
        Assert.NotNull(depois);
        Assert.Equal(300m, depois.Fixas);
        Assert.Equal(700m, depois.Sobra);
    }

    [Fact]
    public async Task Obter_AssinaturaCartaoLancadaNoMes_ReduzSobra()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 0m));

        var antes = Desserializar((await GetPlanoAsync(AnoAtual, MesAtual)).Body);
        Assert.NotNull(antes);
        Assert.Equal(1000m, antes.Sobra);

        var cartao = await CriarContaAsync(TipoConta.Cartao);
        var assinatura = new AssinaturaCartao
        {
            Id = Guid.NewGuid(),
            ContaId = cartao.Id,
            Descricao = "Streaming",
            Valor = 40m,
            DiaReferencia = 10,
            Ativa = true
        };
        await _fixture.AddEntidadeAsync(assinatura);
        await AddLancamentoDebitoAsync(cartao, 40m, cenario.Lazer,
            new DateOnly(AnoAtual, MesAtual, 10), assinaturaCartaoId: assinatura.Id);

        var depois = Desserializar((await GetPlanoAsync(AnoAtual, MesAtual)).Body);
        Assert.NotNull(depois);
        Assert.Equal(40m, depois.Fixas);
        Assert.Equal(960m, depois.Sobra);
    }

    [Fact]
    public async Task Obter_GastoVariavelDaCategoriaComSublimite_ApareceNoSublimite()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        // renda 1000, sem fixas, sem investimento: sobra 1000; sublimite Alimentacao 50% = R$ 500.
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 0m, (cenario.Alimentacao, 50m)));

        var conta = await CriarContaAsync();
        await AddLancamentoDebitoAsync(conta, 120m, cenario.Alimentacao, new DateOnly(AnoAtual, MesAtual, 5));

        var plano = Desserializar((await GetPlanoAsync(AnoAtual, MesAtual)).Body);
        Assert.NotNull(plano);
        var sublimite = plano.Sublimites.Single(s => s.CategoriaId == cenario.Alimentacao);
        Assert.Equal(500m, sublimite.Valor);
        Assert.Equal(120m, sublimite.GastoRealizado);
        Assert.False(sublimite.Estourado);
        Assert.Equal(0m, plano.GastoSemSublimite);
    }

    [Fact]
    public async Task Obter_GastoVariavelSemSublimite_ApareceEmGastoSemSublimite()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 0m, (cenario.Alimentacao, 50m)));

        var conta = await CriarContaAsync();
        await AddLancamentoDebitoAsync(conta, 80m, cenario.Lazer, new DateOnly(AnoAtual, MesAtual, 5));
        await AddLancamentoDebitoAsync(conta, 20m, categoriaId: null, new DateOnly(AnoAtual, MesAtual, 6));

        var plano = Desserializar((await GetPlanoAsync(AnoAtual, MesAtual)).Body);
        Assert.NotNull(plano);
        Assert.Equal(100m, plano.GastoSemSublimite);
        Assert.Equal(0m, plano.Sublimites.Single(s => s.CategoriaId == cenario.Alimentacao).GastoRealizado);
    }

    [Fact]
    public async Task Obter_CategoriaDespesaSemSublimiteSalvo_RetornaGastosPorCategoriaComGastoVariavel()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(1000m, 0m, (cenario.Alimentacao, 50m)));

        var conta = await CriarContaAsync();
        await AddLancamentoDebitoAsync(conta, 150m, cenario.Lazer, new DateOnly(AnoAtual, MesAtual, 5));
        await AddLancamentoDebitoAsync(conta, 40m, cenario.Subcategoria, new DateOnly(AnoAtual, MesAtual, 6));

        var (status, body) = await GetPlanoAsync(AnoAtual, MesAtual);
        Assert.Equal(HttpStatusCode.OK, status);

        var plano = Desserializar(body);
        Assert.NotNull(plano);
        Assert.DoesNotContain(plano.Sublimites, s => s.CategoriaId == cenario.Lazer);
        Assert.Equal(150m, plano.GastosPorCategoria.Single(g => g.CategoriaId == cenario.Lazer).GastoRealizado);
        Assert.Equal(40m, plano.GastosPorCategoria.Single(g => g.CategoriaId == cenario.Alimentacao).GastoRealizado);
        Assert.Equal(40m, plano.GastosPorCategoria.Single(g => g.CategoriaId == cenario.Subcategoria).GastoRealizado);
        Assert.Equal(0m, plano.GastosPorCategoria.Single(g => g.CategoriaId == cenario.Transporte).GastoRealizado);
        Assert.DoesNotContain(plano.GastosPorCategoria, g => g.CategoriaId == cenario.Salario);
    }

    [Fact]
    public async Task Salvar_CategoriaDespesaSemSublimiteSalvo_RetornaGastosPorCategoriaComGastoVariavel()
    {
        await _fixture.ClearAsync();
        var cenario = await CriarCenarioAsync();
        var conta = await CriarContaAsync();
        await AddLancamentoDebitoAsync(conta, 210m, cenario.Lazer, new DateOnly(AnoAtual, MesAtual, 8));

        var response = await PutPlanoAsync(AnoAtual, MesAtual, CorpoValido(2000m, 10m, (cenario.Alimentacao, 30m)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var plano = Desserializar(await response.Content.ReadAsStringAsync());
        Assert.NotNull(plano);
        Assert.DoesNotContain(plano.Sublimites, s => s.CategoriaId == cenario.Lazer);
        Assert.Equal(210m, plano.GastosPorCategoria.Single(g => g.CategoriaId == cenario.Lazer).GastoRealizado);
        Assert.DoesNotContain(plano.GastosPorCategoria, g => g.CategoriaId == cenario.Salario);
    }

    #endregion
}
