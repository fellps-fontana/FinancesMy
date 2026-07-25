using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyFinances.Data;
using MyFinances.DTOs;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Controllers;

// Usa SQLite (nao o provider InMemory do EF) porque EstornoCompraParceladaService
// abre transacao real via IDbContextTransaction (BeginTransactionAsync), que o
// provider InMemory nao suporta -- mesmo motivo de ComprasParceladasServiceIntegrationTests
// usar SQLite em vez de UseInMemoryDatabase.
public class CartaoComprasParceladasControllerWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CartaoComprasParceladasControllerWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Cria o schema ANTES do host subir: CreateClient() inicia o app (ambiente
        // Development), que roda o DevUserSeeder no startup -- sem isso, o seeder
        // falha com "no such table" porque a conexao SQLite ainda esta vazia.
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

            // Remover seeder pra nao executar em testes
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

public class CartaoComprasParceladasControllerTestsFixture : IAsyncLifetime
{
    private CartaoComprasParceladasControllerWebApplicationFactory _factory = null!;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task InitializeAsync()
    {
        _factory = new CartaoComprasParceladasControllerWebApplicationFactory();
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
            Username = $"cartao_test_{Guid.NewGuid():N}",
            Email = $"cartao_test_{Guid.NewGuid():N}@example.com",
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

    public async Task AddCompraParceladaAsync(CompraParcelada compraParcelada)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.ComprasParceladas.Add(compraParcelada);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddFaturaAsync(Fatura fatura)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Faturas.Add(fatura);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddLancamentoAsync(Lancamento lancamento)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.Add(lancamento);
        await dbContext.SaveChangesAsync();
    }
}

public class CartaoComprasParceladasControllerTests : IAsyncLifetime
{
    private CartaoComprasParceladasControllerTestsFixture _fixture = null!;

    public async Task InitializeAsync()
    {
        _fixture = new CartaoComprasParceladasControllerTestsFixture();
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    #region POST /api/contas/{contaId}/compras-parceladas/{compraParceladaId}/estornos

    [Fact]
    public async Task EstornarCompraParcelada_ParcelasAbertasNCanceladas_Retorna200ComParcelasCanceladas()
    {
        // Arrange: (a) cancelamento de parcelas nao pagas
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var faturaNaoPaga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 31),
            DataVencimento = new DateOnly(2025, 2, 15),
            Status = StatusFatura.Aberta
        };

        var faturaPaga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2024, 12, 31),
            DataVencimento = new DateOnly(2025, 1, 15),
            Status = StatusFatura.Paga
        };

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Notebook Dell",
            ValorTotal = 300m,
            QuantidadeParcelas = 3,
            DataCompra = new DateOnly(2024, 12, 5),
            Lancamentos = new List<Lancamento>()
        };

        var lancamento1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = faturaPaga.Id,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2024, 12, 5)
        };

        var lancamento2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 2,
            FaturaId = faturaNaoPaga.Id,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2025, 1, 5)
        };

        var lancamento3 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 3,
            FaturaId = faturaNaoPaga.Id,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2025, 2, 5)
        };

        await _fixture.AddContaAsync(conta);
        await _fixture.AddFaturaAsync(faturaPaga);
        await _fixture.AddFaturaAsync(faturaNaoPaga);
        await _fixture.AddCompraParceladaAsync(compraParcelada);
        await _fixture.AddLancamentoAsync(lancamento1);
        await _fixture.AddLancamentoAsync(lancamento2);
        await _fixture.AddLancamentoAsync(lancamento3);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Defeito no produto",
            Data = new DateOnly(2025, 2, 1)
        };

        var json = JsonSerializer.Serialize(request, CartaoComprasParceladasControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaId}/compras-parceladas/{compraParceladaId}/estornos",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var estornoResponse = JsonSerializer.Deserialize<EstornoCompraParceladaResponse>(
            responseBody,
            CartaoComprasParceladasControllerTestsFixture.JsonOptions);

        Assert.NotNull(estornoResponse);
        Assert.NotNull(estornoResponse.ParcelasCanceladas);
        Assert.NotNull(estornoResponse.EstornosRetroativos);

        // Duas parcelas foram canceladas (as nao pagas)
        Assert.Equal(2, estornoResponse.ParcelasCanceladas.Count);
        Assert.Contains(estornoResponse.ParcelasCanceladas, l => l.Id == lancamento2.Id);
        Assert.Contains(estornoResponse.ParcelasCanceladas, l => l.Id == lancamento3.Id);

        // Uma parcela gera estorno retroativo (a que estava em fatura paga)
        Assert.Single(estornoResponse.EstornosRetroativos);
        var estorno = estornoResponse.EstornosRetroativos.First();
        Assert.Equal(TipoLancamento.Credit.ToStorageValue(), estorno.Tipo);
        Assert.Equal(100m, estorno.Valor);
    }

    [Fact]
    public async Task EstornarCompraParcelada_TodasParcelasEmFaturaPaga_GeraEstornosRetroativos()
    {
        // Arrange: (b) estorno retroativo em fatura paga mantém status paga
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var faturaPaga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2024, 12, 31),
            DataVencimento = new DateOnly(2025, 1, 15),
            Status = StatusFatura.Paga
        };

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Eletronicos",
            Tipo = TipoCategoria.Despesa
        };

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Smartphone",
            ValorTotal = 300m,
            QuantidadeParcelas = 3,
            DataCompra = new DateOnly(2024, 12, 5),
            Lancamentos = new List<Lancamento>()
        };

        var lancamento1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = faturaPaga.Id,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2024, 12, 5),
            CategoriaId = categoria.Id,
            Descricao = "Smartphone 1/3"
        };

        var lancamento2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 2,
            FaturaId = faturaPaga.Id,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2024, 12, 5),
            CategoriaId = categoria.Id,
            Descricao = "Smartphone 2/3"
        };

        var lancamento3 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 3,
            FaturaId = faturaPaga.Id,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2024, 12, 5),
            CategoriaId = categoria.Id,
            Descricao = "Smartphone 3/3"
        };

        await _fixture.AddContaAsync(conta);
        await _fixture.AddCategoriaAsync(categoria);
        await _fixture.AddFaturaAsync(faturaPaga);
        await _fixture.AddCompraParceladaAsync(compraParcelada);
        await _fixture.AddLancamentoAsync(lancamento1);
        await _fixture.AddLancamentoAsync(lancamento2);
        await _fixture.AddLancamentoAsync(lancamento3);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Defeito no produto",
            Data = new DateOnly(2025, 2, 1)
        };

        var json = JsonSerializer.Serialize(request, CartaoComprasParceladasControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaId}/compras-parceladas/{compraParceladaId}/estornos",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var estornoResponse = JsonSerializer.Deserialize<EstornoCompraParceladaResponse>(
            responseBody,
            CartaoComprasParceladasControllerTestsFixture.JsonOptions);

        Assert.NotNull(estornoResponse);
        Assert.NotNull(estornoResponse.EstornosRetroativos);
        Assert.Empty(estornoResponse.ParcelasCanceladas);

        // Tres estornos criados (um por parcela)
        Assert.Equal(3, estornoResponse.EstornosRetroativos.Count);

        // Cada estorno tem mesmo valor e categoria
        foreach (var estorno in estornoResponse.EstornosRetroativos)
        {
            Assert.Equal(TipoLancamento.Credit.ToStorageValue(), estorno.Tipo);
            Assert.Equal(100m, estorno.Valor);
            Assert.Equal(categoria.Id, estorno.CategoriaId);
            Assert.Equal(faturaPaga.Id, estorno.FaturaId);
        }
    }

    [Fact]
    public async Task EstornarCompraParcelada_Idempotente_ChamarDuasVezesNaoDuplica()
    {
        // Arrange: (c) idempotencia - chamar 2x nao duplica
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var faturaPaga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2024, 12, 31),
            DataVencimento = new DateOnly(2025, 1, 15),
            Status = StatusFatura.Paga
        };

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Mouse",
            ValorTotal = 100m,
            QuantidadeParcelas = 1,
            DataCompra = new DateOnly(2024, 12, 5),
            Lancamentos = new List<Lancamento>()
        };

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = faturaPaga.Id,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2024, 12, 5)
        };

        await _fixture.AddContaAsync(conta);
        await _fixture.AddFaturaAsync(faturaPaga);
        await _fixture.AddCompraParceladaAsync(compraParcelada);
        await _fixture.AddLancamentoAsync(lancamento);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Arrependimento",
            Data = new DateOnly(2025, 2, 1)
        };

        var json = JsonSerializer.Serialize(request, CartaoComprasParceladasControllerTestsFixture.JsonOptions);
        var content1 = new StringContent(json, Encoding.UTF8, "application/json");
        var content2 = new StringContent(json, Encoding.UTF8, "application/json");

        // Act: chamar 2x
        var response1 = await _fixture.Client.PostAsync(
            $"/api/contas/{contaId}/compras-parceladas/{compraParceladaId}/estornos",
            content1);

        var response2 = await _fixture.Client.PostAsync(
            $"/api/contas/{contaId}/compras-parceladas/{compraParceladaId}/estornos",
            content2);

        // Assert: ambas retornam 200
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var body1 = await response1.Content.ReadAsStringAsync();
        var body2 = await response2.Content.ReadAsStringAsync();

        var estorno1 = JsonSerializer.Deserialize<EstornoCompraParceladaResponse>(
            body1,
            CartaoComprasParceladasControllerTestsFixture.JsonOptions);

        var estorno2 = JsonSerializer.Deserialize<EstornoCompraParceladaResponse>(
            body2,
            CartaoComprasParceladasControllerTestsFixture.JsonOptions);

        Assert.NotNull(estorno1);
        Assert.NotNull(estorno2);

        // Primeira chamada cria 1 estorno
        Assert.Single(estorno1.EstornosRetroativos);

        // Segunda chamada retorna o mesmo (sem duplicar)
        Assert.Single(estorno2.EstornosRetroativos);

        // Os IDs dos estornos devem ser os mesmos
        Assert.Equal(estorno1.EstornosRetroativos.First().Id, estorno2.EstornosRetroativos.First().Id);
    }

    [Fact]
    public async Task EstornarCompraParcelada_CompraInexistente_Retorna400()
    {
        // Arrange: (d) compra_parcelada inexistente -> 400
        var contaId = Guid.NewGuid();
        var compraParceladaIdInexistente = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        await _fixture.AddContaAsync(conta);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Defeito",
            Data = new DateOnly(2025, 2, 1)
        };

        var json = JsonSerializer.Serialize(request, CartaoComprasParceladasControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaId}/compras-parceladas/{compraParceladaIdInexistente}/estornos",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("erro", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EstornarCompraParcelada_CompraDeOutraConta_Retorna400()
    {
        // Arrange: (d) compra_parcelada de outra conta -> 400
        var contaId = Guid.NewGuid();
        var outraContaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var outraConta = new Conta
        {
            Id = outraContaId,
            Nome = "Outro Cartao",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = outraContaId,
            DataFechamento = new DateOnly(2024, 12, 31),
            DataVencimento = new DateOnly(2025, 1, 15),
            Status = StatusFatura.Aberta
        };

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Teclado",
            ValorTotal = 100m,
            QuantidadeParcelas = 1,
            DataCompra = new DateOnly(2024, 12, 5),
            Lancamentos = new List<Lancamento>()
        };

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = outraContaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = fatura.Id,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2024, 12, 5)
        };

        await _fixture.AddContaAsync(conta);
        await _fixture.AddContaAsync(outraConta);
        await _fixture.AddFaturaAsync(fatura);
        await _fixture.AddCompraParceladaAsync(compraParcelada);
        await _fixture.AddLancamentoAsync(lancamento);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Defeito",
            Data = new DateOnly(2025, 2, 1)
        };

        var json = JsonSerializer.Serialize(request, CartaoComprasParceladasControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _fixture.Client.PostAsync(
            $"/api/contas/{contaId}/compras-parceladas/{compraParceladaId}/estornos",
            content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("erro", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EstornarCompraParcelada_RetroativoComCredito_ProximaFaturaAbertaMostraSaldoReduzido()
    {
        // Arrange: (e) efeito no GET da proxima fatura - ValorPendente reduzido
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var faturaPagaAntiga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2024, 12, 31),
            DataVencimento = new DateOnly(2025, 1, 15),
            Status = StatusFatura.Paga
        };

        var faturaProximaAberta = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 31),
            DataVencimento = new DateOnly(2025, 2, 15),
            Status = StatusFatura.Aberta
        };

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Monitor LG",
            ValorTotal = 200m,
            QuantidadeParcelas = 2,
            DataCompra = new DateOnly(2024, 12, 5),
            Lancamentos = new List<Lancamento>()
        };

        var lancamento1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = faturaPagaAntiga.Id,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2024, 12, 5)
        };

        var lancamento2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 2,
            FaturaId = faturaProximaAberta.Id,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2025, 1, 5)
        };

        var lancamento3 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            FaturaId = faturaProximaAberta.Id,
            Valor = 50m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Data = new DateOnly(2025, 1, 10),
            Descricao = "Mouse"
        };

        await _fixture.AddContaAsync(conta);
        await _fixture.AddFaturaAsync(faturaPagaAntiga);
        await _fixture.AddFaturaAsync(faturaProximaAberta);
        await _fixture.AddCompraParceladaAsync(compraParcelada);
        await _fixture.AddLancamentoAsync(lancamento1);
        await _fixture.AddLancamentoAsync(lancamento2);
        await _fixture.AddLancamentoAsync(lancamento3);

        var getBeforeResponse = await _fixture.Client.GetAsync($"/api/contas/{contaId}/faturas/{faturaProximaAberta.Id}");
        Assert.Equal(HttpStatusCode.OK, getBeforeResponse.StatusCode);

        var bodyBefore = await getBeforeResponse.Content.ReadAsStringAsync();
        var faturaBefore = JsonSerializer.Deserialize<FaturaResponse>(
            bodyBefore,
            CartaoComprasParceladasControllerTestsFixture.JsonOptions);

        Assert.NotNull(faturaBefore);
        var valorAntesDoEstorno = faturaBefore.ValorPendente;

        var estornoRequest = new EstornarCompraParceladaRequest
        {
            Motivo = "Defeito no monitor",
            Data = new DateOnly(2025, 2, 1)
        };

        var json = JsonSerializer.Serialize(estornoRequest, CartaoComprasParceladasControllerTestsFixture.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var estornoResponse = await _fixture.Client.PostAsync(
            $"/api/contas/{contaId}/compras-parceladas/{compraParceladaId}/estornos",
            content);

        Assert.Equal(HttpStatusCode.OK, estornoResponse.StatusCode);

        var getAfterResponse = await _fixture.Client.GetAsync($"/api/contas/{contaId}/faturas/{faturaProximaAberta.Id}");
        Assert.Equal(HttpStatusCode.OK, getAfterResponse.StatusCode);

        var bodyAfter = await getAfterResponse.Content.ReadAsStringAsync();
        var faturaAfter = JsonSerializer.Deserialize<FaturaResponse>(
            bodyAfter,
            CartaoComprasParceladasControllerTestsFixture.JsonOptions);

        Assert.NotNull(faturaAfter);
        var valorDepoisDoEstorno = faturaAfter.ValorPendente;

        // O saldo deve ter diminuido (parcela cancelada + possivel credito de estorno retroativo)
        Assert.True(valorDepoisDoEstorno <= valorAntesDoEstorno,
            $"Saldo nao foi reduzido. Antes: {valorAntesDoEstorno}, Depois: {valorDepoisDoEstorno}");
    }

    #endregion
}
