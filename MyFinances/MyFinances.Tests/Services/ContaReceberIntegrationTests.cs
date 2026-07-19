using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using Xunit;

namespace MyFinances.Tests.Services;

[CollectionDefinition("ContaReceberIntegration Collection")]
public class ContaReceberIntegrationCollection : ICollectionFixture<ContaReceberIntegrationTestsFixture>
{
}

public class ContaReceberIntegrationTestsFixture : IAsyncLifetime
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

[Collection("ContaReceberIntegration Collection")]
public class ContaReceberIntegrationTests
{
    private readonly ContaReceberIntegrationTestsFixture _fixture;

    public ContaReceberIntegrationTests(ContaReceberIntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ObterPorId_CarregaRecebimentosDoRepositorio_SemMockDoDbContext()
    {
        // Arrange - criar Conta (necessaria para Lancamento)
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        };
        _fixture.DbContext.Contas.Add(conta);

        // Arrange - criar ContaReceber
        var contaReceberId = Guid.NewGuid();
        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Recebivel Teste",
            Pessoa = null,
            ValorTotal = 1000m,
            DataRegistro = new DateOnly(2025, 1, 1),
            DataPrevista = new DateOnly(2025, 2, 1),
            CategoriaId = null,
            Status = StatusContaReceber.Pendente
        };
        _fixture.DbContext.ContasReceber.Add(contaReceber);
        await _fixture.DbContext.SaveChangesAsync();

        // Arrange - criar Lancamento vinculado (recebimento parcial de 400 dos 1000)
        var lancamentoId = Guid.NewGuid();
        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            ContaReceberId = contaReceberId,
            Tipo = TipoLancamento.Credit,
            Status = StatusLancamento.Pago,
            Valor = 400m,
            Data = new DateOnly(2025, 1, 15),
            Manual = false,
            Oculto = false
        };
        _fixture.DbContext.Lancamentos.Add(lancamento);
        await _fixture.DbContext.SaveChangesAsync();

        // CRITICO: limpar identity map para forcar o EF a carregar do banco real
        // Sem isso, o identity map pode "lembrar" da colecao mesmo sem o Include,
        // mascarando o bug do Include ausente
        _fixture.DbContext.ChangeTracker.Clear();

        // Act - instanciar repositorio e chamar ObterPorId
        var repositorio = new ContaReceberRepository(_fixture.DbContext);
        var resultado = await repositorio.ObterPorId(contaReceberId);

        // Assert - Recebimentos deve ter 1 item com Valor 400m
        // DEVE FALHAR (RED) porque falta .Include(cr => cr.Recebimentos) no repositorio
        Assert.NotNull(resultado);
        Assert.NotEmpty(resultado.Recebimentos);
        Assert.Single(resultado.Recebimentos);
        var primeiroRecebimento = resultado.Recebimentos.First();
        Assert.Equal(400m, primeiroRecebimento.Valor);
        Assert.Equal(TipoLancamento.Credit, primeiroRecebimento.Tipo);
        Assert.Equal(StatusLancamento.Pago, primeiroRecebimento.Status);
    }

    [Fact]
    public async Task Calcular_ComRecebimentoCarregado_CalculaSaldoPendente()
    {
        // Arrange - criar Conta (necessaria para Lancamento)
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Teste",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        };
        _fixture.DbContext.Contas.Add(conta);

        // Arrange - criar ContaReceber
        var contaReceberId = Guid.NewGuid();
        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Recebivel Teste",
            Pessoa = null,
            ValorTotal = 1000m,
            DataRegistro = new DateOnly(2025, 1, 1),
            DataPrevista = new DateOnly(2025, 2, 1),
            CategoriaId = null,
            Status = StatusContaReceber.Pendente
        };
        _fixture.DbContext.ContasReceber.Add(contaReceber);
        await _fixture.DbContext.SaveChangesAsync();

        // Arrange - criar Lancamento vinculado (recebimento parcial de 400 dos 1000)
        var lancamentoId = Guid.NewGuid();
        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            ContaReceberId = contaReceberId,
            Tipo = TipoLancamento.Credit,
            Status = StatusLancamento.Pago,
            Valor = 400m,
            Data = new DateOnly(2025, 1, 15),
            Manual = false,
            Oculto = false
        };
        _fixture.DbContext.Lancamentos.Add(lancamento);
        await _fixture.DbContext.SaveChangesAsync();

        // CRITICO: limpar identity map
        _fixture.DbContext.ChangeTracker.Clear();

        // Act - instanciar repositorio, obter ContaReceber e calcular saldo
        var repositorio = new ContaReceberRepository(_fixture.DbContext);
        var resultado = await repositorio.ObterPorId(contaReceberId);
        Assert.NotNull(resultado);

        var saldo = ContaReceberSaldoCalculator.Calcular(resultado);

        // Assert - saldo pendente deve ser 600 (1000 - 400)
        // DEVE FALHAR (RED) porque sem Recebimentos carregados, o calculo retorna 1000
        Assert.Equal(1000m, saldo.ValorTotal);
        Assert.Equal(400m, saldo.ValorRecebido);
        Assert.Equal(600m, saldo.SaldoPendente);
        Assert.Equal(StatusContaReceber.Parcial, saldo.Status);
    }

    [Fact]
    public async Task Listar_CarregaRecebimentosDoRepositorio_SemMockDoDbContext()
    {
        // Arrange - criar Conta (necessaria para Lancamento)
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Teste Listar",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        };
        _fixture.DbContext.Contas.Add(conta);

        // Arrange - criar ContaReceber
        var contaReceberId = Guid.NewGuid();
        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Recebivel Teste Listar",
            Pessoa = null,
            ValorTotal = 1000m,
            DataRegistro = new DateOnly(2025, 1, 1),
            DataPrevista = new DateOnly(2025, 2, 1),
            CategoriaId = null,
            Status = StatusContaReceber.Pendente
        };
        _fixture.DbContext.ContasReceber.Add(contaReceber);
        await _fixture.DbContext.SaveChangesAsync();

        // Arrange - criar Lancamento vinculado (recebimento parcial de 400 dos 1000)
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            ContaReceberId = contaReceberId,
            Tipo = TipoLancamento.Credit,
            Status = StatusLancamento.Pago,
            Valor = 400m,
            Data = new DateOnly(2025, 1, 15),
            Manual = false,
            Oculto = false
        };
        _fixture.DbContext.Lancamentos.Add(lancamento);
        await _fixture.DbContext.SaveChangesAsync();

        // CRITICO: limpar identity map para forcar o EF a carregar do banco real
        _fixture.DbContext.ChangeTracker.Clear();

        // Act - instanciar repositorio e chamar Listar
        var repositorio = new ContaReceberRepository(_fixture.DbContext);
        var resultado = await repositorio.Listar();

        // Assert - a ContaReceber listada deve trazer Recebimentos carregados,
        // pra permitir calcular SaldoPendente corretamente numa listagem (TASK-008)
        var contaReceberListada = resultado.Single(cr => cr.Id == contaReceberId);
        Assert.NotEmpty(contaReceberListada.Recebimentos);
        Assert.Single(contaReceberListada.Recebimentos);
        Assert.Equal(400m, contaReceberListada.Recebimentos.First().Valor);

        var saldo = ContaReceberSaldoCalculator.Calcular(contaReceberListada);
        Assert.Equal(600m, saldo.SaldoPendente);
        Assert.Equal(StatusContaReceber.Parcial, saldo.Status);
    }
}
