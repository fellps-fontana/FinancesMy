using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Repositories;
using MyFinances.Services;
using MyFinances.Tests.Helpers;
using Xunit;

namespace MyFinances.Tests.Services;

// TASK-238 / item 12 "Ciclo e status da fatura": maquina de estado da fatura em integracao.
// SQLite em memoria, um banco por teste, relogio fixo em 2026-10-13 (RelogioFake).
// Cartao com dia_fechamento 20 e vencimento 27: janela do ciclo corrente = 13/10 a 20/10.
public class FaturaCicloEstadoIntegrationTests : IAsyncLifetime
{
    private static readonly DateOnly Hoje = new(2026, 10, 13);
    private static readonly DateOnly FechOut = new(2026, 10, 20);
    private static readonly DateOnly FechNov = new(2026, 11, 20);
    private static readonly DateOnly FechSet = new(2026, 9, 20);

    private SqliteConnection _connection = null!;
    private MyFinancesDbContext _db = null!;
    private RelogioFake _relogio = null!;
    private IContaRepository _contaRepo = null!;
    private IFaturaRepository _faturaRepo = null!;
    private ILancamentoRepository _lancamentoRepo = null!;
    private FaturaCicloService _ciclo = null!;
    private CompraCartaoService _compras = null!;
    private EstornoCartaoService _estornos = null!;
    private AssinaturaCartaoService _assinaturas = null!;

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
        _contaRepo = new ContaRepository(_db);
        _faturaRepo = new FaturaRepository(_db);
        _lancamentoRepo = new LancamentoRepository(_db);
        var validacao = new ValidacaoCartaoService(_contaRepo);

        _ciclo = new FaturaCicloService(_faturaRepo, _contaRepo, _relogio);
        _compras = new CompraCartaoService(_lancamentoRepo, _ciclo, validacao);
        _estornos = new EstornoCartaoService(_lancamentoRepo, _ciclo, validacao);
        _assinaturas = new AssinaturaCartaoService(
            new AssinaturaCartaoRepository(_db),
            _contaRepo,
            _lancamentoRepo,
            _ciclo);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.CloseAsync();
        _connection.Dispose();
    }

    #region 6. Compra 8x a partir de nov/26

    [Fact]
    public async Task CompraOitoVezes_ApenasCicloAtualPodeSerAberta_NenhumaFechadaIndevida()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();

        // Act - 8 parcelas mensais a partir de 05/11/2026
        for (var i = 0; i < 8; i++)
        {
            var data = new DateOnly(2026, 11, 5).AddMonths(i);
            var resultado = await _compras.CriarCompraAsync(contaId, Requisicao(data, 100m));
            Assert.True(resultado.Sucesso, resultado.Erro);
        }

        // Assert
        var faturas = await FaturasAsync(contaId);
        Assert.Equal(8, faturas.Count);
        Assert.DoesNotContain(faturas, f => f.Status == StatusFatura.Fechada);
        Assert.True(faturas.Count(f => f.Status == StatusFatura.Aberta) <= 1);
        Assert.All(faturas, f => Assert.True(f.DataFechamento > FechOut));
        Assert.All(faturas, f => Assert.Equal(StatusFatura.Futura, f.Status));
    }

    #endregion

    #region 7. Compra de hoje feita depois da parcelada cai no ciclo atual

    [Fact]
    public async Task CompraDeHojeDepoisDaParcelada_CaiNaFaturaDoCicloAtualComoAberta()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        for (var i = 0; i < 8; i++)
        {
            await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 11, 5).AddMonths(i), 100m));
        }

        // Act
        var resultado = await _compras.CriarCompraAsync(contaId, Requisicao(Hoje, 50m));

        // Assert
        Assert.True(resultado.Sucesso, resultado.Erro);
        var faturas = await FaturasAsync(contaId);
        var doCiclo = faturas.Single(f => f.DataFechamento == FechOut);
        Assert.Equal(StatusFatura.Aberta, doCiclo.Status);
        Assert.Equal(doCiclo.Id, resultado.Compra!.FaturaId);
        Assert.Equal(1, faturas.Count(f => f.Status == StatusFatura.Aberta));
    }

    #endregion

    #region 8. Compra no dia exato do fechamento

    [Fact]
    public async Task CompraNoDiaDoFechamento_EntraNaFaturaQueFechaHoje_SemCicloDuplicado()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();

        // Act
        var primeira = await _compras.CriarCompraAsync(contaId, Requisicao(FechOut, 100m));
        var segunda = await _compras.CriarCompraAsync(contaId, Requisicao(FechOut, 40m));

        // Assert
        Assert.True(primeira.Sucesso, primeira.Erro);
        Assert.True(segunda.Sucesso, segunda.Erro);
        var faturas = await FaturasAsync(contaId);
        Assert.Single(faturas.Where(f => f.DataFechamento == FechOut));
        Assert.Equal(StatusFatura.Aberta, faturas.Single(f => f.DataFechamento == FechOut).Status);
    }

    #endregion

    #region 9. Mesma data duas vezes cai na mesma fatura

    [Fact]
    public async Task MesmaDataDuasVezes_CaiNaMesmaFatura()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        var data = new DateOnly(2026, 10, 15);

        // Act
        var primeira = await _compras.CriarCompraAsync(contaId, Requisicao(data, 100m));
        var segunda = await _compras.CriarCompraAsync(contaId, Requisicao(data, 100m));

        // Assert
        Assert.True(primeira.Sucesso, primeira.Erro);
        Assert.True(segunda.Sucesso, segunda.Erro);
        Assert.Equal(primeira.Compra!.FaturaId, segunda.Compra!.FaturaId);
        Assert.Single(await FaturasAsync(contaId));
    }

    #endregion

    #region 10. Lancamento futuro nao muda o status de outra fatura

    [Fact]
    public async Task LancamentoFuturo_NaoMudaStatusDaFaturaAtual()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 10, 15), 100m));

        // Act
        var resultado = await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 11, 5), 100m));

        // Assert
        Assert.True(resultado.Sucesso, resultado.Erro);
        var faturas = await FaturasAsync(contaId);
        Assert.Equal(StatusFatura.Aberta, faturas.Single(f => f.DataFechamento == FechOut).Status);
        Assert.Equal(StatusFatura.Futura, faturas.Single(f => f.DataFechamento == FechNov).Status);
    }

    #endregion

    #region 11. Avancar o relogio alem do fechamento reclassifica na primeira leitura

    [Fact]
    public async Task AvancarRelogioAlemDoFechamento_ReconciliarNaPrimeiraLeitura_ReclassificaAtual()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 10, 15), 100m));
        await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 11, 5), 100m));
        _relogio.Avancar(TimeSpan.FromDays(8)); // hoje = 21/10/2026

        // Act
        var atual = await _ciclo.ObterFaturaAtualAsync(contaId);

        // Assert
        Assert.NotNull(atual);
        Assert.Equal(FechNov, atual.DataFechamento);
        Assert.Equal(StatusFatura.Aberta, atual.Status);
        var faturas = await FaturasAsync(contaId);
        Assert.Equal(StatusFatura.Fechada, faturas.Single(f => f.DataFechamento == FechOut).Status);
    }

    [Fact]
    public async Task AvancarRelogioAlemDoFechamento_ResolverNaPrimeiraLeitura_ReclassificaAntesDeEscolher()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 10, 15), 100m));
        await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 11, 5), 100m));
        _relogio.Avancar(TimeSpan.FromDays(8));

        // Act - lancamento de 21/10 cai no ciclo de 20/11
        var (fatura, rejeitada, motivo) = await _ciclo.ResolverFaturaParaLancamentoAsync(
            contaId, new DateOnly(2026, 10, 21));

        // Assert
        Assert.False(rejeitada, motivo);
        Assert.NotNull(fatura);
        var nov = (await FaturasAsync(contaId)).Single(f => f.DataFechamento == FechNov);
        Assert.Equal(nov.Id, fatura.Id);
        Assert.Equal(StatusFatura.Aberta, fatura.Status);
        Assert.Equal(StatusFatura.Fechada, (await FaturasAsync(contaId)).Single(f => f.DataFechamento == FechOut).Status);
    }

    #endregion

    #region 12. Retroativa em ciclo FECHADA entra e a fatura continua FECHADA

    [Fact]
    public async Task CompraRetroativaEmCicloFechada_EntraSemReabrirAFatura()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        var fechada = SeedFatura(contaId, FechSet, StatusFatura.Fechada);
        await _db.SaveChangesAsync();

        // Act
        var resultado = await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 9, 15), 120m));

        // Assert
        Assert.True(resultado.Sucesso, resultado.Erro);
        Assert.Equal(fechada.Id, resultado.Compra!.FaturaId);
        var depois = (await FaturasAsync(contaId)).Single(f => f.Id == fechada.Id);
        Assert.Equal(StatusFatura.Fechada, depois.Status);
        Assert.Equal(120m, await SomaDosLancamentosAsync(fechada.Id));
    }

    #endregion

    #region 13. Retroativa em ciclo PAGA encerrado e rejeitada e nada e gravado

    [Fact]
    public async Task CompraAvulsaRetroativaEmCicloPagoEncerrado_EhRejeitadaSemGravarNada()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        var paga = SeedFatura(contaId, FechSet, StatusFatura.Paga);
        await _db.SaveChangesAsync();

        // Act
        var resultado = await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 9, 15), 120m));

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Equal(FaturaCicloPolitica.ErroCicloPagoEncerrado, resultado.Erro);
        Assert.Equal(0, await ContarLancamentosAsync(contaId));
        Assert.Equal(StatusFatura.Paga, (await FaturasAsync(contaId)).Single(f => f.Id == paga.Id).Status);
        Assert.Single(await FaturasAsync(contaId));
    }

    [Fact]
    public async Task EdicaoDeDataDeCompraParaCicloPagoEncerrado_EhRejeitadaENaoAlteraACompra()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        var criada = await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 10, 15), 100m));
        Assert.True(criada.Sucesso, criada.Erro);
        SeedFatura(contaId, FechSet, StatusFatura.Paga);
        await _db.SaveChangesAsync();

        // Act
        var resultado = await _compras.EditarCompraAsync(contaId, criada.Compra!.Id, new EditarCompraRequest
        {
            Descricao = "Compra",
            Valor = 100m,
            Data = new DateOnly(2026, 9, 15)
        });

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Equal(FaturaCicloPolitica.ErroCicloPagoEncerrado, resultado.Erro);
        _db.ChangeTracker.Clear();
        var compra = await _db.Lancamentos.FirstAsync(l => l.Id == criada.Compra.Id);
        Assert.Equal(new DateOnly(2026, 10, 15), compra.Data);
    }

    [Fact]
    public async Task EstornoAvulsoEmCicloPagoEncerrado_EhRejeitadoSemGravarEstorno()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        var criada = await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 10, 15), 100m));
        Assert.True(criada.Sucesso, criada.Erro);
        SeedFatura(contaId, FechSet, StatusFatura.Paga);
        await _db.SaveChangesAsync();
        var antes = await ContarLancamentosAsync(contaId);

        // Act
        var resultado = await _estornos.CriarEstornoAsync(contaId, new CriarEstornoRequest
        {
            CompraId = criada.Compra!.Id,
            Motivo = "Devolucao",
            Data = new DateOnly(2026, 9, 15)
        });

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Equal(FaturaCicloPolitica.ErroCicloPagoEncerrado, resultado.Erro);
        Assert.Equal(antes, await ContarLancamentosAsync(contaId));
    }

    [Fact]
    public async Task AssinaturaComCompetenciaEmCicloPagoEncerrado_NaoGeraCompra()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        SeedFatura(contaId, FechSet, StatusFatura.Paga);
        var assinatura = new AssinaturaCartao
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Descricao = "Streaming",
            Valor = 39.90m,
            DiaReferencia = 5,
            Ativa = true
        };
        _db.AssinaturasCartao.Add(assinatura);
        await _db.SaveChangesAsync();

        // Act
        await _assinaturas.GarantirAssinaturasDoCicloAsync(contaId, 2026, 9);

        // Assert
        Assert.Equal(0, await ContarLancamentosAsync(contaId));
    }

    #endregion

    #region 14. Compra em fatura PAGA de ciclo ainda nao encerrado entra e reabre

    [Fact]
    public async Task CompraEmPagaDoCicloAtualAindaNaoEncerrado_EntraEReabreComoAberta()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        var paga = SeedFatura(contaId, FechOut, StatusFatura.Paga);
        await _db.SaveChangesAsync();

        // Act
        var resultado = await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 10, 15), 80m));

        // Assert
        Assert.True(resultado.Sucesso, resultado.Erro);
        var faturas = await FaturasAsync(contaId);
        Assert.Single(faturas.Where(f => f.DataFechamento == FechOut));
        Assert.Equal(paga.Id, resultado.Compra!.FaturaId);
        Assert.Equal(StatusFatura.Aberta, faturas.Single(f => f.Id == paga.Id).Status);
    }

    [Fact]
    public async Task CompraEmPagaDeCicloFuturoAindaNaoEncerrado_EntraEReabreComoFutura()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        var paga = SeedFatura(contaId, FechNov, StatusFatura.Paga);
        await _db.SaveChangesAsync();

        // Act
        var resultado = await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 11, 5), 80m));

        // Assert
        Assert.True(resultado.Sucesso, resultado.Erro);
        var faturas = await FaturasAsync(contaId);
        Assert.Single(faturas.Where(f => f.DataFechamento == FechNov));
        Assert.Equal(StatusFatura.Futura, faturas.Single(f => f.Id == paga.Id).Status);
    }

    #endregion

    #region 15. Nunca existem 2 ABERTA, inclusive depois de reabrir

    [Fact]
    public async Task ReabrirPagaAtual_NaoDeixaDuasAbertas()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        SeedFatura(contaId, FechOut, StatusFatura.Paga);
        await _db.SaveChangesAsync();

        // Act
        await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 10, 15), 80m));
        await _ciclo.ReconciliarAsync(contaId);

        // Assert
        var faturas = await FaturasAsync(contaId);
        Assert.True(faturas.Count(f => f.Status == StatusFatura.Aberta) <= 1);
    }

    [Fact]
    public async Task ReabrirPagaDeCicloFuturo_QuandoJaHaAtualAberta_NaoCriaSegundaAberta()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 10, 15), 100m));
        SeedFatura(contaId, FechNov, StatusFatura.Paga);
        await _db.SaveChangesAsync();

        // Act
        var resultado = await _compras.CriarCompraAsync(contaId, Requisicao(new DateOnly(2026, 11, 5), 80m));

        // Assert
        Assert.True(resultado.Sucesso, resultado.Erro);
        var faturas = await FaturasAsync(contaId);
        Assert.Equal(1, faturas.Count(f => f.Status == StatusFatura.Aberta));
        Assert.Equal(StatusFatura.Aberta, faturas.Single(f => f.DataFechamento == FechOut).Status);
        Assert.Equal(StatusFatura.Futura, faturas.Single(f => f.DataFechamento == FechNov).Status);
    }

    #endregion

    #region 16. ReconciliarAsync e idempotente e no-op para Banco ou conta inexistente

    [Fact]
    public async Task ReconciliarAsync_CartaoAplicadoDuasVezes_SegundaNaoMudaNada()
    {
        // Arrange
        var contaId = await SeedCartaoAsync();
        SeedFatura(contaId, FechSet, StatusFatura.Aberta);
        SeedFatura(contaId, FechOut, StatusFatura.Futura);
        await _db.SaveChangesAsync();

        // Act
        await _ciclo.ReconciliarAsync(contaId);
        var primeira = await FaturasAsync(contaId);
        await _ciclo.ReconciliarAsync(contaId);
        var segunda = await FaturasAsync(contaId);

        // Assert
        Assert.Equal(StatusFatura.Fechada, primeira.Single(f => f.DataFechamento == FechSet).Status);
        Assert.Equal(StatusFatura.Aberta, primeira.Single(f => f.DataFechamento == FechOut).Status);
        Assert.Equal(
            primeira.Select(f => (f.Id, f.Status)),
            segunda.Select(f => (f.Id, f.Status)));
    }

    [Fact]
    public async Task ReconciliarAsync_ContaBanco_NaoAlteraFaturas()
    {
        // Arrange
        var bancoId = Guid.NewGuid();
        _db.Contas.Add(new Conta
        {
            Id = bancoId,
            Nome = "Banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        });
        SeedFatura(bancoId, FechSet, StatusFatura.Aberta);
        await _db.SaveChangesAsync();

        // Act
        await _ciclo.ReconciliarAsync(bancoId);

        // Assert
        Assert.Equal(StatusFatura.Aberta, (await FaturasAsync(bancoId)).Single().Status);
    }

    [Fact]
    public async Task ReconciliarAsync_ContaInexistente_NaoLancaExcecao()
    {
        // Act
        var excecao = await Record.ExceptionAsync(() => _ciclo.ReconciliarAsync(Guid.NewGuid()));

        // Assert
        Assert.Null(excecao);
    }

    #endregion

    #region Helpers

    private static CriarCompraRequest Requisicao(DateOnly data, decimal valor) => new()
    {
        Descricao = "Compra",
        Valor = valor,
        Data = data
    };

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

    private async Task<List<Fatura>> FaturasAsync(Guid contaId)
    {
        _db.ChangeTracker.Clear();
        return await _db.Faturas
            .Where(f => f.ContaId == contaId)
            .OrderBy(f => f.DataFechamento)
            .ToListAsync();
    }

    private async Task<int> ContarLancamentosAsync(Guid contaId)
    {
        _db.ChangeTracker.Clear();
        return await _db.Lancamentos.CountAsync(l => l.ContaId == contaId);
    }

    private async Task<decimal> SomaDosLancamentosAsync(Guid faturaId)
    {
        _db.ChangeTracker.Clear();
        return await _db.Lancamentos
            .Where(l => l.FaturaId == faturaId)
            .SumAsync(l => l.Valor);
    }

    #endregion
}
