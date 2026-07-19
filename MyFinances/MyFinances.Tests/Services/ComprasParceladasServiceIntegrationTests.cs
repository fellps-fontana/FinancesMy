using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

[CollectionDefinition("ComprasParceladas Integration Collection")]
public class ComprasParceladasIntegrationCollection : ICollectionFixture<ComprasParceladasIntegrationTestsFixture>
{
}

public class ComprasParceladasIntegrationTestsFixture : IAsyncLifetime
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

[Collection("ComprasParceladas Integration Collection")]
public class ComprasParceladasServiceIntegrationTests
{
    private readonly ComprasParceladasIntegrationTestsFixture _fixture;

    public ComprasParceladasServiceIntegrationTests(ComprasParceladasIntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CriarCompraParcelada_Compra100ReaisEm3x_GeraTresLancamentosComValoresExatos()
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
        var lancamentoRepository = new LancamentoRepository(_fixture.DbContext);
        var compraParceladaRepository = new CompraParceladaRepository(_fixture.DbContext);
        var faturaCicloService = new FaturaCicloService(faturaRepository, contaRepository);
        var validacaoCartaoService = new ValidacaoCartaoService(contaRepository);
        var comprasParceladasService = new ComprasParceladasService(
            compraParceladaRepository,
            lancamentoRepository,
            faturaCicloService,
            validacaoCartaoService);

        var request = new CriarCompraParceladaRequest
        {
            Descricao = "Notebook",
            ValorTotal = 100.00m,
            QuantidadeParcelas = 3,
            CategoriaId = null,
            DataCompra = new DateOnly(2025, 1, 5)
        };

        // Act
        var (sucesso, compraParcelada, erro) = await comprasParceladasService.CriarCompraParceladaAsync(contaId, request);

        // Assert - sucesso e criacao
        Assert.True(sucesso, $"Erro ao criar compra parcelada: {erro}");
        Assert.NotNull(compraParcelada);
        Assert.Equal("Notebook", compraParcelada.Descricao);
        Assert.Equal(100.00m, compraParcelada.ValorTotal);
        Assert.Equal(3, compraParcelada.QuantidadeParcelas);

        // Assert - lancamentos no banco com valores exatos
        var lancamentos = await _fixture.DbContext.Lancamentos
            .Where(l => l.CompraParceladaId == compraParcelada.Id)
            .OrderBy(l => l.ParcelaNumero)
            .ToListAsync();

        Assert.Equal(3, lancamentos.Count);
        Assert.Equal(33.33m, lancamentos[0].Valor);
        Assert.Equal(33.33m, lancamentos[1].Valor);
        Assert.Equal(33.34m, lancamentos[2].Valor);

        // Assert - soma dos valores bate com o total
        var somaValores = lancamentos.Sum(l => l.Valor);
        Assert.Equal(100.00m, somaValores);
    }

    [Fact]
    public async Task CriarCompraParcelada_TresParcelasEmMesesDiferentes_CadaLancamentoEmSuaFatura()
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
        var lancamentoRepository = new LancamentoRepository(_fixture.DbContext);
        var compraParceladaRepository = new CompraParceladaRepository(_fixture.DbContext);
        var faturaCicloService = new FaturaCicloService(faturaRepository, contaRepository);
        var validacaoCartaoService = new ValidacaoCartaoService(contaRepository);
        var comprasParceladasService = new ComprasParceladasService(
            compraParceladaRepository,
            lancamentoRepository,
            faturaCicloService,
            validacaoCartaoService);

        var request = new CriarCompraParceladaRequest
        {
            Descricao = "Notebook",
            ValorTotal = 100.00m,
            QuantidadeParcelas = 3,
            CategoriaId = null,
            DataCompra = new DateOnly(2025, 1, 5)
        };

        // Act
        var (sucesso, compraParcelada, erro) = await comprasParceladasService.CriarCompraParceladaAsync(contaId, request);

        // Assert - sucesso
        Assert.True(sucesso, $"Erro ao criar compra parcelada: {erro}");
        Assert.NotNull(compraParcelada);

        // Assert - cada lancamento em sua propria fatura
        var lancamentos = await _fixture.DbContext.Lancamentos
            .Where(l => l.CompraParceladaId == compraParcelada.Id)
            .OrderBy(l => l.ParcelaNumero)
            .Include(l => l.Fatura)
            .ToListAsync();

        Assert.Equal(3, lancamentos.Count);

        // Primeira parcela: fatura jan 10 - jan 20 (vencimento jan 20)
        var fatura1 = lancamentos[0].Fatura;
        Assert.NotNull(fatura1);
        Assert.Equal(new DateOnly(2025, 1, 10), fatura1.DataFechamento);
        Assert.Equal(new DateOnly(2025, 1, 20), fatura1.DataVencimento);

        // Segunda parcela: fatura fev 10 - fev 20 (vencimento fev 20)
        var fatura2 = lancamentos[1].Fatura;
        Assert.NotNull(fatura2);
        Assert.Equal(new DateOnly(2025, 2, 10), fatura2.DataFechamento);
        Assert.Equal(new DateOnly(2025, 2, 20), fatura2.DataVencimento);

        // Terceira parcela: fatura mar 10 - mar 20 (vencimento mar 20)
        var fatura3 = lancamentos[2].Fatura;
        Assert.NotNull(fatura3);
        Assert.Equal(new DateOnly(2025, 3, 10), fatura3.DataFechamento);
        Assert.Equal(new DateOnly(2025, 3, 20), fatura3.DataVencimento);

        // Assert - faturas sao diferentes
        var faturasIds = new[] { lancamentos[0].FaturaId, lancamentos[1].FaturaId, lancamentos[2].FaturaId };
        Assert.Equal(3, faturasIds.Distinct().Count());
    }

    [Fact]
    public async Task CriarCompraParcelada_SomaDeValoresIguaiAoTotal()
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
        var lancamentoRepository = new LancamentoRepository(_fixture.DbContext);
        var compraParceladaRepository = new CompraParceladaRepository(_fixture.DbContext);
        var faturaCicloService = new FaturaCicloService(faturaRepository, contaRepository);
        var validacaoCartaoService = new ValidacaoCartaoService(contaRepository);
        var comprasParceladasService = new ComprasParceladasService(
            compraParceladaRepository,
            lancamentoRepository,
            faturaCicloService,
            validacaoCartaoService);

        var valorTotal = 1000.00m;
        var quantidadeParcelas = 7;
        var request = new CriarCompraParceladaRequest
        {
            Descricao = "Passagem Aerea",
            ValorTotal = valorTotal,
            QuantidadeParcelas = quantidadeParcelas,
            CategoriaId = null,
            DataCompra = new DateOnly(2025, 1, 5)
        };

        // Act
        var (sucesso, compraParcelada, erro) = await comprasParceladasService.CriarCompraParceladaAsync(contaId, request);

        // Assert - sucesso
        Assert.True(sucesso, $"Erro ao criar compra parcelada: {erro}");
        Assert.NotNull(compraParcelada);

        // Assert - soma dos lancamentos bate com valor total
        var lancamentos = await _fixture.DbContext.Lancamentos
            .Where(l => l.CompraParceladaId == compraParcelada.Id)
            .ToListAsync();

        Assert.Equal(quantidadeParcelas, lancamentos.Count);
        var somaValores = lancamentos.Sum(l => l.Valor);
        Assert.Equal(valorTotal, somaValores);
    }

    [Fact]
    public async Task CriarCompraParcelada_QuantidadeParcelasUm_RejeitadoENadaPersistido()
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
        var lancamentoRepository = new LancamentoRepository(_fixture.DbContext);
        var compraParceladaRepository = new CompraParceladaRepository(_fixture.DbContext);
        var faturaCicloService = new FaturaCicloService(faturaRepository, contaRepository);
        var validacaoCartaoService = new ValidacaoCartaoService(contaRepository);
        var comprasParceladasService = new ComprasParceladasService(
            compraParceladaRepository,
            lancamentoRepository,
            faturaCicloService,
            validacaoCartaoService);

        var request = new CriarCompraParceladaRequest
        {
            Descricao = "Notebook",
            ValorTotal = 100.00m,
            QuantidadeParcelas = 1,
            CategoriaId = null,
            DataCompra = new DateOnly(2025, 1, 5)
        };

        // Act
        var (sucesso, compraParcelada, erro) = await comprasParceladasService.CriarCompraParceladaAsync(contaId, request);

        // Assert - rejeitado
        Assert.False(sucesso);
        Assert.Null(compraParcelada);
        Assert.NotNull(erro);
        Assert.Contains("minimo 2", erro);

        // Assert - nada persistido no banco
        var comprasParceladas = await _fixture.DbContext.ComprasParceladas.ToListAsync();
        Assert.Empty(comprasParceladas);

        var lancamentos = await _fixture.DbContext.Lancamentos.ToListAsync();
        Assert.Empty(lancamentos);
    }

    [Fact]
    public async Task CriarCompraParcelada_TodosLancamentosCompartilhamCompraParceladaIdEParcelaNumero()
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
        var lancamentoRepository = new LancamentoRepository(_fixture.DbContext);
        var compraParceladaRepository = new CompraParceladaRepository(_fixture.DbContext);
        var faturaCicloService = new FaturaCicloService(faturaRepository, contaRepository);
        var validacaoCartaoService = new ValidacaoCartaoService(contaRepository);
        var comprasParceladasService = new ComprasParceladasService(
            compraParceladaRepository,
            lancamentoRepository,
            faturaCicloService,
            validacaoCartaoService);

        var quantidadeParcelas = 5;
        var request = new CriarCompraParceladaRequest
        {
            Descricao = "Monitor",
            ValorTotal = 250.00m,
            QuantidadeParcelas = quantidadeParcelas,
            CategoriaId = null,
            DataCompra = new DateOnly(2025, 1, 5)
        };

        // Act
        var (sucesso, compraParcelada, erro) = await comprasParceladasService.CriarCompraParceladaAsync(contaId, request);

        // Assert - sucesso
        Assert.True(sucesso, $"Erro ao criar compra parcelada: {erro}");
        Assert.NotNull(compraParcelada);

        // Assert - todos lancamentos tem o mesmo CompraParceladaId
        var lancamentos = await _fixture.DbContext.Lancamentos
            .Where(l => l.CompraParceladaId == compraParcelada.Id)
            .OrderBy(l => l.ParcelaNumero)
            .ToListAsync();

        Assert.Equal(quantidadeParcelas, lancamentos.Count);
        foreach (var lancamento in lancamentos)
        {
            Assert.Equal(compraParcelada.Id, lancamento.CompraParceladaId);
        }

        // Assert - ParcelaNumero vai de 1 a N sem lacuna
        for (int i = 0; i < quantidadeParcelas; i++)
        {
            Assert.Equal(i + 1, lancamentos[i].ParcelaNumero);
        }
    }
}
