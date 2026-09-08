using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class AssinaturaCartaoServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private MyFinancesDbContext _dbContext = null!;
    private IAssinaturaCartaoRepository _assinaturaRepo = null!;
    private IContaRepository _contaRepo = null!;
    private ILancamentoRepository _lancamentoRepo = null!;
    private IFaturaRepository _faturaRepo = null!;
    private FaturaCicloService _faturaCicloService = null!;
    private AssinaturaCartaoService _service = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new MyFinancesDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _assinaturaRepo = new AssinaturaCartaoRepository(_dbContext);
        _contaRepo = new ContaRepository(_dbContext);
        _lancamentoRepo = new LancamentoRepository(_dbContext);
        _faturaRepo = new FaturaRepository(_dbContext);
        _faturaCicloService = new FaturaCicloService(_faturaRepo, _contaRepo);

        _service = new AssinaturaCartaoService(
            _assinaturaRepo,
            _contaRepo,
            _lancamentoRepo,
            _faturaCicloService);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.CloseAsync();
        _connection.Dispose();
    }

    private async Task<Conta> CriarContaCartao(bool ativa = true, int diaFechamento = 10, int diaVencimento = 17)
    {
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Nubank",
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Cartao,
            DiaFechamento = diaFechamento,
            DiaVencimento = diaVencimento,
            Ativa = ativa
        };
        await _contaRepo.Adicionar(conta);
        await _contaRepo.Salvar();
        return conta;
    }

    private async Task<Conta> CriarContaBanco(bool ativa = true)
    {
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Itaú Corrente",
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Banco,
            Ativa = ativa
        };
        await _contaRepo.Adicionar(conta);
        await _contaRepo.Salvar();
        return conta;
    }

    #region 1. Validações de Entrada (CriarAsync)

    [Fact]
    public async Task CriarAsync_ContaNaoEncontrada_LancaExcecao()
    {
        var contaInexistenteId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CriarAsync(contaInexistenteId, "Netflix", 44.90m, 15, null));
    }

    [Fact]
    public async Task CriarAsync_ContaInativa_LancaExcecao()
    {
        var contaInativa = await CriarContaCartao(ativa: false);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CriarAsync(contaInativa.Id, "Netflix", 44.90m, 15, null));
    }

    [Fact]
    public async Task CriarAsync_ContaTipoBanco_RejeitaComExcecao()
    {
        var contaBanco = await CriarContaBanco();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CriarAsync(contaBanco.Id, "Netflix", 44.90m, 15, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10.50)]
    public async Task CriarAsync_ValorInvalidoMenorOuIgualZero_LancaArgumentException(decimal valorInvalido)
    {
        var contaCartao = await CriarContaCartao();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CriarAsync(contaCartao.Id, "Netflix", valorInvalido, 15, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-5)]
    public async Task CriarAsync_DiaReferenciaInvalido_LancaArgumentException(int diaInvalido)
    {
        var contaCartao = await CriarContaCartao();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CriarAsync(contaCartao.Id, "Netflix", 44.90m, diaInvalido, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CriarAsync_DescricaoVaziaOuWhitespace_LancaArgumentException(string? descricaoInvalida)
    {
        var contaCartao = await CriarContaCartao();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CriarAsync(contaCartao.Id, descricaoInvalida!, 44.90m, 15, null));
    }

    #endregion

    #region 2. Criação com Geração Imediata no Ciclo Corrente

    [Fact]
    public async Task CriarAsync_DadosValidos_PersisteAssinaturaEAtiva()
    {
        var contaCartao = await CriarContaCartao();

        var assinatura = await _service.CriarAsync(contaCartao.Id, "Spotify Premium", 21.90m, 10, null);

        Assert.NotNull(assinatura);
        Assert.True(assinatura.Ativa);
        Assert.Equal("Spotify Premium", assinatura.Descricao);
        Assert.Equal(21.90m, assinatura.Valor);
        Assert.Equal(10, assinatura.DiaReferencia);

        var persistida = await _assinaturaRepo.ObterPorId(assinatura.Id);
        Assert.NotNull(persistida);
    }

    [Fact]
    public async Task CriarAsync_DadosValidos_GeraCompraDeCartaoNaCompetencia()
    {
        var contaCartao = await CriarContaCartao();

        var assinatura = await _service.CriarAsync(contaCartao.Id, "Netflix", 39.90m, 15, null);

        var compras = await _dbContext.Lancamentos
            .Where(l => l.AssinaturaCartaoId == assinatura.Id)
            .ToListAsync();

        Assert.Single(compras);
        var compra = compras.First();
        Assert.Equal(39.90m, compra.Valor);
        Assert.Equal("Netflix", compra.Descricao);
        Assert.Equal(TipoLancamento.Debit, compra.Tipo);
        Assert.Equal(StatusLancamento.Pago, compra.Status);
        Assert.True(compra.Manual);
        Assert.NotNull(compra.FaturaId);
        Assert.Equal(contaCartao.Id, compra.ContaId);
    }

    #endregion

    #region 3. Clamp de Data para Meses Mais Curtos

    [Fact]
    public async Task CriarAsync_DiaReferencia31_ClampaParaUltimoDiaDoMes()
    {
        var contaCartao = await CriarContaCartao();

        var assinatura = await _service.CriarAsync(contaCartao.Id, "Assinatura Dia 31", 100m, 31, null);

        var compra = await _dbContext.Lancamentos
            .FirstOrDefaultAsync(l => l.AssinaturaCartaoId == assinatura.Id);

        Assert.NotNull(compra);
        var diasNoMes = DateTime.DaysInMonth(compra.Data.Year, compra.Data.Month);
        Assert.Equal(diasNoMes, compra.Data.Day);
    }

    #endregion

    #region 4. Idempotência por Ciclo / Mês

    [Fact]
    public async Task Idempotencia_ChamarGarantirDuasVezesNoMesmoMes_NaoDuplicaCompra()
    {
        var contaCartao = await CriarContaCartao();
        var assinatura = await _service.CriarAsync(contaCartao.Id, "Amazon Prime", 19.90m, 5, null);

        var hoje = DateTime.Today;
        // Segunda chamada para o mesmo ano/mes
        await _service.GarantirAssinaturasDoCicloAsync(contaCartao.Id, hoje.Year, hoje.Month);

        var totalCompras = await _dbContext.Lancamentos
            .CountAsync(l => l.AssinaturaCartaoId == assinatura.Id && l.Data.Year == hoje.Year && l.Data.Month == hoje.Month);

        Assert.Equal(1, totalCompras);
    }

    #endregion

    #region 5. Edição e Propagação para Fatura ABERTA (Decisão PO)

    [Fact]
    public async Task EditarAsync_AssinaturaNaoEncontrada_LancaExcecao()
    {
        var idInexistente = Guid.NewGuid();

        await Assert.ThrowsAsync<AssinaturaCartaoNaoEncontradaException>(() =>
            _service.EditarAsync(idInexistente, "Novo Nome", 50m, 10, null));
    }

    [Fact]
    public async Task EditarAsync_FaturaAberta_PropagaNovoValorEDescricaoParaCompraJaGerada()
    {
        var contaCartao = await CriarContaCartao();
        var assinatura = await _service.CriarAsync(contaCartao.Id, "Netflix", 39.90m, 15, null);

        // Fatura nasce Aberta
        var atualizada = await _service.EditarAsync(assinatura.Id, "Netflix 4K", 55.90m, 15, null);

        Assert.Equal(55.90m, atualizada.Valor);
        Assert.Equal("Netflix 4K", atualizada.Descricao);

        var compra = await _dbContext.Lancamentos
            .FirstOrDefaultAsync(l => l.AssinaturaCartaoId == assinatura.Id);

        Assert.NotNull(compra);
        Assert.Equal(55.90m, compra.Valor);
        Assert.Equal("Netflix 4K", compra.Descricao);
    }

    [Fact]
    public async Task EditarAsync_FaturaFechadaOuPaga_NaoAlteraCompraHistorica()
    {
        var contaCartao = await CriarContaCartao();
        var assinatura = await _service.CriarAsync(contaCartao.Id, "Netflix", 39.90m, 15, null);

        var compra = await _dbContext.Lancamentos
            .Include(l => l.Fatura)
            .FirstOrDefaultAsync(l => l.AssinaturaCartaoId == assinatura.Id);
        Assert.NotNull(compra);

        // Força fatura a ser Paga (fato consumado)
        compra.Fatura!.Status = StatusFatura.Paga;
        await _dbContext.SaveChangesAsync();

        // Edita a assinatura
        await _service.EditarAsync(assinatura.Id, "Netflix 4K", 55.90m, 15, null);

        // Compra na fatura Paga nao pode ter sido alterada
        var compraAposEdicao = await _dbContext.Lancamentos.FindAsync(compra.Id);
        Assert.NotNull(compraAposEdicao);
        Assert.Equal(39.90m, compraAposEdicao.Valor);
        Assert.Equal("Netflix", compraAposEdicao.Descricao);
    }

    #endregion

    #region 6. Desativação Preserva Compras (Fato Histórico)

    [Fact]
    public async Task DesativarAsync_MantemComprasJaGeradasIntocadas()
    {
        var contaCartao = await CriarContaCartao();
        var assinatura = await _service.CriarAsync(contaCartao.Id, "Disney+", 33.90m, 12, null);

        await _service.DesativarAsync(assinatura.Id);

        var desativada = await _assinaturaRepo.ObterPorId(assinatura.Id);
        Assert.NotNull(desativada);
        Assert.False(desativada.Ativa);

        // Compra continua existindo
        var compras = await _dbContext.Lancamentos
            .Where(l => l.AssinaturaCartaoId == assinatura.Id)
            .ToListAsync();
        Assert.Single(compras);
    }

    #endregion

    #region 7. Reativação

    [Fact]
    public async Task ReativarAsync_AssinaturaInativa_MarcaAtivaTrue()
    {
        var contaCartao = await CriarContaCartao();
        var assinatura = await _service.CriarAsync(contaCartao.Id, "HBO Max", 34.90m, 20, null);
        await _service.DesativarAsync(assinatura.Id);

        await _service.ReativarAsync(assinatura.Id);

        var reativada = await _assinaturaRepo.ObterPorId(assinatura.Id);
        Assert.NotNull(reativada);
        Assert.True(reativada.Ativa);
    }

    #endregion

    #region 8. Garantir Assinaturas do Ciclo (Sob Demanda)

    [Fact]
    public async Task GarantirAssinaturasDoCicloAsync_AssinaturaAtivaSemCompraNoCiclo_GeraCompra()
    {
        var contaCartao = await CriarContaCartao();
        var assinatura = new AssinaturaCartao
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Descricao = "Cloud Storage",
            Valor = 9.99m,
            DiaReferencia = 5,
            Ativa = true
        };
        await _assinaturaRepo.Adicionar(assinatura);
        await _assinaturaRepo.Salvar();

        var hoje = DateTime.Today;
        var geradas = await _service.GarantirAssinaturasDoCicloAsync(contaCartao.Id, hoje.Year, hoje.Month);

        Assert.Equal(1, geradas);

        var compra = await _dbContext.Lancamentos
            .FirstOrDefaultAsync(l => l.AssinaturaCartaoId == assinatura.Id && l.Data.Year == hoje.Year && l.Data.Month == hoje.Month);
        Assert.NotNull(compra);
        Assert.Equal(9.99m, compra.Valor);
    }

    [Fact]
    public async Task GarantirAssinaturasDoCicloAsync_AssinaturaInativa_NaoGeraCompra()
    {
        var contaCartao = await CriarContaCartao();
        var assinaturaInativa = new AssinaturaCartao
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Descricao = "Servico Cancelado",
            Valor = 50m,
            DiaReferencia = 15,
            Ativa = false
        };
        await _assinaturaRepo.Adicionar(assinaturaInativa);
        await _assinaturaRepo.Salvar();

        var hoje = DateTime.Today;
        var geradas = await _service.GarantirAssinaturasDoCicloAsync(contaCartao.Id, hoje.Year, hoje.Month);

        Assert.Equal(0, geradas);

        var compras = await _dbContext.Lancamentos
            .Where(l => l.AssinaturaCartaoId == assinaturaInativa.Id)
            .ToListAsync();
        Assert.Empty(compras);
    }

    #endregion
}
