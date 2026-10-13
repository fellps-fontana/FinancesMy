using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using Xunit;

namespace MyFinances.Tests.Controllers;

// Regra-de-negocio item 18: PUT/DELETE de lancamento nao avulso -> 409 com
// origemVinculo; avulso edita (inclusive trocar conta) e exclui. GET fluxo-caixa
// expoe Avulso, OrigemVinculo e campos de parcela (item 17).
public class LancamentosAvulsoWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public LancamentosAvulsoWebApplicationFactory(string databaseName)
    {
        _databaseName = databaseName;
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
                    options.UseInMemoryDatabase(_databaseName));
            });
    }
}

public class LancamentosAvulsoTestsFixture : IAsyncLifetime
{
    private readonly LancamentosAvulsoWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public LancamentosAvulsoTestsFixture()
    {
        _factory = new LancamentosAvulsoWebApplicationFactory($"LancamentosAvulsoTestDb-{Guid.NewGuid():N}");
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
            Username = $"lancamentos_avulso_test_{Guid.NewGuid():N}",
            Email = $"lancamentos_avulso_test_{Guid.NewGuid():N}@example.com",
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

    public async Task AddCompraParceladaAsync(CompraParcelada compra)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Set<CompraParcelada>().Add(compra);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Lancamento?> GetLancamentoByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Lancamentos.FirstOrDefaultAsync(l => l.Id == id);
    }
}

[CollectionDefinition("Lancamentos Avulso Controller Collection")]
public class LancamentosAvulsoControllerCollection : ICollectionFixture<LancamentosAvulsoTestsFixture>
{
}

[Collection("Lancamentos Avulso Controller Collection")]
public class LancamentosAvulsoControllerTests
{
    private readonly LancamentosAvulsoTestsFixture _fixture;

    public LancamentosAvulsoControllerTests(LancamentosAvulsoTestsFixture fixture)
    {
        _fixture = fixture;
    }

    // ---------- Helpers ----------

    private static Conta CriarContaManual(Guid id, TipoConta tipo = TipoConta.Banco)
    {
        return new Conta
        {
            Id = id,
            Nome = "Conta " + id.ToString()[..4],
            Tipo = tipo,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };
    }

    private static Lancamento CriarLancamentoAvulso(Guid contaId, string descricao = "Almoco")
    {
        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Descricao = descricao,
            Valor = 50m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 3, 10),
            Status = StatusLancamento.Pendente,
            Manual = true
        };
    }

    // Monta lancamento com o vinculo nomeado (string vazia = nenhum vinculo).
    private static Lancamento CriarLancamentoComVinculo(Guid contaId, string vinculo, bool manual)
    {
        var lancamento = CriarLancamentoAvulso(contaId);
        lancamento.Manual = manual;

        switch (vinculo)
        {
            case "":
                break;
            case "ContaFixaId":
                lancamento.ContaFixaId = Guid.NewGuid();
                break;
            case "CompraParceladaId":
                lancamento.CompraParceladaId = Guid.NewGuid();
                break;
            case "FaturaId":
                lancamento.FaturaId = Guid.NewGuid();
                break;
            case "AssinaturaCartaoId":
                lancamento.AssinaturaCartaoId = Guid.NewGuid();
                break;
            case "ContaReceberId":
                lancamento.ContaReceberId = Guid.NewGuid();
                break;
            case "TransferenciaId":
                lancamento.TransferenciaId = Guid.NewGuid();
                break;
            case "ConciliadoCom":
                lancamento.ConciliadoCom = Guid.NewGuid();
                break;
            default:
                throw new ArgumentException($"Vinculo desconhecido no teste: {vinculo}", nameof(vinculo));
        }

        return lancamento;
    }

    private static EditarLancamentoRequest CriarRequestEdicao(Guid? contaDestinoId = null)
    {
        return new EditarLancamentoRequest
        {
            Descricao = "Almoco editado",
            Valor = 75m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 3, 12),
            Status = "PAGO",
            ContaId = contaDestinoId
        };
    }

    private static async Task<(string? Erro, string? OrigemVinculo)> LerConflito(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        return (root.GetProperty("erro").GetString(), root.GetProperty("origemVinculo").GetString());
    }

    private static string UrlLancamento(Guid contaId, Guid lancamentoId)
    {
        return $"/api/contas/{contaId}/lancamentos/{lancamentoId}";
    }

    // ---------- PUT nao avulso -> 409 ----------

    [Theory]
    [InlineData("ContaFixaId", true, "CONTA_FIXA")]
    [InlineData("CompraParceladaId", true, "COMPRA_PARCELADA")]
    [InlineData("FaturaId", true, "CARTAO")]
    [InlineData("AssinaturaCartaoId", true, "CARTAO")]
    [InlineData("ContaReceberId", true, "CONTA_RECEBER")]
    [InlineData("TransferenciaId", true, "TRANSFERENCIA")]
    [InlineData("ConciliadoCom", true, "CONCILIADO")]
    [InlineData("", false, "NAO_MANUAL")]
    public async Task EditarLancamento_NaoAvulso_Retorna409ComOrigemVinculoEMantemDados(
        string vinculo,
        bool manual,
        string origemEsperada)
    {
        var contaId = Guid.NewGuid();
        var lancamento = CriarLancamentoComVinculo(contaId, vinculo, manual);

        await _fixture.AddContaAsync(CriarContaManual(contaId));
        await _fixture.AddLancamentoAsync(lancamento);

        var response = await _fixture.Client.PutAsJsonAsync(
            UrlLancamento(contaId, lancamento.Id),
            CriarRequestEdicao());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var (erro, origem) = await LerConflito(response);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        Assert.Equal(origemEsperada, origem);

        var noBanco = await _fixture.GetLancamentoByIdAsync(lancamento.Id);
        Assert.NotNull(noBanco);
        Assert.Equal("Almoco", noBanco!.Descricao);
        Assert.Equal(50m, noBanco.Valor);
    }

    // ---------- DELETE nao avulso -> 409 ----------

    [Theory]
    [InlineData("ContaFixaId", true, "CONTA_FIXA")]
    [InlineData("CompraParceladaId", true, "COMPRA_PARCELADA")]
    [InlineData("FaturaId", true, "CARTAO")]
    [InlineData("AssinaturaCartaoId", true, "CARTAO")]
    [InlineData("ContaReceberId", true, "CONTA_RECEBER")]
    [InlineData("TransferenciaId", true, "TRANSFERENCIA")]
    [InlineData("ConciliadoCom", true, "CONCILIADO")]
    [InlineData("", false, "NAO_MANUAL")]
    public async Task RemoverLancamento_NaoAvulso_Retorna409ComOrigemVinculoEMantemNoBanco(
        string vinculo,
        bool manual,
        string origemEsperada)
    {
        var contaId = Guid.NewGuid();
        var lancamento = CriarLancamentoComVinculo(contaId, vinculo, manual);

        await _fixture.AddContaAsync(CriarContaManual(contaId));
        await _fixture.AddLancamentoAsync(lancamento);

        var response = await _fixture.Client.DeleteAsync(UrlLancamento(contaId, lancamento.Id));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var (erro, origem) = await LerConflito(response);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        Assert.Equal(origemEsperada, origem);

        Assert.NotNull(await _fixture.GetLancamentoByIdAsync(lancamento.Id));
    }

    // ---------- PUT avulso ----------

    [Fact]
    public async Task EditarLancamento_Avulso_Retorna200EAtualizaCampos()
    {
        var contaId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId);

        await _fixture.AddContaAsync(CriarContaManual(contaId));
        await _fixture.AddLancamentoAsync(lancamento);

        var response = await _fixture.Client.PutAsJsonAsync(
            UrlLancamento(contaId, lancamento.Id),
            CriarRequestEdicao());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<LancamentoResponseDto>(body, LancamentosAvulsoTestsFixture.JsonOptions);
        Assert.NotNull(dto);
        Assert.True(dto!.Avulso);
        Assert.Equal("AVULSO", dto.OrigemVinculo);
        Assert.Equal("Almoco editado", dto.Descricao);

        var noBanco = await _fixture.GetLancamentoByIdAsync(lancamento.Id);
        Assert.NotNull(noBanco);
        Assert.Equal("Almoco editado", noBanco!.Descricao);
        Assert.Equal(75m, noBanco.Valor);
        Assert.Equal(new DateOnly(2025, 3, 12), noBanco.Data);
        Assert.Equal(StatusLancamento.Pago, noBanco.Status);
    }

    [Fact]
    public async Task EditarLancamento_AvulsoTrocandoDeConta_Retorna200EMudaParaContaDestino()
    {
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaOrigemId);

        await _fixture.AddContaAsync(CriarContaManual(contaOrigemId));
        await _fixture.AddContaAsync(CriarContaManual(contaDestinoId));
        await _fixture.AddLancamentoAsync(lancamento);

        var response = await _fixture.Client.PutAsJsonAsync(
            UrlLancamento(contaOrigemId, lancamento.Id),
            CriarRequestEdicao(contaDestinoId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<LancamentoResponseDto>(body, LancamentosAvulsoTestsFixture.JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(contaDestinoId, dto!.ContaId);

        var noBanco = await _fixture.GetLancamentoByIdAsync(lancamento.Id);
        Assert.NotNull(noBanco);
        Assert.Equal(contaDestinoId, noBanco!.ContaId);
    }

    [Fact]
    public async Task EditarLancamento_AvulsoTrocandoParaContaCartao_Retorna400ESemMudarConta()
    {
        var contaOrigemId = Guid.NewGuid();
        var contaCartaoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaOrigemId);

        await _fixture.AddContaAsync(CriarContaManual(contaOrigemId));
        await _fixture.AddContaAsync(CriarContaManual(contaCartaoId, TipoConta.Cartao));
        await _fixture.AddLancamentoAsync(lancamento);

        var response = await _fixture.Client.PutAsJsonAsync(
            UrlLancamento(contaOrigemId, lancamento.Id),
            CriarRequestEdicao(contaCartaoId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var noBanco = await _fixture.GetLancamentoByIdAsync(lancamento.Id);
        Assert.NotNull(noBanco);
        Assert.Equal(contaOrigemId, noBanco!.ContaId);
    }

    // ---------- DELETE avulso ----------

    [Fact]
    public async Task RemoverLancamento_Avulso_Retorna200ERemoveDoBanco()
    {
        var contaId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId);

        await _fixture.AddContaAsync(CriarContaManual(contaId));
        await _fixture.AddLancamentoAsync(lancamento);

        var response = await _fixture.Client.DeleteAsync(UrlLancamento(contaId, lancamento.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(await _fixture.GetLancamentoByIdAsync(lancamento.Id));
    }

    // ---------- GET fluxo-caixa ----------

    [Fact]
    public async Task ListarFluxoCaixa_ComAvulsoContaFixaEParcela_ExpoeOrigemEDadosDeParcela()
    {
        var contaId = Guid.NewGuid();
        var compraId = Guid.NewGuid();

        await _fixture.AddContaAsync(CriarContaManual(contaId));
        await _fixture.AddCompraParceladaAsync(new CompraParcelada
        {
            Id = compraId,
            Descricao = "Notebook",
            ValorTotal = 3000m,
            QuantidadeParcelas = 10,
            DataCompra = new DateOnly(2025, 1, 10)
        });

        var avulso = CriarLancamentoAvulso(contaId, "Avulso");
        var contaFixa = CriarLancamentoComVinculo(contaId, "ContaFixaId", true);
        contaFixa.Descricao = "Conta fixa";
        var parcela = CriarLancamentoComVinculo(contaId, "", true);
        parcela.Descricao = "Notebook 3/10";
        parcela.CompraParceladaId = compraId;
        parcela.ParcelaNumero = 3;

        await _fixture.AddLancamentoAsync(avulso);
        await _fixture.AddLancamentoAsync(contaFixa);
        await _fixture.AddLancamentoAsync(parcela);

        var response = await _fixture.Client.GetAsync($"/api/contas/{contaId}/lancamentos/fluxo-caixa");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var lista = JsonSerializer.Deserialize<List<LancamentoResponseDto>>(body, LancamentosAvulsoTestsFixture.JsonOptions);
        Assert.NotNull(lista);
        Assert.Equal(3, lista!.Count);

        var dtoAvulso = lista.Single(l => l.Id == avulso.Id);
        Assert.True(dtoAvulso.Avulso);
        Assert.Equal("AVULSO", dtoAvulso.OrigemVinculo);

        var dtoFixa = lista.Single(l => l.Id == contaFixa.Id);
        Assert.False(dtoFixa.Avulso);
        Assert.Equal("CONTA_FIXA", dtoFixa.OrigemVinculo);

        var dtoParcela = lista.Single(l => l.Id == parcela.Id);
        Assert.False(dtoParcela.Avulso);
        Assert.Equal("COMPRA_PARCELADA", dtoParcela.OrigemVinculo);
        Assert.Equal(compraId, dtoParcela.CompraParceladaId);
        Assert.Equal(3, dtoParcela.ParcelaNumero);
        Assert.Equal(10, dtoParcela.TotalParcelas);
    }
}
