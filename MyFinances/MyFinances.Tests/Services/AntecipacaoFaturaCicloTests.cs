using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using MyFinances.Tests.Helpers;
using Xunit;

namespace MyFinances.Tests.Services;

// TASK-238 / item 12 "Antecipacao" x "Ciclo e status da fatura" (integracao SQLite, relogio em 2026-10-13).
// Usa o construtor de 6 argumentos (com FaturaCicloService): a fatura destino vem de ObterFaturaAtualAsync.
// Cenario: cartao 20/27. Fatura atual = 20/10/2026. Compra 8x com parcela k na fatura de nov/26 + (k-1) meses.
public class AntecipacaoFaturaCicloTests : IAsyncLifetime
{
    private static readonly DateOnly Hoje = new(2026, 10, 13);
    private static readonly DateOnly FechAtual = new(2026, 10, 20);
    private static readonly DateOnly PrimeiraFuturaNov = new(2026, 11, 20);

    private SqliteConnection _connection = null!;
    private MyFinancesDbContext _db = null!;
    private AntecipacaoParcelasService _service = null!;

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
        var ciclo = new FaturaCicloService(faturaRepository, contaRepository, new RelogioFake(Hoje));

        _service = new AntecipacaoParcelasService(
            new CompraParceladaRepository(_db),
            new LancamentoRepository(_db),
            new ValidacaoCartaoService(contaRepository),
            NullLogger<AntecipacaoParcelasService>.Instance,
            ciclo);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.CloseAsync();
        _connection.Dispose();
    }

    #region 17. Elegiveis sao as parcelas em FUTURA e o destino e a fatura atual

    [Fact]
    public async Task ObterElegiveis_ParcelasEmFuturaComAtualAberta_RetornaListaNaoVaziaEDestinoAtual()
    {
        // Arrange
        var cenario = await SeedCompraOitoVezesAsync(atualStatus: StatusFatura.Aberta, parcelasEmAtual: false);

        // Act
        var (sucesso, _, elegiveis, erro) = await _service.ObterElegiveisAsync(cenario.ContaId, cenario.CompraId);

        // Assert
        Assert.True(sucesso, erro);
        Assert.NotNull(elegiveis);
        Assert.Equal(8, elegiveis.Count);

        var resultado = await _service.AnteciparAsync(cenario.ContaId, cenario.CompraId, 2, new DateOnly(2026, 10, 13));
        Assert.True(resultado.Sucesso, resultado.Erro);
        Assert.Equal(cenario.AtualId, resultado.FaturaDestino!.Id);
    }

    #endregion

    #region 18. Atual PAGA antes do fechamento: antecipacao funciona e reabre a atual

    [Fact]
    public async Task Antecipar_ComAtualPagaAntesDoFechamento_ReabreAAtualEMoveParcelas()
    {
        // Arrange
        var cenario = await SeedCompraOitoVezesAsync(atualStatus: StatusFatura.Paga, parcelasEmAtual: false);

        // Act
        var resultado = await _service.AnteciparAsync(cenario.ContaId, cenario.CompraId, 2, new DateOnly(2026, 10, 13));

        // Assert
        Assert.True(resultado.Sucesso, resultado.Erro);
        Assert.Equal(cenario.AtualId, resultado.FaturaDestino!.Id);
        _db.ChangeTracker.Clear();
        var atual = await _db.Faturas.FirstAsync(f => f.Id == cenario.AtualId);
        Assert.Equal(StatusFatura.Aberta, atual.Status);
        Assert.Equal(2, await _db.Lancamentos.CountAsync(l => l.FaturaId == cenario.AtualId));
    }

    #endregion

    #region 19. Sem fatura atual, ErroSemFaturaAberta continua

    [Fact]
    public async Task Antecipar_SemFaturaAtual_RejeitaComErroSemFaturaAbertaESemCriarFatura()
    {
        // Arrange - so faturas futuras, nenhuma na janela do ciclo corrente
        var cenario = await SeedCompraOitoVezesAsync(atualStatus: null, parcelasEmAtual: false);
        var quantidadeAntes = await _db.Faturas.CountAsync(f => f.ContaId == cenario.ContaId);

        // Act
        var resultado = await _service.AnteciparAsync(cenario.ContaId, cenario.CompraId, 2, new DateOnly(2026, 10, 13));

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Equal("Nao existe fatura aberta para a conta de cartao", resultado.Erro);
        Assert.Equal(quantidadeAntes, await _db.Faturas.CountAsync(f => f.ContaId == cenario.ContaId));
    }

    #endregion

    #region 20. A reconciliacao roda antes dos elegiveis

    [Fact]
    public async Task ObterElegiveis_ReconciliaAntesDeSelecionar_ParcelaDaAtualNaoEhElegivel()
    {
        // Arrange - Set/20 esta ABERTA e vencida; Out/20 e FUTURA e vira a atual apos reconciliar.
        var cenario = await SeedCompraOitoVezesAsync(atualStatus: null, parcelasEmAtual: true);
        var setembro = SeedFatura(cenario.ContaId, new DateOnly(2026, 9, 20), StatusFatura.Aberta);
        await _db.SaveChangesAsync();

        // Act
        var (sucesso, _, elegiveis, erro) = await _service.ObterElegiveisAsync(cenario.ContaId, cenario.CompraId);

        // Assert
        Assert.True(sucesso, erro);
        Assert.NotNull(elegiveis);
        Assert.DoesNotContain(elegiveis, l => l.FaturaId == cenario.AtualId);
        Assert.Equal(7, elegiveis.Count);
        _db.ChangeTracker.Clear();
        Assert.Equal(StatusFatura.Fechada, (await _db.Faturas.FirstAsync(f => f.Id == setembro.Id)).Status);
    }

    #endregion

    #region Fatura PAGA posterior a atual nao e elegivel para antecipacao

    [Fact]
    public async Task Antecipar_ComFuturaDoMeioPaga_NaoMoveAParcelaDessaFatura()
    {
        // Arrange - atual out/20 ABERTA; parcelas 1..8 em nov..jun (FUTURA).
        // A fatura da parcela 4 (fev/27) fica PAGA: sobram 7 elegiveis (1,2,3,5,6,7,8).
        var cenario = await SeedCompraOitoVezesAsync(atualStatus: StatusFatura.Aberta, parcelasEmAtual: false);
        var parcela4 = await _db.Lancamentos
            .Include(l => l.Fatura)
            .FirstAsync(l => l.CompraParceladaId == cenario.CompraId && l.ParcelaNumero == 4);
        var faturaPagaId = parcela4.FaturaId!.Value;
        parcela4.Fatura!.Status = StatusFatura.Paga;
        await _db.SaveChangesAsync();

        // Act - pedir 7 move 8..1 sem a parcela 4
        var resultado = await _service.AnteciparAsync(cenario.ContaId, cenario.CompraId, 7, new DateOnly(2026, 10, 13));

        // Assert
        Assert.True(resultado.Sucesso, resultado.Erro);
        _db.ChangeTracker.Clear();
        var parcela4Depois = await _db.Lancamentos
            .FirstAsync(l => l.CompraParceladaId == cenario.CompraId && l.ParcelaNumero == 4);
        Assert.Equal(faturaPagaId, parcela4Depois.FaturaId);
    }

    #endregion

    #region Helpers

    private sealed record Cenario(Guid ContaId, Guid CompraId, Guid AtualId);

    // Compra 8x: parcela k (1..8) fica na fatura de nov/26 + (k-1) meses.
    // Se parcelasEmAtual, a parcela 1 fica na fatura de out/26 (atual) e as demais em nov..jun.
    // atualStatus null = nao cria fatura de out/26 (exceto quando parcelasEmAtual, que a cria como FUTURA).
    private async Task<Cenario> SeedCompraOitoVezesAsync(StatusFatura? atualStatus, bool parcelasEmAtual)
    {
        var contaId = Guid.NewGuid();
        _db.Contas.Add(new Conta
        {
            Id = contaId,
            Nome = "Cartao",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 20,
            DiaVencimento = 27,
            Ativa = true
        });

        var compraId = Guid.NewGuid();
        _db.ComprasParceladas.Add(new CompraParcelada
        {
            Id = compraId,
            Descricao = "Notebook",
            ValorTotal = 800m,
            QuantidadeParcelas = 8,
            DataCompra = new DateOnly(2026, 11, 5)
        });

        var atual = SeedFatura(contaId, FechAtual, atualStatus ?? StatusFatura.Futura);
        if (atualStatus == null && !parcelasEmAtual)
        {
            _db.Faturas.Remove(atual);
        }

        var faturasPorParcela = new Dictionary<int, Fatura>();
        for (var k = 1; k <= 8; k++)
        {
            if (k == 1 && parcelasEmAtual)
            {
                faturasPorParcela[k] = atual;
                continue;
            }

            var fechamento = PrimeiraFuturaNov.AddMonths(k - 1);
            faturasPorParcela[k] = SeedFatura(contaId, fechamento, StatusFatura.Futura);
        }

        await _db.SaveChangesAsync();

        for (var k = 1; k <= 8; k++)
        {
            var fatura = faturasPorParcela[k];
            _db.Lancamentos.Add(new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                CompraParceladaId = compraId,
                ParcelaNumero = k,
                Descricao = "Notebook",
                Valor = 100m,
                Tipo = TipoLancamento.Debit,
                Data = fatura.DataFechamento.AddDays(-1),
                Status = StatusLancamento.Pendente,
                FaturaId = fatura.Id
            });
        }

        await _db.SaveChangesAsync();

        return new Cenario(contaId, compraId, atual.Id);
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

    #endregion
}
