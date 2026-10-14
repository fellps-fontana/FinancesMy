using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using Xunit;

namespace MyFinances.Tests.Controllers;

// Regra-de-negocio item 17 (lancamento parcelado em conta banco) e item 18
// (guarda de avulso em parcela), exercitadas via HTTP.
// Usa SQLite (nao o provider InMemory) porque LancamentoParceladoService abre
// transacao real via BeginTransactionAsync, que o InMemory nao suporta.
public class LancamentosParceladosWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public LancamentosParceladosWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Schema antes do host subir (ver CartaoComprasParceladasControllerTests).
        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var schemaContext = new MyFinancesDbContext(options);
        schemaContext.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("TEST_MODE", "true");

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
                options.UseSqlite(_connection));

            var seederDescriptor = services.FirstOrDefault(d => d.ServiceType.Name == "DevUserSeeder");
            if (seederDescriptor != null)
            {
                services.Remove(seederDescriptor);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

public class LancamentosParceladosTestsFixture : IAsyncLifetime
{
    private LancamentosParceladosWebApplicationFactory _factory = null!;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task InitializeAsync()
    {
        _factory = new LancamentosParceladosWebApplicationFactory();
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
            Username = $"parcelado_test_{Guid.NewGuid():N}",
            Email = $"parcelado_test_{Guid.NewGuid():N}@example.com",
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

    public async Task AddFaturaAsync(Fatura fatura)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Faturas.Add(fatura);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddCompraParceladaAsync(CompraParcelada compra)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.ComprasParceladas.Add(compra);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddLancamentoAsync(Lancamento lancamento)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.Add(lancamento);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Lancamento?> GetLancamentoByIdAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Lancamentos.FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<int> ContarLancamentosDaCompraAsync(Guid compraParceladaId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        return await dbContext.Lancamentos.CountAsync(l => l.CompraParceladaId == compraParceladaId);
    }
}

public class LancamentosParceladosControllerTests : IAsyncLifetime
{
    private LancamentosParceladosTestsFixture _fixture = null!;

    public async Task InitializeAsync()
    {
        _fixture = new LancamentosParceladosTestsFixture();
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    // ---------- Helpers ----------

    private static Conta CriarContaBanco(Guid id)
    {
        return new Conta
        {
            Id = id,
            Nome = "Conta banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 1000m,
            Ativa = true
        };
    }

    private static Conta CriarContaCartao(Guid id)
    {
        return new Conta
        {
            Id = id,
            Nome = "Cartao",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        };
    }

    private static CriarLancamentoParceladoRequest CriarRequest(
        int quantidade = 3,
        string status = "PENDENTE",
        Guid? categoriaId = null)
    {
        return new CriarLancamentoParceladoRequest
        {
            Descricao = "Notebook",
            ValorTotal = 100m,
            QuantidadeParcelas = quantidade,
            DataPrimeiraParcela = new DateOnly(2026, 1, 15),
            CategoriaId = categoriaId,
            Status = status
        };
    }

    private static string UrlParcelados(Guid contaId) => $"/api/contas/{contaId}/lancamentos-parcelados";

    private static string UrlCompra(Guid contaId, Guid compraId) => $"{UrlParcelados(contaId)}/{compraId}";

    private static string UrlLancamento(Guid contaId, Guid lancamentoId) =>
        $"/api/contas/{contaId}/lancamentos/{lancamentoId}";

    private static string UrlFluxo(Guid contaId) => $"/api/contas/{contaId}/lancamentos/fluxo-caixa";

    private async Task<LancamentoParceladoResponse> CriarComSucessoAsync(Guid contaId, CriarLancamentoParceladoRequest request)
    {
        var response = await _fixture.Client.PostAsJsonAsync(UrlParcelados(contaId), request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var resposta = JsonSerializer.Deserialize<LancamentoParceladoResponse>(body, LancamentosParceladosTestsFixture.JsonOptions);
        Assert.NotNull(resposta);
        return resposta!;
    }

    private async Task<List<LancamentoResponseDto>> ListarFluxoAsync(Guid contaId)
    {
        var response = await _fixture.Client.GetAsync(UrlFluxo(contaId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var lista = JsonSerializer.Deserialize<List<LancamentoResponseDto>>(body, LancamentosParceladosTestsFixture.JsonOptions);
        Assert.NotNull(lista);
        return lista!;
    }

    private static async Task<(int Excluidas, int Mantidas)> LerCancelamentoAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        return (root.GetProperty("excluidas").GetInt32(), root.GetProperty("mantidas").GetInt32());
    }

    // ---------- POST: criacao com sucesso ----------

    [Fact]
    public async Task Criar_TresParcelas_Retorna201ComTresParcelasNumeradasEValoresDivididos()
    {
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));

        var resposta = await CriarComSucessoAsync(contaId, CriarRequest(quantidade: 3));

        Assert.NotEqual(Guid.Empty, resposta.CompraParceladaId);
        Assert.Equal(3, resposta.Parcelas.Count);
        Assert.Equal(new int?[] { 1, 2, 3 }, resposta.Parcelas.Select(p => p.ParcelaNumero));
        Assert.All(resposta.Parcelas, p => Assert.Equal(3, p.TotalParcelas));
        Assert.All(resposta.Parcelas, p => Assert.Equal(resposta.CompraParceladaId, p.CompraParceladaId));
        Assert.Equal(new[] { 33.33m, 33.33m, 33.34m }, resposta.Parcelas.Select(p => p.Valor));
        Assert.Equal(
            new[] { new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 15), new DateOnly(2026, 3, 15) },
            resposta.Parcelas.Select(p => p.Data));
    }

    [Fact]
    public async Task Criar_StatusPendente_TodasAsParcelasNascemPendentesEAvulsoFalso()
    {
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));

        var resposta = await CriarComSucessoAsync(contaId, CriarRequest(status: "PENDENTE"));

        Assert.All(resposta.Parcelas, p => Assert.Equal("PENDENTE", p.Status));
        Assert.All(resposta.Parcelas, p => Assert.False(p.Avulso));
        Assert.All(resposta.Parcelas, p => Assert.Equal("COMPRA_PARCELADA", p.OrigemVinculo));
    }

    [Fact]
    public async Task Criar_StatusPagoNaPrimeira_SoPrimeiraParcelaNascePaga()
    {
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));

        var resposta = await CriarComSucessoAsync(contaId, CriarRequest(quantidade: 3, status: "PAGO"));

        Assert.Equal("PAGO", resposta.Parcelas.Single(p => p.ParcelaNumero == 1).Status);
        Assert.Equal(2, resposta.Parcelas.Count(p => p.Status == "PENDENTE"));
    }

    [Fact]
    public async Task Criar_TresParcelas_ParcelasAparecemNoFluxoCaixaComNumeroETotal()
    {
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));

        var resposta = await CriarComSucessoAsync(contaId, CriarRequest(quantidade: 3, status: "PAGO"));

        var fluxo = await ListarFluxoAsync(contaId);

        Assert.Equal(3, fluxo.Count);
        Assert.All(fluxo, l => Assert.Equal(resposta.CompraParceladaId, l.CompraParceladaId));
        Assert.All(fluxo, l => Assert.Equal(3, l.TotalParcelas));
        Assert.All(fluxo, l => Assert.Equal("COMPRA_PARCELADA", l.OrigemVinculo));
        Assert.Equal(new int?[] { 1, 2, 3 }, fluxo.OrderBy(l => l.ParcelaNumero).Select(l => l.ParcelaNumero));
        Assert.Equal("PAGO", fluxo.Single(l => l.ParcelaNumero == 1).Status);
        Assert.Equal("PENDENTE", fluxo.Single(l => l.ParcelaNumero == 2).Status);
    }

    // ---------- POST: validacoes -> 400 e nada gravado ----------

    [Fact]
    public async Task Criar_QuantidadeUm_Retorna400ENaoGravaNada()
    {
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));

        var response = await _fixture.Client.PostAsJsonAsync(UrlParcelados(contaId), CriarRequest(quantidade: 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListarFluxoAsync(contaId));
    }

    [Fact]
    public async Task Criar_ContaCartao_Retorna400ENaoGravaNada()
    {
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaCartao(contaId));

        var response = await _fixture.Client.PostAsJsonAsync(UrlParcelados(contaId), CriarRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListarFluxoAsync(contaId));
    }

    [Fact]
    public async Task Criar_ContaInexistente_Retorna400ENaoGravaNada()
    {
        var contaIdInexistente = Guid.NewGuid();

        var response = await _fixture.Client.PostAsJsonAsync(UrlParcelados(contaIdInexistente), CriarRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Criar_CategoriaDeReceita_Retorna400ENaoGravaNada()
    {
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));
        await _fixture.AddCategoriaAsync(new Categoria
        {
            Id = categoriaId,
            Nome = "Salario",
            Tipo = TipoCategoria.Receita
        });

        var response = await _fixture.Client.PostAsJsonAsync(
            UrlParcelados(contaId),
            CriarRequest(categoriaId: categoriaId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListarFluxoAsync(contaId));
    }

    // ---------- DELETE compra parcelada: cancela so Pendente ----------

    [Fact]
    public async Task Cancelar_UmPagoEDoisPendentes_Retorna200ComExcluidasEMantidasEPagoPermanece()
    {
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));
        var resposta = await CriarComSucessoAsync(contaId, CriarRequest(quantidade: 3, status: "PAGO"));

        var response = await _fixture.Client.DeleteAsync(UrlCompra(contaId, resposta.CompraParceladaId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (excluidas, mantidas) = await LerCancelamentoAsync(response);
        Assert.Equal(2, excluidas);
        Assert.Equal(1, mantidas);

        var fluxo = await ListarFluxoAsync(contaId);
        var restante = Assert.Single(fluxo);
        Assert.Equal("PAGO", restante.Status);
        Assert.Equal(1, restante.ParcelaNumero);
        Assert.Equal(resposta.Parcelas.Single(p => p.ParcelaNumero == 1).Id, restante.Id);
    }

    [Fact]
    public async Task Cancelar_TresPendentes_Retorna200ERemoveTodasDoFluxo()
    {
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));
        var resposta = await CriarComSucessoAsync(contaId, CriarRequest(quantidade: 3, status: "PENDENTE"));

        var response = await _fixture.Client.DeleteAsync(UrlCompra(contaId, resposta.CompraParceladaId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (excluidas, mantidas) = await LerCancelamentoAsync(response);
        Assert.Equal(3, excluidas);
        Assert.Equal(0, mantidas);
        Assert.Empty(await ListarFluxoAsync(contaId));
    }

    [Fact]
    public async Task Cancelar_SemNenhumaPendente_Retorna400ESemRemoverPagas()
    {
        var contaId = Guid.NewGuid();
        var compraId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));
        await _fixture.AddCompraParceladaAsync(new CompraParcelada
        {
            Id = compraId,
            Descricao = "Mouse",
            ValorTotal = 200m,
            QuantidadeParcelas = 2,
            DataCompra = new DateOnly(2026, 1, 15)
        });
        for (var i = 1; i <= 2; i++)
        {
            await _fixture.AddLancamentoAsync(new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Descricao = "Mouse",
                Valor = 100m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2026, 1, 15).AddMonths(i - 1),
                Status = StatusLancamento.Pago,
                Manual = true,
                CompraParceladaId = compraId,
                ParcelaNumero = i
            });
        }

        var response = await _fixture.Client.DeleteAsync(UrlCompra(contaId, compraId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, await _fixture.ContarLancamentosDaCompraAsync(compraId));
    }

    [Fact]
    public async Task Cancelar_CompraInexistente_Retorna404()
    {
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));

        var response = await _fixture.Client.DeleteAsync(UrlCompra(contaId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancelar_CompraDeOutraConta_Retorna404ESemRemoverNada()
    {
        var contaDonaId = Guid.NewGuid();
        var contaChamadoraId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaDonaId));
        await _fixture.AddContaAsync(CriarContaBanco(contaChamadoraId));
        var resposta = await CriarComSucessoAsync(contaDonaId, CriarRequest(quantidade: 3));

        var response = await _fixture.Client.DeleteAsync(UrlCompra(contaChamadoraId, resposta.CompraParceladaId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(3, await _fixture.ContarLancamentosDaCompraAsync(resposta.CompraParceladaId));
    }

    [Fact]
    public async Task Cancelar_CompraDeCartao_Retorna400ESemRemoverNada()
    {
        var contaId = Guid.NewGuid();
        var compraId = Guid.NewGuid();
        var faturaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaCartao(contaId));
        await _fixture.AddFaturaAsync(new Fatura
        {
            Id = faturaId,
            ContaId = contaId,
            DataFechamento = new DateOnly(2026, 1, 31),
            DataVencimento = new DateOnly(2026, 2, 15),
            Status = StatusFatura.Aberta
        });
        await _fixture.AddCompraParceladaAsync(new CompraParcelada
        {
            Id = compraId,
            Descricao = "Tenis",
            ValorTotal = 200m,
            QuantidadeParcelas = 2,
            DataCompra = new DateOnly(2026, 1, 15)
        });
        for (var i = 1; i <= 2; i++)
        {
            await _fixture.AddLancamentoAsync(new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Descricao = "Tenis",
                Valor = 100m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2026, 1, 15).AddMonths(i - 1),
                Status = StatusLancamento.Pendente,
                Manual = true,
                FaturaId = faturaId,
                CompraParceladaId = compraId,
                ParcelaNumero = i
            });
        }

        var response = await _fixture.Client.DeleteAsync(UrlCompra(contaId, compraId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, await _fixture.ContarLancamentosDaCompraAsync(compraId));
    }

    // ---------- DELETE parcela isolada pelo endpoint de lancamento: 409 (item 18) ----------

    [Fact]
    public async Task RemoverParcelaIsolada_PeloEndpointDeLancamento_Retorna409ComOrigemCompraParcelada()
    {
        var contaId = Guid.NewGuid();
        await _fixture.AddContaAsync(CriarContaBanco(contaId));
        var resposta = await CriarComSucessoAsync(contaId, CriarRequest(quantidade: 3));
        var parcela = resposta.Parcelas.Single(p => p.ParcelaNumero == 2);

        var response = await _fixture.Client.DeleteAsync(UrlLancamento(contaId, parcela.Id));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("COMPRA_PARCELADA", doc.RootElement.GetProperty("origemVinculo").GetString());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("erro").GetString()));

        Assert.NotNull(await _fixture.GetLancamentoByIdAsync(parcela.Id));
        Assert.Equal(3, (await ListarFluxoAsync(contaId)).Count);
    }
}
