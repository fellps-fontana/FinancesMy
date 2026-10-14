using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs.Conta;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

// Item 10.2 x item 12: dias do cartao editados valem so para faturas criadas depois.
// Integracao autorizada no briefing: SQLite em memoria, repositorios e FaturaCicloService reais.
// xUnit cria uma instancia por teste, entao cada teste tem seu proprio banco.
public class ContaEdicaoFaturaIntegrationTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private MyFinancesDbContext _dbContext = null!;
    private ContaService _contaService = null!;
    private FaturaCicloService _faturaCicloService = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new MyFinancesDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        var contaRepository = new ContaRepository(_dbContext);
        var faturaRepository = new FaturaRepository(_dbContext);
        var ativoRepository = new AtivoRepository(_dbContext);

        _contaService = new ContaService(contaRepository, ativoRepository, NullLogger<ContaService>.Instance);
        _faturaCicloService = new FaturaCicloService(faturaRepository, contaRepository);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.CloseAsync();
        _connection.Dispose();
    }

    #region Faturas existentes nao mudam ao editar os dias

    [Fact]
    public async Task EditarDiasDoCartao_FaturaAbertaMantemDataFechamentoEVencimento()
    {
        // Arrange
        var contaId = await CriarCartaoAsync(diaFechamento: 10, diaVencimento: 20);
        var aberta = await ResolverCompraAsync(contaId, new DateOnly(2025, 1, 5));
        Assert.Equal(new DateOnly(2025, 1, 10), aberta.DataFechamento);
        Assert.Equal(new DateOnly(2025, 1, 20), aberta.DataVencimento);

        // Act
        await _contaService.EditarConta(contaId, new EditarContaRequest { Nome = "Cartao", DiaFechamento = 20, DiaVencimento = 27 });

        // Assert
        var depois = await ObterFaturaAsync(aberta.Id);
        Assert.Equal(StatusFatura.Aberta, depois.Status);
        Assert.Equal(new DateOnly(2025, 1, 10), depois.DataFechamento);
        Assert.Equal(new DateOnly(2025, 1, 20), depois.DataVencimento);
    }

    [Fact]
    public async Task EditarDiasDoCartao_FaturasFechadaEPagaNaoMudam()
    {
        // Arrange
        var contaId = await CriarCartaoAsync(diaFechamento: 10, diaVencimento: 20);
        var fechada = SeedFatura(contaId, StatusFatura.Fechada, new DateOnly(2024, 12, 10), new DateOnly(2024, 12, 20));
        var paga = SeedFatura(contaId, StatusFatura.Paga, new DateOnly(2024, 11, 10), new DateOnly(2024, 11, 20));
        await _dbContext.SaveChangesAsync();

        // Act
        await _contaService.EditarConta(contaId, new EditarContaRequest { Nome = "Cartao", DiaFechamento = 20, DiaVencimento = 27 });

        // Assert
        var fechadaDepois = await ObterFaturaAsync(fechada.Id);
        Assert.Equal(StatusFatura.Fechada, fechadaDepois.Status);
        Assert.Equal(new DateOnly(2024, 12, 10), fechadaDepois.DataFechamento);
        Assert.Equal(new DateOnly(2024, 12, 20), fechadaDepois.DataVencimento);

        var pagaDepois = await ObterFaturaAsync(paga.Id);
        Assert.Equal(StatusFatura.Paga, pagaDepois.Status);
        Assert.Equal(new DateOnly(2024, 11, 10), pagaDepois.DataFechamento);
        Assert.Equal(new DateOnly(2024, 11, 20), pagaDepois.DataVencimento);
    }

    [Fact]
    public async Task EditarDiasDoCartao_FaturaFuturaExistenteMantemDatas()
    {
        // Arrange
        var contaId = await CriarCartaoAsync(diaFechamento: 10, diaVencimento: 20);
        var futura = SeedFatura(contaId, StatusFatura.Aberta, new DateOnly(2025, 3, 10), new DateOnly(2025, 3, 20));
        await _dbContext.SaveChangesAsync();

        // Act
        await _contaService.EditarConta(contaId, new EditarContaRequest { Nome = "Cartao", DiaFechamento = 20, DiaVencimento = 27 });

        // Assert
        var depois = await ObterFaturaAsync(futura.Id);
        Assert.Equal(new DateOnly(2025, 3, 10), depois.DataFechamento);
        Assert.Equal(new DateOnly(2025, 3, 20), depois.DataVencimento);
    }

    [Fact]
    public async Task EditarDiasDoCartao_FaturaIdDasComprasExistentesNaoMuda()
    {
        // Arrange
        var contaId = await CriarCartaoAsync(diaFechamento: 10, diaVencimento: 20);
        var aberta = await ResolverCompraAsync(contaId, new DateOnly(2025, 1, 5));

        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Descricao = "Compra ja lancada",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 5),
            Status = StatusLancamento.Pendente,
            FaturaId = aberta.Id
        };
        _dbContext.Lancamentos.Add(compra);
        await _dbContext.SaveChangesAsync();

        // Act
        await _contaService.EditarConta(contaId, new EditarContaRequest { Nome = "Cartao", DiaFechamento = 20, DiaVencimento = 27 });

        // Assert
        _dbContext.ChangeTracker.Clear();
        var compraDepois = await _dbContext.Lancamentos.FirstAsync(l => l.Id == compra.Id);
        Assert.Equal(aberta.Id, compraDepois.FaturaId);
    }

    #endregion

    #region Fatura criada depois da edicao usa os dias novos

    [Fact]
    public async Task EditarDiasDoCartao_LancamentoDepoisDaEdicao_CriaNovaFaturaComDiasNovos()
    {
        // Arrange
        var contaId = await CriarCartaoAsync(diaFechamento: 10, diaVencimento: 20);
        var aberta = await ResolverCompraAsync(contaId, new DateOnly(2025, 1, 5));
        Assert.Equal(new DateOnly(2025, 1, 10), aberta.DataFechamento);

        await _contaService.EditarConta(contaId, new EditarContaRequest { Nome = "Cartao", DiaFechamento = 20, DiaVencimento = 27 });

        // Act - lancamento em 12/01 cai depois do fechamento da aberta (10/01), entao abre nova fatura
        var nova = await ResolverCompraAsync(contaId, new DateOnly(2025, 1, 12));

        // Assert
        Assert.NotEqual(aberta.Id, nova.Id);
        Assert.Equal(new DateOnly(2025, 1, 20), nova.DataFechamento);
        Assert.Equal(new DateOnly(2025, 1, 27), nova.DataVencimento);
        Assert.Equal(StatusFatura.Aberta, nova.Status);

        var antiga = await ObterFaturaAsync(aberta.Id);
        Assert.Equal(StatusFatura.Fechada, antiga.Status);
        Assert.Equal(new DateOnly(2025, 1, 10), antiga.DataFechamento);
        Assert.Equal(new DateOnly(2025, 1, 20), antiga.DataVencimento);
    }

    #endregion

    #region Helpers

    private async Task<Guid> CriarCartaoAsync(int diaFechamento, int diaVencimento)
    {
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = diaFechamento,
            DiaVencimento = diaVencimento,
            Ativa = true
        };
        _dbContext.Contas.Add(conta);
        await _dbContext.SaveChangesAsync();
        return conta.Id;
    }

    private async Task<Fatura> ResolverCompraAsync(Guid contaId, DateOnly data)
    {
        var (fatura, rejeitada, motivo) = await _faturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, data);
        Assert.False(rejeitada, motivo);
        Assert.NotNull(fatura);
        return fatura;
    }

    private Fatura SeedFatura(Guid contaId, StatusFatura status, DateOnly fechamento, DateOnly vencimento)
    {
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = fechamento,
            DataVencimento = vencimento,
            Status = status
        };
        _dbContext.Faturas.Add(fatura);
        return fatura;
    }

    private async Task<Fatura> ObterFaturaAsync(Guid faturaId)
    {
        // Limpa o change tracker para ler o estado persistido, nao a instancia em memoria.
        _dbContext.ChangeTracker.Clear();
        return await _dbContext.Faturas.FirstAsync(f => f.Id == faturaId);
    }

    #endregion
}
