using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using MyFinances.Tests.Helpers;
using Xunit;

namespace MyFinances.Tests.Services;

// TASK-240: indice unico (conta, data_fechamento) e recuperacao de corrida no Resolver.
// SQLite em memoria, um banco por teste, relogio fixo em 2026-10-13.
// Cartao 20/27: ciclo corrente fecha em 20/10/2026.
public class FaturaCicloConflitoTests : IAsyncLifetime
{
    private static readonly DateOnly Hoje = new(2026, 10, 13);

    private SqliteConnection _connection = null!;
    private MyFinancesDbContext _db = null!;
    private RelogioFake _relogio = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new MyFinancesDbContext(options);
        await _db.Database.EnsureCreatedAsync();
        _relogio = new RelogioFake(Hoje);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.CloseAsync();
        _connection.Dispose();
    }

    #region 1-2. Indice unico (conta, data_fechamento)

    [Fact]
    public async Task MesmaContaEMesmaDataFechamento_SaveChangesLancaDbUpdateException()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        SeedFatura(contaId, new DateOnly(2026, 9, 20), StatusFatura.Fechada);
        SeedFatura(contaId, new DateOnly(2026, 9, 20), StatusFatura.Futura);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task MesmaDataFechamentoEmContasDiferentes_NaoConflita()
    {
        // Arrange
        var contaA = await SeedCartaoAsync();
        var contaB = await SeedCartaoAsync();
        SeedFatura(contaA, new DateOnly(2026, 9, 20), StatusFatura.Fechada);
        SeedFatura(contaB, new DateOnly(2026, 9, 20), StatusFatura.Fechada);

        // Act
        var excecao = await Record.ExceptionAsync(() => _db.SaveChangesAsync());

        // Assert
        Assert.Null(excecao);
    }

    [Fact]
    public async Task MesmaContaEmDatasFechamentoDiferentes_NaoConflita()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        SeedFatura(contaId, new DateOnly(2026, 9, 20), StatusFatura.Fechada);
        SeedFatura(contaId, new DateOnly(2026, 10, 20), StatusFatura.Futura);

        // Act
        var excecao = await Record.ExceptionAsync(() => _db.SaveChangesAsync());

        // Assert
        Assert.Null(excecao);
    }

    #endregion

    #region 3-7. Corrida no Resolver: recuperacao pela fatura vencedora

    [Fact]
    public async Task CorridaNoResolver_DevolveAVencedoraSemRejeitarESobraUmaFatura()
    {
        // Arrange - vencedora FUTURA 20/11; o lookup da 1a chamada devolve null (corrida simulada)
        var contaId = await SeedCartaoAsync();
        var vencedora = SeedFatura(contaId, new DateOnly(2026, 11, 20), StatusFatura.Futura);
        await _db.SaveChangesAsync();
        var ciclo = CriarCiclo(lookupsNulos: 1);

        // Act
        var (fatura, rejeitada, motivo) = await ciclo.ResolverFaturaParaLancamentoAsync(contaId, new DateOnly(2026, 11, 5));

        // Assert
        Assert.False(rejeitada, motivo);
        Assert.NotNull(fatura);
        Assert.Equal(vencedora.Id, fatura.Id);
        Assert.Equal(1, await ContarFaturasAsync(contaId, new DateOnly(2026, 11, 20)));
    }

    [Fact]
    public async Task CorridaNoResolver_DepoisDaRecuperacao_NaoRestaFaturaAddedNoChangeTracker()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        SeedFatura(contaId, new DateOnly(2026, 11, 20), StatusFatura.Futura);
        await _db.SaveChangesAsync();
        var ciclo = CriarCiclo(lookupsNulos: 1);

        // Act
        await ciclo.ResolverFaturaParaLancamentoAsync(contaId, new DateOnly(2026, 11, 5));
        var excecao = await Record.ExceptionAsync(() => _db.SaveChangesAsync());

        // Assert
        Assert.DoesNotContain(_db.ChangeTracker.Entries<Fatura>(), e => e.State == EntityState.Added);
        Assert.Null(excecao);
    }

    [Fact]
    public async Task CorridaNoResolver_ReleituraNula_SobeADbUpdateExceptionOriginal()
    {
        // Arrange - conflito real (Fechada 20/09 ja gravada) e lookup sempre nulo: nada para recuperar
        var contaId = await SeedCartaoAsync();
        SeedFatura(contaId, new DateOnly(2026, 9, 20), StatusFatura.Fechada);
        await _db.SaveChangesAsync();
        var ciclo = CriarCiclo(sempreNulo: true);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(
            () => ciclo.ResolverFaturaParaLancamentoAsync(contaId, new DateOnly(2026, 9, 15)));
    }

    [Fact]
    // LIMITACAO: este teste so valida o SQLite. No Postgres, a violacao dentro de BeginTransactionAsync
    // aborta a transacao (25P02) e a releitura falha; la o erro sobe e a operacao e desfeita. Nao prometer
    // recuperacao transacional no Postgres sem savepoint e teste proprio.
    public async Task CorridaNoResolver_DentroDeTransacaoSqlite_RecuperaVencedora()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        var vencedora = SeedFatura(contaId, new DateOnly(2026, 11, 20), StatusFatura.Futura);
        await _db.SaveChangesAsync();
        var ciclo = CriarCiclo(lookupsNulos: 1);

        // Act
        await using var transacao = await _db.Database.BeginTransactionAsync();
        var (fatura, rejeitada, motivo) = await ciclo.ResolverFaturaParaLancamentoAsync(contaId, new DateOnly(2026, 11, 5));
        await transacao.CommitAsync();

        // Assert
        Assert.False(rejeitada, motivo);
        Assert.NotNull(fatura);
        Assert.Equal(vencedora.Id, fatura.Id);
        Assert.Equal(1, await ContarFaturasAsync(contaId, new DateOnly(2026, 11, 20)));
    }

    [Fact]
    public async Task CorridaNoResolver_DepoisDoConflito_NoMaximoUmaAbertaEUmaFaturaPorData()
    {
        // Arrange - vencedora ABERTA com fechamento hoje (20/10); lookup da 1a chamada nulo
        var contaId = await SeedCartaoAsync();
        SeedFatura(contaId, new DateOnly(2026, 10, 20), StatusFatura.Aberta);
        await _db.SaveChangesAsync();
        var ciclo = CriarCiclo(lookupsNulos: 1);

        // Act
        await ciclo.ResolverFaturaParaLancamentoAsync(contaId, new DateOnly(2026, 10, 20));

        // Assert
        _db.ChangeTracker.Clear();
        var faturas = await _db.Faturas.Where(f => f.ContaId == contaId).ToListAsync();
        Assert.True(faturas.Count(f => f.Status == StatusFatura.Aberta) <= 1);
        Assert.Single(faturas, f => f.DataFechamento == new DateOnly(2026, 10, 20));
    }

    #endregion

    #region Helpers

    private FaturaCicloService CriarCiclo(int lookupsNulos = 0, bool sempreNulo = false)
    {
        var contaRepository = new ContaRepository(_db);
        var faturaRepository = new RepositorioComLookupFalso(
            new FaturaRepository(_db),
            lookupsNulos,
            sempreNulo);

        return new FaturaCicloService(faturaRepository, contaRepository, _relogio);
    }

    private async Task<Guid> SeedCartaoAsync()
    {
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Cartao",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 20,
            DiaVencimento = 27,
            Ativa = true
        };
        _db.Contas.Add(conta);
        await _db.SaveChangesAsync();
        return conta.Id;
    }

    private Fatura SeedFatura(Guid contaId, DateOnly dataFechamento, StatusFatura status)
    {
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = dataFechamento,
            DataVencimento = dataFechamento.AddDays(7),
            Status = status
        };
        _db.Faturas.Add(fatura);
        return fatura;
    }

    private async Task<int> ContarFaturasAsync(Guid contaId, DateOnly dataFechamento)
    {
        _db.ChangeTracker.Clear();
        return await _db.Faturas.CountAsync(f => f.ContaId == contaId && f.DataFechamento == dataFechamento);
    }

    // Decorator: os primeiros `lookupsNulos` (ou todos, com sempreNulo) lookups por
    // data de fechamento devolvem null, simulando a outra requisicao que ainda nao gravou.
    private sealed class RepositorioComLookupFalso : IFaturaRepository
    {
        private readonly IFaturaRepository _interno;
        private readonly bool _sempreNulo;
        private int _lookupsNulos;

        public RepositorioComLookupFalso(IFaturaRepository interno, int lookupsNulos, bool sempreNulo)
        {
            _interno = interno;
            _lookupsNulos = lookupsNulos;
            _sempreNulo = sempreNulo;
        }

        public Task<Fatura?> ObterPorContaEDataFechamento(Guid contaId, DateOnly dataFechamento)
        {
            if (_sempreNulo || _lookupsNulos > 0)
            {
                _lookupsNulos--;
                return Task.FromResult<Fatura?>(null);
            }

            return _interno.ObterPorContaEDataFechamento(contaId, dataFechamento);
        }

        public Task Adicionar(Fatura fatura) => _interno.Adicionar(fatura);
        public Task<Fatura?> ObterPorId(Guid id) => _interno.ObterPorId(id);
        public Task<IEnumerable<Fatura>> ListarPorConta(Guid contaId) => _interno.ListarPorConta(contaId);
        public Task<Fatura?> ObterFaturaAbertaPorConta(Guid contaId) => _interno.ObterFaturaAbertaPorConta(contaId);
        public Task<IReadOnlyList<Fatura>> ListarParaReconciliacao(Guid contaId, DateOnly hoje) => _interno.ListarParaReconciliacao(contaId, hoje);
        public Task<IEnumerable<Fatura>> ListarFaturasCartaoPorVencimentoNoMes(int ano, int mes) => _interno.ListarFaturasCartaoPorVencimentoNoMes(ano, mes);
        public Task Atualizar(Fatura fatura) => _interno.Atualizar(fatura);
        public Task Salvar() => _interno.Salvar();
        public void Descartar(Fatura fatura) => _interno.Descartar(fatura);
    }

    #endregion
}
