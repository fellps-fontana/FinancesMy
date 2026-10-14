using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

// TASK-238 / item 12 "Pagamento x fatura" com estados FUTURA (integracao SQLite).
// Pagamento parcial mantem o status; quitacao vira PAGA. A proxima fatura so vira ABERTA na virada do calendario.
public class PagamentoFaturaCicloTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private MyFinancesDbContext _db = null!;
    private PagamentoFaturaService _service = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new MyFinancesDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        var faturaRepository = new FaturaRepository(_db);
        _service = new PagamentoFaturaService(
            faturaRepository,
            new TransferenciaRepository(_db),
            new LancamentoRepository(_db),
            new ContaRepository(_db),
            new FaturaCreditoService(faturaRepository));
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.CloseAsync();
        _connection.Dispose();
    }

    #region 24. Fatura FUTURA com pagamento parcial continua FUTURA; quitada vira PAGA

    [Fact]
    public async Task PagarFuturaParcialmente_MantemFutura_QuitarViraPaga()
    {
        // Arrange
        var (cartaoId, origemId) = await SeedContasAsync();
        var futura = SeedFaturaComLancamento(cartaoId, new DateOnly(2026, 11, 20), StatusFatura.Futura, 500m);
        await _db.SaveChangesAsync();

        // Act 1 - pagamento parcial
        var parcial = await _service.PagarFaturaAsync(futura.Id, Pagamento(origemId, 200m));

        // Assert 1
        Assert.True(parcial.Sucesso, parcial.Erro);
        Assert.Equal(StatusFatura.Futura, (await FaturaAsync(futura.Id)).Status);

        // Act 2 - quita o saldo restante
        var quitacao = await _service.PagarFaturaAsync(futura.Id, Pagamento(origemId, 300m));

        // Assert 2
        Assert.True(quitacao.Sucesso, quitacao.Erro);
        Assert.Equal(StatusFatura.Paga, (await FaturaAsync(futura.Id)).Status);
    }

    #endregion

    #region 25. Atual quitada antes do fechamento vira PAGA; a proxima continua FUTURA ate o fechamento

    [Fact]
    public async Task QuitarAtualAntesDoFechamento_ViraPaga_ProximaContinuaFutura()
    {
        // Arrange
        var (cartaoId, origemId) = await SeedContasAsync();
        var atual = SeedFaturaComLancamento(cartaoId, new DateOnly(2026, 10, 20), StatusFatura.Aberta, 500m);
        var proxima = SeedFaturaComLancamento(cartaoId, new DateOnly(2026, 11, 20), StatusFatura.Futura, 300m);
        await _db.SaveChangesAsync();

        // Act
        var resultado = await _service.PagarFaturaAsync(atual.Id, Pagamento(origemId, 500m));

        // Assert
        Assert.True(resultado.Sucesso, resultado.Erro);
        Assert.Equal(StatusFatura.Paga, (await FaturaAsync(atual.Id)).Status);
        Assert.Equal(StatusFatura.Futura, (await FaturaAsync(proxima.Id)).Status);
    }

    #endregion

    #region Helpers

    private static PagarFaturaRequest Pagamento(Guid origemId, decimal valor) => new()
    {
        Valor = valor,
        Data = new DateOnly(2026, 10, 13),
        ContaOrigemId = origemId
    };

    private async Task<(Guid CartaoId, Guid OrigemId)> SeedContasAsync()
    {
        var cartaoId = Guid.NewGuid();
        var origemId = Guid.NewGuid();
        _db.Contas.Add(new Conta
        {
            Id = cartaoId,
            Nome = "Cartao",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 20,
            DiaVencimento = 27,
            Ativa = true
        });
        _db.Contas.Add(new Conta
        {
            Id = origemId,
            Nome = "Banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            SaldoManual = 10000m,
            Ativa = true
        });
        await _db.SaveChangesAsync();
        return (cartaoId, origemId);
    }

    private Fatura SeedFaturaComLancamento(Guid contaId, DateOnly fechamento, StatusFatura status, decimal valor)
    {
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = fechamento,
            DataVencimento = fechamento.AddDays(7),
            Status = status
        };
        _db.Faturas.Add(fatura);
        _db.Lancamentos.Add(new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Descricao = "Compra",
            Valor = valor,
            Tipo = TipoLancamento.Debit,
            Data = fechamento.AddDays(-1),
            Status = StatusLancamento.Pendente,
            FaturaId = fatura.Id
        });
        return fatura;
    }

    private async Task<Fatura> FaturaAsync(Guid faturaId)
    {
        _db.ChangeTracker.Clear();
        return await _db.Faturas.FirstAsync(f => f.Id == faturaId);
    }

    #endregion
}
