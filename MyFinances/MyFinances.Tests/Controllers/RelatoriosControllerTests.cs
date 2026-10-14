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
using MyFinances.DTOs.LimiteGasto;
using MyFinances.DTOs.Relatorio;
using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Controllers;

public class RelatoriosControllerWebApplicationFactory : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("RelatoriosControllerTestDb"));
        });
    }
}

public class RelatoriosControllerTestsFixture : IAsyncLifetime
{
    private readonly RelatoriosControllerWebApplicationFactory _factory;
    public HttpClient Client { get; private set; } = null!;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public RelatoriosControllerTestsFixture()
    {
        _factory = new RelatoriosControllerWebApplicationFactory();
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
            Username = $"relatorios_test_{Guid.NewGuid():N}",
            Email = $"relatorios_test_{Guid.NewGuid():N}@example.com",
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

    public async Task AddCategoriaAsync(Categoria categoria)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Categorias.Add(categoria);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddContaAsync(Conta conta)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Contas.Add(conta);
        await dbContext.SaveChangesAsync();
    }

    public async Task AddLancamentosAsync(params Lancamento[] lancamentos)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.AddRange(lancamentos);
        await dbContext.SaveChangesAsync();
    }

    public async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyFinancesDbContext>();
        dbContext.Lancamentos.RemoveRange(await dbContext.Lancamentos.ToListAsync());
        dbContext.LimitesGasto.RemoveRange(await dbContext.LimitesGasto.ToListAsync());
        dbContext.Categorias.RemoveRange(await dbContext.Categorias.ToListAsync());
        dbContext.Contas.RemoveRange(await dbContext.Contas.ToListAsync());
        await dbContext.SaveChangesAsync();
    }
}

[CollectionDefinition("Relatorios Controller Collection")]
public class RelatoriosControllerCollection : ICollectionFixture<RelatoriosControllerTestsFixture>
{
}

[Collection("Relatorios Controller Collection")]
public class RelatoriosControllerTests
{
    private static readonly Guid IdIndefinido = Guid.Parse("00000000-0000-0000-0000-00000000ABCD");

    private readonly RelatoriosControllerTestsFixture _fixture;

    // Massa de Marco/2026 (mes consultado) e Fevereiro/2026 (mes anterior):
    //   Mercado  : 100 banco (Pago) + 50 cartao (Pendente)            = 150
    //   Alimentacao: 30 cartao na sub Restaurante + 20 banco na raiz  =  50
    //   Sem categoria: 25 banco (Pago)                                =  25
    //   Total Marco = 225 | Total Fevereiro = 100 | Variacao = 125,00
    // Excluidos (nao contam): transferencia 200, oculto 70, credit 40, sugerido 15, receita 5000.
    private Guid _contaBancoId;
    private Guid _contaCartaoId;
    private Guid _contaPoupancaId;
    private Guid _categoriaMercadoId;
    private Guid _categoriaAlimentacaoId;
    private Guid _categoriaRestauranteId;
    private Guid _categoriaSalarioId;

    public RelatoriosControllerTests(RelatoriosControllerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region GET /api/relatorios/gasto-por-categoria (resumo)

    [Fact]
    public async Task ObterResumo_MassaCompleta_RetornaLinhasOrdenadasTotalEVariacao()
    {
        await PrepararMassaAsync();

        var resumo = await ObterResumoAsync(2026, 3);

        Assert.Equal(2026, resumo.Ano);
        Assert.Equal(3, resumo.Mes);
        Assert.Equal(225m, resumo.TotalGasto);
        Assert.Equal(100m, resumo.TotalMesAnterior);
        Assert.Equal<decimal?>(125.00m, resumo.VariacaoPercentual);

        Assert.Equal(3, resumo.Linhas.Count);
        Assert.Equal(new[] { "Mercado", "Alimentacao", "Sem categoria" }, resumo.Linhas.Select(l => l.Nome));
        Assert.Equal(new[] { 150m, 50m, 25m }, resumo.Linhas.Select(l => l.Valor));
        Assert.Equal(new[] { 66.67m, 22.22m, 11.11m }, resumo.Linhas.Select(l => l.Percentual));
    }

    [Fact]
    public async Task ObterResumo_MassaCompleta_NaoContaTransferenciaOcultoCreditSugeridoNemReceita()
    {
        await PrepararMassaAsync();

        var resumo = await ObterResumoAsync(2026, 3);

        var linhaMercado = Assert.Single(resumo.Linhas, l => l.CategoriaId == _categoriaMercadoId);
        Assert.Equal(150m, linhaMercado.Valor);
        Assert.DoesNotContain(resumo.Linhas, l => l.CategoriaId == _categoriaSalarioId);
        Assert.Equal(225m, resumo.TotalGasto);
    }

    [Fact]
    public async Task ObterResumo_LancamentoSemCategoria_ApareceComoLinhaSemCategoria()
    {
        await PrepararMassaAsync();

        var resumo = await ObterResumoAsync(2026, 3);

        var linha = Assert.Single(resumo.Linhas, l => l.CategoriaId == null);
        Assert.Equal("Sem categoria", linha.Nome);
        Assert.Null(linha.Icone);
        Assert.Equal(25m, linha.Valor);
    }

    [Fact]
    public async Task ObterResumo_MesSemDados_TotalZeroLinhasVaziasEVariacaoNula()
    {
        await PrepararMassaAsync();

        var resumo = await ObterResumoAsync(2020, 1);

        Assert.Equal(0m, resumo.TotalGasto);
        Assert.Equal(0m, resumo.TotalMesAnterior);
        Assert.Null(resumo.VariacaoPercentual);
        Assert.Empty(resumo.Linhas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task ObterResumo_MesInvalido_Retorna400BadRequest(int mesInvalido)
    {
        await PrepararMassaAsync();

        var response = await _fixture.Client.GetAsync($"/api/relatorios/gasto-por-categoria?ano=2026&mes={mesInvalido}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GET /api/relatorios/gasto-por-categoria/detalhe

    [Fact]
    public async Task ObterDetalhe_CategoriaRaiz_SomaDoDetalheEhIgualAoValorDaLinhaDoResumo()
    {
        await PrepararMassaAsync();

        var resumo = await ObterResumoAsync(2026, 3);
        var linhaResumo = Assert.Single(resumo.Linhas, l => l.CategoriaId == _categoriaMercadoId);

        var detalhe = await ObterDetalheAsync(2026, 3, _categoriaMercadoId);

        Assert.Equal(linhaResumo.Valor, detalhe.Total);
        Assert.Equal(linhaResumo.Percentual, detalhe.PercentualDoTotal);
        Assert.Equal(linhaResumo.Valor, detalhe.Lancamentos.Sum(l => l.Valor));
        Assert.Equal(2, detalhe.Lancamentos.Count);
    }

    [Fact]
    public async Task ObterDetalhe_LancamentosEmOrdemCrescenteDeDataComContaTipoBancoOuCartao()
    {
        await PrepararMassaAsync();

        var detalhe = await ObterDetalheAsync(2026, 3, _categoriaMercadoId);

        Assert.Equal(new[] { new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 10) }, detalhe.Lancamentos.Select(l => l.Data));
        Assert.Equal(new[] { "BANCO", "CARTAO" }, detalhe.Lancamentos.Select(l => l.ContaTipo));
        Assert.Equal(new[] { _contaBancoId, _contaCartaoId }, detalhe.Lancamentos.Select(l => l.ContaId));
    }

    [Fact]
    public async Task ObterDetalhe_CategoriaComSubcategoria_IncluiSubcategoriaComSubcategoriaNomePreenchido()
    {
        await PrepararMassaAsync();

        var detalhe = await ObterDetalheAsync(2026, 3, _categoriaAlimentacaoId);

        Assert.Equal(50m, detalhe.Total);
        Assert.Equal(50m, detalhe.Lancamentos.Sum(l => l.Valor));
        Assert.Equal("Restaurante", detalhe.Lancamentos.Single(l => l.Valor == 30m).SubcategoriaNome);
        Assert.Null(detalhe.Lancamentos.Single(l => l.Valor == 20m).SubcategoriaNome);
    }

    [Fact]
    public async Task ObterDetalhe_CategoriaComLimite_RetornaLimiteComGastoRealizadoIgualAoTotal()
    {
        await PrepararMassaAsync();
        await DefinirLimiteAsync(_categoriaMercadoId, 120m);

        var detalhe = await ObterDetalheAsync(2026, 3, _categoriaMercadoId);

        Assert.NotNull(detalhe.Limite);
        Assert.Equal(120m, detalhe.Limite!.ValorLimite);
        Assert.Equal(detalhe.Total, detalhe.Limite.GastoRealizado);
        Assert.Equal(150m, detalhe.Limite.GastoRealizado);
    }

    [Fact]
    public async Task ObterDetalhe_SemCategoriaId_RetornaSemCategoriaComTotalESemLimite()
    {
        await PrepararMassaAsync();

        var detalhe = await ObterDetalheAsync(2026, 3, categoriaId: null);

        Assert.Null(detalhe.CategoriaId);
        Assert.Equal("Sem categoria", detalhe.Nome);
        Assert.Null(detalhe.Icone);
        Assert.Equal(25m, detalhe.Total);
        Assert.Null(detalhe.Limite);
        Assert.Single(detalhe.Lancamentos);
    }

    [Fact]
    public async Task ObterDetalhe_MesSemDados_TotalZeroELancamentosVazios()
    {
        await PrepararMassaAsync();

        var detalhe = await ObterDetalheAsync(2020, 1, _categoriaMercadoId);

        Assert.Equal(0m, detalhe.Total);
        Assert.Equal(0m, detalhe.PercentualDoTotal);
        Assert.Empty(detalhe.Lancamentos);
    }

    [Fact]
    public async Task ObterDetalhe_CategoriaInexistente_Retorna404NotFound()
    {
        await PrepararMassaAsync();

        var response = await _fixture.Client.GetAsync(
            $"/api/relatorios/gasto-por-categoria/detalhe?ano=2026&mes=3&categoriaId={IdIndefinido}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ObterDetalhe_Subcategoria_Retorna400BadRequest()
    {
        await PrepararMassaAsync();

        var response = await _fixture.Client.GetAsync(
            $"/api/relatorios/gasto-por-categoria/detalhe?ano=2026&mes=3&categoriaId={_categoriaRestauranteId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ObterDetalhe_Mes13_Retorna400BadRequest()
    {
        await PrepararMassaAsync();

        var response = await _fixture.Client.GetAsync(
            $"/api/relatorios/gasto-por-categoria/detalhe?ano=2026&mes=13&categoriaId={_categoriaMercadoId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Helpers

    private async Task PrepararMassaAsync()
    {
        await _fixture.ClearAsync();

        var contaBanco = CriarConta("Nubank Conta", TipoConta.Banco);
        var contaCartao = CriarConta("Nubank Cartao", TipoConta.Cartao);
        var contaPoupanca = CriarConta("Poupanca", TipoConta.Banco);
        await _fixture.AddContaAsync(contaBanco);
        await _fixture.AddContaAsync(contaCartao);
        await _fixture.AddContaAsync(contaPoupanca);
        _contaBancoId = contaBanco.Id;
        _contaCartaoId = contaCartao.Id;
        _contaPoupancaId = contaPoupanca.Id;

        var mercado = CriarCategoria("Mercado", TipoCategoria.Despesa, icone: "cart");
        var alimentacao = CriarCategoria("Alimentacao", TipoCategoria.Despesa, icone: "food");
        var restaurante = CriarCategoria("Restaurante", TipoCategoria.Despesa, parentId: alimentacao.Id);
        var salario = CriarCategoria("Salario", TipoCategoria.Receita);
        foreach (var categoria in new[] { mercado, alimentacao, restaurante, salario })
        {
            await _fixture.AddCategoriaAsync(categoria);
        }
        _categoriaMercadoId = mercado.Id;
        _categoriaAlimentacaoId = alimentacao.Id;
        _categoriaRestauranteId = restaurante.Id;
        _categoriaSalarioId = salario.Id;

        await _fixture.AddLancamentosAsync(
            // Fevereiro/2026 (mes anterior): total 100
            Debito(_contaBancoId, _categoriaMercadoId, 100m, new DateOnly(2026, 2, 14)),

            // Marco/2026 (mes consultado): entram
            Debito(_contaBancoId, _categoriaMercadoId, 100m, new DateOnly(2026, 3, 5), "Supermercado"),
            Debito(_contaCartaoId, _categoriaMercadoId, 50m, new DateOnly(2026, 3, 10), "Compra cartao",
                status: StatusLancamento.Pendente),
            Debito(_contaCartaoId, _categoriaRestauranteId, 30m, new DateOnly(2026, 3, 12), "Almoco"),
            Debito(_contaBancoId, _categoriaAlimentacaoId, 20m, new DateOnly(2026, 3, 15), "Padaria"),
            Debito(_contaBancoId, null, 25m, new DateOnly(2026, 3, 18), "Sem categoria"),

            // Marco/2026: NAO entram
            Debito(_contaBancoId, _categoriaMercadoId, 200m, new DateOnly(2026, 3, 6), "Transferencia",
                transferenciaId: Guid.NewGuid()),
            Debito(_contaCartaoId, _categoriaMercadoId, 70m, new DateOnly(2026, 3, 7), "Oculto", oculto: true),
            Debito(_contaCartaoId, _categoriaMercadoId, 40m, new DateOnly(2026, 3, 8), "Estorno",
                tipo: TipoLancamento.Credit),
            Debito(_contaBancoId, _categoriaMercadoId, 15m, new DateOnly(2026, 3, 9), "Sugerido",
                status: StatusLancamento.Sugerido),
            Debito(_contaBancoId, _categoriaSalarioId, 5000m, new DateOnly(2026, 3, 1), "Salario",
                tipo: TipoLancamento.Credit));
    }

    private static Conta CriarConta(string nome, TipoConta tipo) => new()
    {
        Id = Guid.NewGuid(),
        Nome = nome,
        Tipo = tipo,
        Origem = OrigemConta.Manual,
        SaldoManual = 0m,
        Ativa = true
    };

    private static Categoria CriarCategoria(string nome, TipoCategoria tipo, Guid? parentId = null, string? icone = null) => new()
    {
        Id = Guid.NewGuid(),
        Nome = nome,
        Tipo = tipo,
        ParentId = parentId,
        Icone = icone,
        Arquivada = false
    };

    private static Lancamento Debito(
        Guid contaId,
        Guid? categoriaId,
        decimal valor,
        DateOnly data,
        string descricao = "Lancamento",
        StatusLancamento status = StatusLancamento.Pago,
        TipoLancamento tipo = TipoLancamento.Debit,
        bool oculto = false,
        Guid? transferenciaId = null) => new()
    {
        Id = Guid.NewGuid(),
        ContaId = contaId,
        CategoriaId = categoriaId,
        Descricao = descricao,
        Valor = valor,
        Tipo = tipo,
        Data = data,
        Status = status,
        Manual = true,
        Oculto = oculto,
        TransferenciaId = transferenciaId
    };

    private async Task DefinirLimiteAsync(Guid categoriaId, decimal valorLimite)
    {
        var request = new DefinirLimiteGastoRequest
        {
            CategoriaId = categoriaId,
            ValorLimite = valorLimite
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _fixture.Client.PostAsync("/api/limites-gasto", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<RelatorioGastoPorCategoriaResponse> ObterResumoAsync(int ano, int mes)
    {
        var response = await _fixture.Client.GetAsync($"/api/relatorios/gasto-por-categoria?ano={ano}&mes={mes}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var resumo = JsonSerializer.Deserialize<RelatorioGastoPorCategoriaResponse>(body, RelatoriosControllerTestsFixture.JsonOptions);

        Assert.NotNull(resumo);
        return resumo!;
    }

    private async Task<RelatorioCategoriaDetalheResponse> ObterDetalheAsync(int ano, int mes, Guid? categoriaId)
    {
        var url = $"/api/relatorios/gasto-por-categoria/detalhe?ano={ano}&mes={mes}";
        if (categoriaId.HasValue)
        {
            url += $"&categoriaId={categoriaId.Value}";
        }

        var response = await _fixture.Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var detalhe = JsonSerializer.Deserialize<RelatorioCategoriaDetalheResponse>(body, RelatoriosControllerTestsFixture.JsonOptions);

        Assert.NotNull(detalhe);
        return detalhe!;
    }

    #endregion
}
