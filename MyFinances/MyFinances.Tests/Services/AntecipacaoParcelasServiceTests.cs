using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

// Testes de integracao (SQLite em memoria, um banco por teste) para
// AntecipacaoParcelasService.AnteciparAsync (regra critica, item 12 subsecao
// "Antecipacao de parcelas"). Autorizado no briefing do Kira.
// Cenario base: compra 550,00 em 10x (parcela k = 10k reais), parcela 3/10 na fatura
// ABERTA (mar/25, fechamento 10/03), 1 e 2 em faturas anteriores, 4..10 em faturas posteriores.
public class AntecipacaoParcelasServiceTests
{
    private static readonly DateOnly DataAntecipacao = new(2025, 3, 5);

    private sealed class Harness : IDisposable
    {
        private readonly SqliteConnection _connection;

        public MyFinancesDbContext Db { get; }

        public Harness()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
                .UseSqlite(_connection)
                .Options;

            Db = new MyFinancesDbContext(options);
            Db.Database.EnsureCreated();
        }

        public AntecipacaoParcelasService CriarService(ILancamentoRepository? lancamentoRepository = null)
        {
            var contaRepository = new ContaRepository(Db);
            var faturaRepository = new FaturaRepository(Db);
            var compraParceladaRepository = new CompraParceladaRepository(Db);
            var lancamentoRepo = lancamentoRepository ?? new LancamentoRepository(Db);
            var validacaoCartaoService = new ValidacaoCartaoService(contaRepository);

            return new AntecipacaoParcelasService(
                compraParceladaRepository,
                lancamentoRepo,
                faturaRepository,
                validacaoCartaoService,
                NullLogger<AntecipacaoParcelasService>.Instance);
        }

        public void Dispose()
        {
            Db.Dispose();
            _connection.Dispose();
        }
    }

    private sealed class Cenario
    {
        public Guid ContaId { get; init; }
        public Guid CompraId { get; init; }
        public Guid CategoriaId { get; init; }
        public Guid FaturaAbertaId { get; init; }
        public Dictionary<int, Guid> FaturaPorParcela { get; } = new();
    }

    private static Fatura CriarFatura(Guid contaId, int ano, int mes, int diaVencimento, StatusFatura status) => new()
    {
        Id = Guid.NewGuid(),
        ContaId = contaId,
        DataFechamento = new DateOnly(ano, mes, 10),
        DataVencimento = new DateOnly(ano, mes, diaVencimento),
        Status = status
    };

    private static async Task<Cenario> SeedCenarioBase(MyFinancesDbContext db)
    {
        var contaId = Guid.NewGuid();
        db.Contas.Add(new Conta
        {
            Id = contaId,
            Nome = "Cartao Teste",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 10,
            DiaVencimento = 20,
            Ativa = true
        });

        var categoriaId = Guid.NewGuid();
        db.Categorias.Add(new Categoria
        {
            Id = categoriaId,
            Nome = "Eletronicos",
            Tipo = TipoCategoria.Despesa
        });

        var compraId = Guid.NewGuid();
        db.ComprasParceladas.Add(new CompraParcelada
        {
            Id = compraId,
            Descricao = "Notebook Dell",
            ValorTotal = 550.00m,
            QuantidadeParcelas = 10,
            DataCompra = new DateOnly(2025, 1, 5)
        });

        var faturas = new List<Fatura>
        {
            CriarFatura(contaId, 2025, 1, 20, StatusFatura.Paga),
            CriarFatura(contaId, 2025, 2, 20, StatusFatura.Fechada),
            CriarFatura(contaId, 2025, 3, 20, StatusFatura.Aberta)
        };
        for (var mes = 4; mes <= 10; mes++)
        {
            faturas.Add(CriarFatura(contaId, 2025, mes, 20, StatusFatura.Fechada));
        }

        db.Faturas.AddRange(faturas);

        // Parcela 1 -> Jan, 2 -> Fev, 3 -> Mar (aberta), k (4..10) -> mes k
        var cenario = new Cenario
        {
            ContaId = contaId,
            CompraId = compraId,
            CategoriaId = categoriaId,
            FaturaAbertaId = faturas[2].Id
        };

        for (var numero = 1; numero <= 10; numero++)
        {
            var fatura = faturas[numero - 1];
            cenario.FaturaPorParcela[numero] = fatura.Id;

            db.Lancamentos.Add(new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                CategoriaId = categoriaId,
                CompraParceladaId = compraId,
                ParcelaNumero = numero,
                Descricao = "Notebook Dell",
                Valor = 10.00m * numero,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, numero, 5),
                Status = StatusLancamento.Pendente,
                FaturaId = fatura.Id
            });
        }

        await db.SaveChangesAsync();

        return cenario;
    }

    private static async Task<List<Lancamento>> LerParcelas(MyFinancesDbContext db, Guid compraId)
    {
        return await db.Lancamentos
            .AsNoTracking()
            .Where(l => l.CompraParceladaId == compraId)
            .OrderBy(l => l.ParcelaNumero)
            .ToListAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AnteciparAsync_QuantidadeMenorQueUm_RejeitaSemAlterarParcelas(int quantidade)
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);
        var service = harness.CriarService();

        // Act
        var (sucesso, fatura, parcelas, erro) = await service.AnteciparAsync(
            cenario.ContaId, cenario.CompraId, quantidade, DataAntecipacao);

        // Assert
        Assert.False(sucesso);
        Assert.Null(fatura);
        Assert.Null(parcelas);
        Assert.NotNull(erro);

        var depois = await LerParcelas(harness.Db, cenario.CompraId);
        Assert.All(depois, l => Assert.False(l.Antecipada));
        Assert.Equal(cenario.FaturaPorParcela[10], depois[9].FaturaId);
        Assert.Equal(cenario.FaturaPorParcela[9], depois[8].FaturaId);
    }

    [Fact]
    public async Task AnteciparAsync_CompraInexistente_Rejeita()
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);
        var service = harness.CriarService();

        // Act
        var (sucesso, fatura, parcelas, erro) = await service.AnteciparAsync(
            cenario.ContaId, Guid.NewGuid(), 2, DataAntecipacao);

        // Assert
        Assert.False(sucesso);
        Assert.Null(fatura);
        Assert.Null(parcelas);
        Assert.NotNull(erro);
    }

    [Fact]
    public async Task AnteciparAsync_CompraDeOutraConta_Rejeita()
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);

        var outraContaId = Guid.NewGuid();
        harness.Db.Contas.Add(new Conta
        {
            Id = outraContaId,
            Nome = "Outro Cartao",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 10,
            DiaVencimento = 20,
            Ativa = true
        });
        await harness.Db.SaveChangesAsync();

        var service = harness.CriarService();

        // Act
        var (sucesso, fatura, parcelas, erro) = await service.AnteciparAsync(
            outraContaId, cenario.CompraId, 2, DataAntecipacao);

        // Assert
        Assert.False(sucesso);
        Assert.Null(fatura);
        Assert.Null(parcelas);
        Assert.NotNull(erro);

        var depois = await LerParcelas(harness.Db, cenario.CompraId);
        Assert.All(depois, l => Assert.False(l.Antecipada));
    }

    [Fact]
    public async Task AnteciparAsync_ContaNaoCartao_Rejeita()
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);

        var conta = await harness.Db.Contas.FindAsync(cenario.ContaId);
        conta!.Tipo = TipoConta.Banco;
        await harness.Db.SaveChangesAsync();

        var service = harness.CriarService();

        // Act
        var (sucesso, fatura, parcelas, erro) = await service.AnteciparAsync(
            cenario.ContaId, cenario.CompraId, 2, DataAntecipacao);

        // Assert
        Assert.False(sucesso);
        Assert.Null(fatura);
        Assert.Null(parcelas);
        Assert.NotNull(erro);
    }

    [Fact]
    public async Task AnteciparAsync_SemFaturaAberta_RejeitaSemCriarFatura()
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);

        var faturaAberta = await harness.Db.Faturas.FindAsync(cenario.FaturaAbertaId);
        faturaAberta!.Status = StatusFatura.Fechada;
        await harness.Db.SaveChangesAsync();

        var faturasAntes = await harness.Db.Faturas.AsNoTracking().CountAsync(f => f.ContaId == cenario.ContaId);
        var service = harness.CriarService();

        // Act
        var (sucesso, fatura, parcelas, erro) = await service.AnteciparAsync(
            cenario.ContaId, cenario.CompraId, 2, DataAntecipacao);

        // Assert
        Assert.False(sucesso);
        Assert.Null(fatura);
        Assert.Null(parcelas);
        Assert.NotNull(erro);

        var faturasDepois = await harness.Db.Faturas.AsNoTracking().CountAsync(f => f.ContaId == cenario.ContaId);
        Assert.Equal(faturasAntes, faturasDepois);
    }

    [Fact]
    public async Task AnteciparAsync_DuasUltimasParcelas_MovePara10e9NaFaturaAbertaComDataEFlagAntecipada()
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);
        var service = harness.CriarService();

        // Act
        var (sucesso, fatura, parcelas, erro) = await service.AnteciparAsync(
            cenario.ContaId, cenario.CompraId, 2, DataAntecipacao);

        // Assert - resultado
        Assert.True(sucesso, $"Erro ao antecipar: {erro}");
        Assert.NotNull(fatura);
        Assert.Equal(cenario.FaturaAbertaId, fatura!.Id);

        // Assert - banco
        var depois = await LerParcelas(harness.Db, cenario.CompraId);
        Assert.Equal(10, depois.Count);

        foreach (var numero in new[] { 9, 10 })
        {
            var parcela = depois[numero - 1];
            Assert.Equal(cenario.FaturaAbertaId, parcela.FaturaId);
            Assert.Equal(DataAntecipacao, parcela.Data);
            Assert.True(parcela.Antecipada);
        }

        for (var numero = 4; numero <= 8; numero++)
        {
            var parcela = depois[numero - 1];
            Assert.Equal(cenario.FaturaPorParcela[numero], parcela.FaturaId);
            Assert.Equal(new DateOnly(2025, numero, 5), parcela.Data);
            Assert.False(parcela.Antecipada);
        }
    }

    [Fact]
    public async Task AnteciparAsync_ParcelaMovida_PreservaValorNumeroCompraECategoria()
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);
        var service = harness.CriarService();

        var antes = await LerParcelas(harness.Db, cenario.CompraId);

        // Act
        var (sucesso, _, _, erro) = await service.AnteciparAsync(
            cenario.ContaId, cenario.CompraId, 2, DataAntecipacao);

        // Assert
        Assert.True(sucesso, $"Erro ao antecipar: {erro}");

        var depois = await LerParcelas(harness.Db, cenario.CompraId);
        foreach (var numero in new[] { 9, 10 })
        {
            var original = antes[numero - 1];
            var movida = depois[numero - 1];
            Assert.Equal(original.Id, movida.Id);
            Assert.Equal(numero * 10.00m, movida.Valor);
            Assert.Equal(numero, movida.ParcelaNumero);
            Assert.Equal(cenario.CompraId, movida.CompraParceladaId);
            Assert.Equal(cenario.CategoriaId, movida.CategoriaId);
            Assert.Equal(TipoLancamento.Debit, movida.Tipo);
        }
    }

    [Fact]
    public async Task AnteciparAsync_AposAntecipar_NenhumLancamentoCriadoOuRemovidoESomaIgualAoTotal()
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);
        var service = harness.CriarService();

        var lancamentosAntes = await harness.Db.Lancamentos.AsNoTracking().CountAsync();

        // Act
        var (sucesso, _, _, erro) = await service.AnteciparAsync(
            cenario.ContaId, cenario.CompraId, 2, DataAntecipacao);

        // Assert
        Assert.True(sucesso, $"Erro ao antecipar: {erro}");

        var lancamentosDepois = await harness.Db.Lancamentos.AsNoTracking().CountAsync();
        Assert.Equal(lancamentosAntes, lancamentosDepois);

        var depois = await LerParcelas(harness.Db, cenario.CompraId);
        Assert.Equal(10, depois.Count);
        Assert.Equal(550.00m, depois.Sum(l => l.Valor));
    }

    [Fact]
    public async Task AnteciparAsync_NaoGeraPagamentoNemTransferenciaNemMudaStatus()
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);
        var service = harness.CriarService();

        var transferenciasAntes = await harness.Db.Transferencias.AsNoTracking().CountAsync();
        var faturasAntes = await harness.Db.Faturas.AsNoTracking()
            .Where(f => f.ContaId == cenario.ContaId)
            .OrderBy(f => f.DataVencimento)
            .Select(f => new { f.Id, f.Status })
            .ToListAsync();

        // Act
        var (sucesso, _, _, erro) = await service.AnteciparAsync(
            cenario.ContaId, cenario.CompraId, 2, DataAntecipacao);

        // Assert
        Assert.True(sucesso, $"Erro ao antecipar: {erro}");

        var transferenciasDepois = await harness.Db.Transferencias.AsNoTracking().CountAsync();
        Assert.Equal(transferenciasAntes, transferenciasDepois);

        var depois = await LerParcelas(harness.Db, cenario.CompraId);
        Assert.All(depois, l => Assert.Null(l.TransferenciaId));
        Assert.All(depois, l => Assert.Equal(StatusLancamento.Pendente, l.Status));

        var faturasDepois = await harness.Db.Faturas.AsNoTracking()
            .Where(f => f.ContaId == cenario.ContaId)
            .OrderBy(f => f.DataVencimento)
            .Select(f => new { f.Id, f.Status })
            .ToListAsync();
        Assert.Equal(faturasAntes.Select(f => (f.Id, f.Status)), faturasDepois.Select(f => (f.Id, f.Status)));
    }

    [Fact]
    public async Task AnteciparAsync_RepetidaSobreAMesmaCompra_MoveProximasParcelas8e7()
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);
        var service = harness.CriarService();

        var primeira = await service.AnteciparAsync(cenario.ContaId, cenario.CompraId, 2, DataAntecipacao);
        Assert.True(primeira.Sucesso, $"Erro na primeira antecipacao: {primeira.Erro}");

        // Act
        var segunda = await service.AnteciparAsync(cenario.ContaId, cenario.CompraId, 2, DataAntecipacao);

        // Assert
        Assert.True(segunda.Sucesso, $"Erro na segunda antecipacao: {segunda.Erro}");

        var depois = await LerParcelas(harness.Db, cenario.CompraId);
        foreach (var numero in new[] { 7, 8, 9, 10 })
        {
            var parcela = depois[numero - 1];
            Assert.Equal(cenario.FaturaAbertaId, parcela.FaturaId);
            Assert.True(parcela.Antecipada);
        }

        for (var numero = 4; numero <= 6; numero++)
        {
            var parcela = depois[numero - 1];
            Assert.Equal(cenario.FaturaPorParcela[numero], parcela.FaturaId);
            Assert.False(parcela.Antecipada);
        }

        Assert.Equal(550.00m, depois.Sum(l => l.Valor));
    }

    [Fact]
    public async Task AnteciparAsync_FalhaNoSalvar_FazRollbackEMantemParcelasOriginais()
    {
        // Arrange
        using var harness = new Harness();
        var cenario = await SeedCenarioBase(harness.Db);
        var lancamentoComSalvarFalho = new LancamentoRepositoryComSalvarFalho(new LancamentoRepository(harness.Db));
        var service = harness.CriarService(lancamentoComSalvarFalho);

        // Act
        var (sucesso, fatura, parcelas, erro) = await service.AnteciparAsync(
            cenario.ContaId, cenario.CompraId, 2, DataAntecipacao);

        // Assert - falha reportada, nada devolvido
        Assert.False(sucesso);
        Assert.Null(fatura);
        Assert.Null(parcelas);
        Assert.NotNull(erro);

        // Assert - banco intacto (rollback)
        var depois = await LerParcelas(harness.Db, cenario.CompraId);
        Assert.Equal(10, depois.Count);
        Assert.All(depois, l => Assert.False(l.Antecipada));
        for (var numero = 1; numero <= 10; numero++)
        {
            Assert.Equal(cenario.FaturaPorParcela[numero], depois[numero - 1].FaturaId);
            Assert.Equal(new DateOnly(2025, numero, 5), depois[numero - 1].Data);
        }
    }

    // Decorator que delega tudo ao repositorio real, exceto Salvar, que falha.
    private sealed class LancamentoRepositoryComSalvarFalho : ILancamentoRepository
    {
        private readonly ILancamentoRepository _interno;

        public LancamentoRepositoryComSalvarFalho(ILancamentoRepository interno)
        {
            _interno = interno;
        }

        public Task Adicionar(Lancamento lancamento) => _interno.Adicionar(lancamento);
        public Task<Lancamento?> ObterPorId(Guid id) => _interno.ObterPorId(id);
        public Task<IEnumerable<Lancamento>> ListarPorConta(Guid contaId) => _interno.ListarPorConta(contaId);
        public Task<IEnumerable<Lancamento>> ListarPorFatura(Guid faturaId) => _interno.ListarPorFatura(faturaId);
        public Task<IEnumerable<Lancamento>> ListarParaFluxoCaixa(Guid? contaId) => _interno.ListarParaFluxoCaixa(contaId);
        public Task<IEnumerable<Lancamento>> ListarPorCategoriasEPeriodo(IEnumerable<Guid> categoriaIds, int ano, int mes)
            => _interno.ListarPorCategoriasEPeriodo(categoriaIds, ano, mes);
        public Task<IEnumerable<Lancamento>> ListarParaFluxoCaixaDoMes(int ano, int mes) => _interno.ListarParaFluxoCaixaDoMes(ano, mes);
        public Task Atualizar(Lancamento lancamento) => _interno.Atualizar(lancamento);
        public Task Remover(Lancamento lancamento) => _interno.Remover(lancamento);
        public Task<IEnumerable<Lancamento>> ListarDebitosParaRelatorioAsync(int ano, int mes)
            => _interno.ListarDebitosParaRelatorioAsync(ano, mes);

        public Task Salvar() => throw new InvalidOperationException("Falha simulada no Salvar");
    }
}
