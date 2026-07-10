using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

[CollectionDefinition("FaturaCicloIntegration Collection")]
public class FaturaCicloIntegrationCollection : ICollectionFixture<FaturaCicloIntegrationTestsFixture>
{
}

public class FaturaCicloIntegrationTestsFixture : IAsyncLifetime
{
    private SqliteConnection? _connection;
    public MyFinancesDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString = "DataSource=:memory:";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        _connection = connection;

        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseSqlite(connection)
            .Options;

        DbContext = new MyFinancesDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}

[Collection("FaturaCicloIntegration Collection")]
public class FaturaCicloIntegrationTests
{
    private readonly FaturaCicloIntegrationTestsFixture _fixture;

    public FaturaCicloIntegrationTests(FaturaCicloIntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ResolverFaturaParaLancamento_ComLancamentoDentroDoCicloAtual_ReutilizaFaturaAberta()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 10,
            DiaVencimento = 20,
            Ativa = true
        };
        _fixture.DbContext.Contas.Add(conta);
        await _fixture.DbContext.SaveChangesAsync();

        var faturaRepository = new FaturaRepository(_fixture.DbContext);
        var contaRepository = new ContaRepository(_fixture.DbContext);
        var faturaCicloService = new FaturaCicloService(faturaRepository, contaRepository);

        var dataCompra1 = new DateOnly(2025, 1, 5);

        // Act - primeira compra
        var (fatura1, rejeitada1, motivo1) = await faturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, dataCompra1);

        // segunda compra dentro do mesmo ciclo
        var dataCompra2 = new DateOnly(2025, 1, 8);
        var (fatura2, rejeitada2, motivo2) = await faturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, dataCompra2);

        // Assert
        Assert.False(rejeitada1);
        Assert.False(rejeitada2);
        Assert.NotNull(fatura1);
        Assert.NotNull(fatura2);
        Assert.Equal(fatura1.Id, fatura2.Id);
        Assert.Equal(StatusFatura.Aberta, fatura2.Status);

        // Confirma que so existe 1 fatura aberta
        var faturasAbertas = await _fixture.DbContext.Faturas
            .Where(f => f.ContaId == contaId && f.Status == StatusFatura.Aberta)
            .ToListAsync();
        Assert.Single(faturasAbertas);
    }

    [Fact]
    public async Task ResolverFaturaParaLancamento_ComLancamentoEmCicloSeguinte_FechaAnterioresCriaNovaComIndiceUnico()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 10,
            DiaVencimento = 20,
            Ativa = true
        };
        _fixture.DbContext.Contas.Add(conta);
        await _fixture.DbContext.SaveChangesAsync();

        var faturaRepository = new FaturaRepository(_fixture.DbContext);
        var contaRepository = new ContaRepository(_fixture.DbContext);
        var faturaCicloService = new FaturaCicloService(faturaRepository, contaRepository);

        var dataCompra1 = new DateOnly(2025, 1, 5);

        // Act - primeira compra (ciclo jan 10-fev 20)
        var (fatura1, rejeitada1, _) = await faturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, dataCompra1);
        Assert.False(rejeitada1);
        Assert.NotNull(fatura1);
        Assert.Equal(StatusFatura.Aberta, fatura1.Status);

        // segunda compra apos a data de fechamento (ciclo fev 10-mar 20)
        var dataCompra2 = new DateOnly(2025, 2, 15);
        var (fatura2, rejeitada2, motivo2) = await faturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, dataCompra2);

        // Assert
        Assert.False(rejeitada2, motivo2);
        Assert.NotNull(fatura2);

        // Fatura 1 deve estar Fechada
        var fatura1Atualizada = await _fixture.DbContext.Faturas.FirstOrDefaultAsync(f => f.Id == fatura1.Id);
        Assert.NotNull(fatura1Atualizada);
        Assert.Equal(StatusFatura.Fechada, fatura1Atualizada.Status);

        // Fatura 2 deve estar Aberta e ser diferente da 1
        Assert.NotEqual(fatura1.Id, fatura2.Id);
        Assert.Equal(StatusFatura.Aberta, fatura2.Status);

        // Deve haver exatamente 1 fatura aberta
        var faturasAbertas = await _fixture.DbContext.Faturas
            .Where(f => f.ContaId == contaId && f.Status == StatusFatura.Aberta)
            .ToListAsync();
        Assert.Single(faturasAbertas);
        Assert.Equal(fatura2.Id, faturasAbertas[0].Id);
    }

    [Fact]
    public async Task ResolverFaturaParaLancamento_IndiceUnicoImpedeFaturasAbertasDuplicadas()
    {
        // Arrange - simula tentativa de inserir 2 faturas abertas diretamente (sem ir por ResolverFatura)
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 10,
            DiaVencimento = 20,
            Ativa = true
        };
        _fixture.DbContext.Contas.Add(conta);
        await _fixture.DbContext.SaveChangesAsync();

        var fatura1 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta
        };
        _fixture.DbContext.Faturas.Add(fatura1);
        await _fixture.DbContext.SaveChangesAsync();

        var fatura2 = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta
        };
        _fixture.DbContext.Faturas.Add(fatura2);

        // Act & Assert - deve lancar exception por violacao do indice unico
        await Assert.ThrowsAsync<DbUpdateException>(async () => await _fixture.DbContext.SaveChangesAsync());
    }

}
