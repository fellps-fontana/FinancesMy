using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using MyFinances.Tests.Helpers;
using Xunit;

namespace MyFinances.Tests.Services;

// TASK-238 / item 12 "Assinatura de cartao" x "Ciclo e status da fatura" (integracao SQLite, relogio em 2026-10-13).
// Edicao propaga para ABERTA e FUTURA (nunca FECHADA/PAGA), reconciliando antes.
public class AssinaturaCicloTests : IAsyncLifetime
{
    private static readonly DateOnly Hoje = new(2026, 10, 13);

    private SqliteConnection _connection = null!;
    private MyFinancesDbContext _db = null!;
    private AssinaturaCartaoService _service = null!;
    private Guid _contaId;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new MyFinancesDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        var contaRepository = new ContaRepository(_db);
        var faturaRepository = new FaturaRepository(_db);
        var lancamentoRepository = new LancamentoRepository(_db);
        var ciclo = new FaturaCicloService(faturaRepository, contaRepository, new RelogioFake(Hoje));

        _service = new AssinaturaCartaoService(
            new AssinaturaCartaoRepository(_db),
            contaRepository,
            lancamentoRepository,
            ciclo);

        _contaId = Guid.NewGuid();
        _db.Contas.Add(new Conta
        {
            Id = _contaId,
            Nome = "Cartao",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 20,
            DiaVencimento = 27,
            Ativa = true
        });
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.CloseAsync();
        _connection.Dispose();
    }

    #region 21. Edicao propaga para ABERTA e FUTURA, nao para FECHADA nem PAGA

    [Fact]
    public async Task EditarAssinatura_PropagaParaAbertaEFutura_NaoPropagaParaFechadaNemPaga()
    {
        // Arrange
        var assinatura = SeedAssinatura();
        var paga = SeedFaturaComCompra(assinatura, new DateOnly(2026, 8, 20), StatusFatura.Paga, new DateOnly(2026, 8, 5));
        var fechada = SeedFaturaComCompra(assinatura, new DateOnly(2026, 9, 20), StatusFatura.Fechada, new DateOnly(2026, 9, 5));
        var aberta = SeedFaturaComCompra(assinatura, new DateOnly(2026, 10, 20), StatusFatura.Aberta, new DateOnly(2026, 10, 5));
        var futura = SeedFaturaComCompra(assinatura, new DateOnly(2026, 11, 20), StatusFatura.Futura, new DateOnly(2026, 11, 5));
        await _db.SaveChangesAsync();

        // Act
        await _service.EditarAsync(assinatura.Id, "Netflix 4K", 55.90m, 15, null);

        // Assert
        _db.ChangeTracker.Clear();
        var compras = await _db.Lancamentos.Where(l => l.AssinaturaCartaoId == assinatura.Id).ToListAsync();

        var compraAberta = compras.Single(l => l.FaturaId == aberta.Id);
        Assert.Equal(55.90m, compraAberta.Valor);
        Assert.Equal(new DateOnly(2026, 10, 15), compraAberta.Data);

        var compraFutura = compras.Single(l => l.FaturaId == futura.Id);
        Assert.Equal(55.90m, compraFutura.Valor);
        Assert.Equal(new DateOnly(2026, 11, 15), compraFutura.Data);

        Assert.Equal(39.90m, compras.Single(l => l.FaturaId == fechada.Id).Valor);
        Assert.Equal(new DateOnly(2026, 9, 5), compras.Single(l => l.FaturaId == fechada.Id).Data);
        Assert.Equal(39.90m, compras.Single(l => l.FaturaId == paga.Id).Valor);
        Assert.Equal(new DateOnly(2026, 8, 5), compras.Single(l => l.FaturaId == paga.Id).Data);
    }

    #endregion

    #region 22. Reconcilia antes de propagar

    [Fact]
    public async Task EditarAssinatura_ReconciliaAntesDePropagar_FaturaVencidaNaoRecebeEdicao()
    {
        // Arrange - set/20 esta ABERTA mas vencida; out/20 e FUTURA e vira a atual apos reconciliar
        var assinatura = SeedAssinatura();
        var vencida = SeedFaturaComCompra(assinatura, new DateOnly(2026, 9, 20), StatusFatura.Aberta, new DateOnly(2026, 9, 5));
        var atual = SeedFaturaComCompra(assinatura, new DateOnly(2026, 10, 20), StatusFatura.Futura, new DateOnly(2026, 10, 5));
        await _db.SaveChangesAsync();

        // Act
        await _service.EditarAsync(assinatura.Id, "Netflix 4K", 55.90m, 15, null);

        // Assert
        _db.ChangeTracker.Clear();
        Assert.Equal(StatusFatura.Fechada, (await _db.Faturas.FirstAsync(f => f.Id == vencida.Id)).Status);
        Assert.Equal(StatusFatura.Aberta, (await _db.Faturas.FirstAsync(f => f.Id == atual.Id)).Status);

        var compraVencida = await _db.Lancamentos.FirstAsync(l => l.FaturaId == vencida.Id);
        Assert.Equal(39.90m, compraVencida.Valor);
        Assert.Equal(new DateOnly(2026, 9, 5), compraVencida.Data);

        var compraAtual = await _db.Lancamentos.FirstAsync(l => l.FaturaId == atual.Id);
        Assert.Equal(55.90m, compraAtual.Valor);
    }

    #endregion

    #region 23. Geracao em mes futuro nao fecha nada

    [Fact]
    public async Task GarantirCicloDeMesFuturo_NaoFechaAFaturaAtual()
    {
        // Arrange
        var assinatura = SeedAssinatura();
        var atual = SeedFaturaComCompra(assinatura, new DateOnly(2026, 10, 20), StatusFatura.Aberta, new DateOnly(2026, 10, 5));
        await _db.SaveChangesAsync();

        // Act
        await _service.GarantirAssinaturasDoCicloAsync(_contaId, 2026, 12);

        // Assert
        _db.ChangeTracker.Clear();
        Assert.Equal(StatusFatura.Aberta, (await _db.Faturas.FirstAsync(f => f.Id == atual.Id)).Status);
        var dezembro = await _db.Faturas.SingleAsync(f => f.ContaId == _contaId && f.DataFechamento == new DateOnly(2026, 12, 20));
        Assert.Equal(StatusFatura.Futura, dezembro.Status);
    }

    #endregion

    #region Helpers

    private AssinaturaCartao SeedAssinatura()
    {
        var assinatura = new AssinaturaCartao
        {
            Id = Guid.NewGuid(),
            ContaId = _contaId,
            Descricao = "Netflix",
            Valor = 39.90m,
            DiaReferencia = 5,
            Ativa = true
        };
        _db.AssinaturasCartao.Add(assinatura);
        return assinatura;
    }

    private Fatura SeedFaturaComCompra(AssinaturaCartao assinatura, DateOnly fechamento, StatusFatura status, DateOnly dataCompra)
    {
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = _contaId,
            DataFechamento = fechamento,
            DataVencimento = fechamento.AddDays(7),
            Status = status
        };
        _db.Faturas.Add(fatura);

        _db.Lancamentos.Add(new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = _contaId,
            AssinaturaCartaoId = assinatura.Id,
            Descricao = assinatura.Descricao,
            Valor = assinatura.Valor,
            Tipo = TipoLancamento.Debit,
            Data = dataCompra,
            Status = StatusLancamento.Pendente,
            FaturaId = fatura.Id
        });

        return fatura;
    }

    #endregion
}
