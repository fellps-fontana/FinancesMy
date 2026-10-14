using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Repositories;
using MyFinances.Services;
using MyFinances.Tests.Helpers;
using Xunit;

namespace MyFinances.Tests.Controllers;

// TASK-238 / item 12: ListarFaturas reconcilia o calendario antes de devolver (fechamento automatico
// na leitura) e expoe o status "FUTURA". Integracao SQLite; datas relativas a hoje real, porque o
// controller ainda le DateTime.Today.
public class FaturasListarCicloTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private MyFinancesDbContext _db = null!;
    private FaturasController _controller = null!;

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
        var contaRepository = new ContaRepository(_db);
        var lancamentoRepository = new LancamentoRepository(_db);
        var transferenciaRepository = new TransferenciaRepository(_db);
        var contaFixaRepository = new ContaFixaRepository(_db);
        var validacaoCartaoService = new ValidacaoCartaoService(contaRepository);
        var faturaCreditoService = new FaturaCreditoService(faturaRepository);
        var faturaCicloService = new FaturaCicloService(
            faturaRepository,
            contaRepository,
            new RelogioFake(DateOnly.FromDateTime(DateTime.Today)));

        var recorrencia = new Mock<IRecorrenciaGeradorService>();

        _controller = new FaturasController(
            faturaRepository,
            new PagamentoFaturaService(faturaRepository, transferenciaRepository, lancamentoRepository, contaRepository, faturaCreditoService),
            new EstornoCartaoService(lancamentoRepository, faturaCicloService, validacaoCartaoService),
            faturaCreditoService,
            recorrencia.Object,
            contaFixaRepository,
            assinaturaCartaoService: null,
            faturaCicloService: faturaCicloService);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.CloseAsync();
        _connection.Dispose();
    }

    [Fact]
    public async Task ListarFaturas_ReconciliaECalendarioEDevolveStatusFutura()
    {
        // Arrange
        var hoje = DateOnly.FromDateTime(DateTime.Today);
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

        // Aberta com fechamento ja passado: o calendario manda FECHADA na leitura.
        var vencida = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = hoje.AddDays(-5),
            DataVencimento = hoje.AddDays(2),
            Status = StatusFatura.Aberta
        };
        // Futura bem depois do ciclo corrente: continua FUTURA.
        var futura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = hoje.AddDays(60),
            DataVencimento = hoje.AddDays(67),
            Status = StatusFatura.Futura
        };
        _db.Faturas.AddRange(vencida, futura);
        await _db.SaveChangesAsync();

        // Act
        var resultado = await _controller.ListarFaturas(contaId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(resultado);
        var faturas = Assert.IsAssignableFrom<IEnumerable<FaturaResponse>>(ok.Value).ToList();

        Assert.Equal("FUTURA", faturas.Single(f => f.Id == futura.Id).Status);
        Assert.Equal("FECHADA", faturas.Single(f => f.Id == vencida.Id).Status);
    }
}
