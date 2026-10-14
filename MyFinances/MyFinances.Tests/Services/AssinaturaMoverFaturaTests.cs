using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using MyFinances.Tests.Helpers;
using Xunit;

namespace MyFinances.Tests.Services;

// TASK-239: assinatura com dia_referencia alterado MOVE a compra para a fatura do novo ciclo
// quando o destino pode receber (Aberta/Futura, existente ou pelo calendario). Destino Fechada/Paga
// nao recebe: a compra fica onde esta, com Data e FaturaId originais, e so valor/descricao/categoria propagam.
// SQLite em memoria, um banco por teste. Cartao 20/27. Relogio 2026-10-13 (ou 2026-10-22 nos casos de ciclo passado).
public class AssinaturaMoverFaturaTests : IAsyncLifetime
{
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

    // Monta o servico com o relogio do cenario (cada teste escolhe o "hoje").
    private void Preparar(DateOnly hoje)
    {
        var contaRepository = new ContaRepository(_db);
        var faturaRepository = new FaturaRepository(_db);
        var lancamentoRepository = new LancamentoRepository(_db);
        var ciclo = new FaturaCicloService(faturaRepository, contaRepository, new RelogioFake(hoje));

        _service = new AssinaturaCartaoService(
            new AssinaturaCartaoRepository(_db),
            contaRepository,
            lancamentoRepository,
            ciclo);
    }

    #region 5-8. Movimento entre ciclos

    [Fact]
    public async Task DiaMudaParaCicloComFuturaExistente_MoveParaFuturaComDataNova()
    {
        // Arrange - hoje 13/10: ABERTA 20/10 com compra 05/10; FUTURA 20/11 existe
        Preparar(new DateOnly(2026, 10, 13));
        var assinatura = SeedAssinatura(dia: 5);
        var aberta = SeedFatura(new DateOnly(2026, 10, 20), StatusFatura.Aberta);
        var futura = SeedFatura(new DateOnly(2026, 11, 20), StatusFatura.Futura);
        var compra = SeedCompra(assinatura, aberta, new DateOnly(2026, 10, 5));
        await _db.SaveChangesAsync();

        // Act - dia 5 -> 25: 25/10 cai no ciclo de 20/11
        await _service.EditarAsync(assinatura.Id, "Netflix", 39.90m, 25, null);

        // Assert
        var depois = await CompraAsync(compra.Id);
        Assert.Equal(futura.Id, depois.FaturaId);
        Assert.Equal(new DateOnly(2026, 10, 25), depois.Data);
    }

    [Fact]
    public async Task DiaMudaParaCicloSemFaturaExistente_CriaFuturaEMoveSemApagarAAberta()
    {
        // Arrange - mesma situacao, mas FUTURA 20/11 nao existe
        Preparar(new DateOnly(2026, 10, 13));
        var assinatura = SeedAssinatura(dia: 5);
        var aberta = SeedFatura(new DateOnly(2026, 10, 20), StatusFatura.Aberta);
        var compra = SeedCompra(assinatura, aberta, new DateOnly(2026, 10, 5));
        await _db.SaveChangesAsync();

        // Act
        await _service.EditarAsync(assinatura.Id, "Netflix", 39.90m, 25, null);

        // Assert
        _db.ChangeTracker.Clear();
        var depois = await CompraAsync(compra.Id);
        var nova = await _db.Faturas.SingleAsync(f => f.ContaId == _contaId && f.DataFechamento == new DateOnly(2026, 11, 20));
        Assert.Equal(StatusFatura.Futura, nova.Status);
        Assert.Equal(nova.Id, depois.FaturaId);
        Assert.Equal(new DateOnly(2026, 10, 25), depois.Data);
        Assert.Equal(StatusFatura.Aberta, (await _db.Faturas.FirstAsync(f => f.Id == aberta.Id)).Status);
    }

    [Fact]
    public async Task DiaMudaParaCicloAlemDaAtual_CriaNovaFuturaEMoveAPartirDeFuturaExistente()
    {
        // Arrange - FUTURA 20/11 com compra 05/11; dia 5 -> 25 cruza para 20/12 (inexistente)
        Preparar(new DateOnly(2026, 10, 13));
        var assinatura = SeedAssinatura(dia: 5);
        var futuraNov = SeedFatura(new DateOnly(2026, 11, 20), StatusFatura.Futura);
        var compra = SeedCompra(assinatura, futuraNov, new DateOnly(2026, 11, 5));
        await _db.SaveChangesAsync();

        // Act
        await _service.EditarAsync(assinatura.Id, "Netflix", 39.90m, 25, null);

        // Assert
        _db.ChangeTracker.Clear();
        var depois = await CompraAsync(compra.Id);
        var dezembro = await _db.Faturas.SingleAsync(f => f.ContaId == _contaId && f.DataFechamento == new DateOnly(2026, 12, 20));
        Assert.Equal(StatusFatura.Futura, dezembro.Status);
        Assert.Equal(dezembro.Id, depois.FaturaId);
        Assert.Equal(new DateOnly(2026, 11, 25), depois.Data);
    }

    [Fact]
    public async Task DiaVoltaParaTras_MoveParaAbertaAtual()
    {
        // Arrange - compra 25/10 na FUTURA 20/11; dia 5 -> 05/10 cai no ciclo ABERTA 20/10
        Preparar(new DateOnly(2026, 10, 13));
        var assinatura = SeedAssinatura(dia: 25);
        var aberta = SeedFatura(new DateOnly(2026, 10, 20), StatusFatura.Aberta);
        var futura = SeedFatura(new DateOnly(2026, 11, 20), StatusFatura.Futura);
        var compra = SeedCompra(assinatura, futura, new DateOnly(2026, 10, 25));
        await _db.SaveChangesAsync();

        // Act
        await _service.EditarAsync(assinatura.Id, "Netflix", 39.90m, 5, null);

        // Assert
        var depois = await CompraAsync(compra.Id);
        Assert.Equal(aberta.Id, depois.FaturaId);
        Assert.Equal(new DateOnly(2026, 10, 5), depois.Data);
    }

    #endregion

    #region 9-12. Destino que nao pode receber (Paga, Fechada, passado)

    [Fact]
    public async Task DestinoPagaComFechamentoNoCicloAtual_NaoMoveEDeixaAPagaIntocada()
    {
        // Arrange - hoje 13/10: PAGA 20/11 (ainda nao encerrada); compra em ABERTA 20/10
        Preparar(new DateOnly(2026, 10, 13));
        var assinatura = SeedAssinatura(dia: 5);
        var aberta = SeedFatura(new DateOnly(2026, 10, 20), StatusFatura.Aberta);
        var paga = SeedFatura(new DateOnly(2026, 11, 20), StatusFatura.Paga);
        var compra = SeedCompra(assinatura, aberta, new DateOnly(2026, 10, 5));
        await _db.SaveChangesAsync();

        // Act
        await _service.EditarAsync(assinatura.Id, "Netflix", 39.90m, 25, null);

        // Assert
        _db.ChangeTracker.Clear();
        var depois = await CompraAsync(compra.Id);
        Assert.Equal(aberta.Id, depois.FaturaId);
        Assert.Equal(new DateOnly(2026, 10, 5), depois.Data);
        Assert.Equal(StatusFatura.Paga, (await _db.Faturas.FirstAsync(f => f.Id == paga.Id)).Status);
    }

    [Fact]
    public async Task DestinoFechada_NaoMoveMasPropagaValorDescricaoECategoria()
    {
        // Arrange - hoje 22/10: compra 28/10 em ABERTA 20/11; destino 20/10 e FECHADA (fechamento passado)
        Preparar(new DateOnly(2026, 10, 22));
        var categoria = await SeedCategoriaAsync();
        var assinatura = SeedAssinatura(dia: 28);
        var atual = SeedFatura(new DateOnly(2026, 11, 20), StatusFatura.Aberta);
        var fechada = SeedFatura(new DateOnly(2026, 10, 20), StatusFatura.Fechada);
        var compra = SeedCompra(assinatura, atual, new DateOnly(2026, 10, 28));
        await _db.SaveChangesAsync();

        // Act - dia 28 -> 5: a data nova 05/10 cai no ciclo 20/10 (Fechada)
        await _service.EditarAsync(assinatura.Id, "Netflix 4K", 55.90m, 5, categoria.Id);

        // Assert
        _db.ChangeTracker.Clear();
        var depois = await CompraAsync(compra.Id);
        Assert.Equal(atual.Id, depois.FaturaId);
        Assert.Equal(new DateOnly(2026, 10, 28), depois.Data);
        Assert.Equal(55.90m, depois.Valor);
        Assert.Equal("Netflix 4K", depois.Descricao);
        Assert.Equal(categoria.Id, depois.CategoriaId);
        Assert.False(await _db.Lancamentos.AnyAsync(l => l.FaturaId == fechada.Id));
    }

    [Fact]
    public async Task DestinoPagaComCicloPassado_NaoMove()
    {
        // Arrange - hoje 22/10: compra 28/10 em ABERTA 20/11; destino 20/10 PAGA (ciclo passado)
        Preparar(new DateOnly(2026, 10, 22));
        var assinatura = SeedAssinatura(dia: 28);
        var atual = SeedFatura(new DateOnly(2026, 11, 20), StatusFatura.Aberta);
        var pagaPassada = SeedFatura(new DateOnly(2026, 10, 20), StatusFatura.Paga);
        var compra = SeedCompra(assinatura, atual, new DateOnly(2026, 10, 28));
        await _db.SaveChangesAsync();

        // Act - dia 28 -> 5: 05/10 cai no ciclo 20/10 (Paga e encerrada)
        await _service.EditarAsync(assinatura.Id, "Netflix", 39.90m, 5, null);

        // Assert
        _db.ChangeTracker.Clear();
        var depois = await CompraAsync(compra.Id);
        Assert.Equal(atual.Id, depois.FaturaId);
        Assert.Equal(new DateOnly(2026, 10, 28), depois.Data);
        Assert.Equal(StatusFatura.Paga, (await _db.Faturas.FirstAsync(f => f.Id == pagaPassada.Id)).Status);
    }

    [Fact]
    public async Task DestinoInexistenteComCicloPassado_NaoMoveENaoCriaFatura()
    {
        // Arrange - hoje 22/10: sem fatura 20/10; calendario a classificaria Fechada
        Preparar(new DateOnly(2026, 10, 22));
        var assinatura = SeedAssinatura(dia: 28);
        var atual = SeedFatura(new DateOnly(2026, 11, 20), StatusFatura.Aberta);
        var compra = SeedCompra(assinatura, atual, new DateOnly(2026, 10, 28));
        await _db.SaveChangesAsync();
        var faturasAntes = await _db.Faturas.CountAsync(f => f.ContaId == _contaId);

        // Act
        await _service.EditarAsync(assinatura.Id, "Netflix", 39.90m, 5, null);

        // Assert
        _db.ChangeTracker.Clear();
        var depois = await CompraAsync(compra.Id);
        Assert.Equal(atual.Id, depois.FaturaId);
        Assert.Equal(new DateOnly(2026, 10, 28), depois.Data);
        Assert.Equal(faturasAntes, await _db.Faturas.CountAsync(f => f.ContaId == _contaId));
        Assert.False(await _db.Faturas.AnyAsync(f => f.ContaId == _contaId && f.DataFechamento == new DateOnly(2026, 10, 20)));
    }

    #endregion

    #region 13-15. Sem cruzar ciclo, clamp e contagem

    [Fact]
    public async Task DiaMudaSemCruzarCiclo_MantemFaturaEAtualizaData()
    {
        // Arrange - ABERTA 20/10 com compra 05/10; dia 5 -> 15 continua no ciclo 20/10
        Preparar(new DateOnly(2026, 10, 13));
        var assinatura = SeedAssinatura(dia: 5);
        var aberta = SeedFatura(new DateOnly(2026, 10, 20), StatusFatura.Aberta);
        var compra = SeedCompra(assinatura, aberta, new DateOnly(2026, 10, 5));
        await _db.SaveChangesAsync();

        // Act
        await _service.EditarAsync(assinatura.Id, "Netflix", 39.90m, 15, null);

        // Assert
        var depois = await CompraAsync(compra.Id);
        Assert.Equal(aberta.Id, depois.FaturaId);
        Assert.Equal(new DateOnly(2026, 10, 15), depois.Data);
    }

    [Fact]
    public async Task DiaTrintaEUmEmMesDeTrintaDias_ClampaParaUltimoDia()
    {
        // Arrange - FUTURA 20/11 com compra 05/11; dia 31 -> clamp 30/11 cruza para 20/12
        Preparar(new DateOnly(2026, 10, 13));
        var assinatura = SeedAssinatura(dia: 5);
        var futuraNov = SeedFatura(new DateOnly(2026, 11, 20), StatusFatura.Futura);
        var compra = SeedCompra(assinatura, futuraNov, new DateOnly(2026, 11, 5));
        await _db.SaveChangesAsync();

        // Act
        await _service.EditarAsync(assinatura.Id, "Netflix", 39.90m, 31, null);

        // Assert - clampado para 30/11 e movido para a fatura do ciclo 20/12
        _db.ChangeTracker.Clear();
        var depois = await CompraAsync(compra.Id);
        var dezembro = await _db.Faturas.SingleAsync(f => f.ContaId == _contaId && f.DataFechamento == new DateOnly(2026, 12, 20));
        Assert.Equal(new DateOnly(2026, 11, 30), depois.Data);
        Assert.Equal(dezembro.Id, depois.FaturaId);
    }

    [Fact]
    public async Task MoverCompra_NaoMudaAContagemDeComprasDaAssinatura()
    {
        // Arrange - mesmo cenario de movimento para a FUTURA existente
        Preparar(new DateOnly(2026, 10, 13));
        var assinatura = SeedAssinatura(dia: 5);
        var aberta = SeedFatura(new DateOnly(2026, 10, 20), StatusFatura.Aberta);
        SeedFatura(new DateOnly(2026, 11, 20), StatusFatura.Futura);
        SeedCompra(assinatura, aberta, new DateOnly(2026, 10, 5));
        await _db.SaveChangesAsync();
        var quantidadeAntes = await _db.Lancamentos.CountAsync(l => l.AssinaturaCartaoId == assinatura.Id);

        // Act
        await _service.EditarAsync(assinatura.Id, "Netflix", 39.90m, 25, null);

        // Assert
        _db.ChangeTracker.Clear();
        Assert.Equal(quantidadeAntes, await _db.Lancamentos.CountAsync(l => l.AssinaturaCartaoId == assinatura.Id));
    }

    #endregion

    #region Helpers

    private AssinaturaCartao SeedAssinatura(int dia)
    {
        var assinatura = new AssinaturaCartao
        {
            Id = Guid.NewGuid(),
            ContaId = _contaId,
            Descricao = "Netflix",
            Valor = 39.90m,
            DiaReferencia = dia,
            Ativa = true
        };
        _db.AssinaturasCartao.Add(assinatura);
        return assinatura;
    }

    private Fatura SeedFatura(DateOnly dataFechamento, StatusFatura status)
    {
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = _contaId,
            DataFechamento = dataFechamento,
            DataVencimento = dataFechamento.AddDays(7),
            Status = status
        };
        _db.Faturas.Add(fatura);
        return fatura;
    }

    private Lancamento SeedCompra(AssinaturaCartao assinatura, Fatura fatura, DateOnly data)
    {
        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = _contaId,
            AssinaturaCartaoId = assinatura.Id,
            Descricao = assinatura.Descricao,
            Valor = assinatura.Valor,
            Tipo = TipoLancamento.Debit,
            Data = data,
            Status = StatusLancamento.Pendente,
            FaturaId = fatura.Id
        };
        _db.Lancamentos.Add(compra);
        return compra;
    }

    private async Task<Categoria> SeedCategoriaAsync()
    {
        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Streaming",
            Tipo = TipoCategoria.Despesa
        };
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();
        return categoria;
    }

    private async Task<Lancamento> CompraAsync(Guid compraId)
    {
        _db.ChangeTracker.Clear();
        return await _db.Lancamentos.FirstAsync(l => l.Id == compraId);
    }

    #endregion
}
