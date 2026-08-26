using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

[CollectionDefinition("RecorrenciaGerador Integration Collection")]
public class RecorrenciaGeradorIntegrationCollection : ICollectionFixture<RecorrenciaGeradorIntegrationTestsFixture>
{
}

public class RecorrenciaGeradorIntegrationTestsFixture : IAsyncLifetime
{
    private SqliteConnection? _connection;
    public MyFinancesDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString = "DataSource=:memory:";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        _connection = connection;

        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseSqlite(connection)
            .Options;

        DbContext = new MyFinancesDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}

public class RecorrenciaGeradorServiceTests
{
    private readonly Mock<IContaFixaRepository> _mockContaFixaRepository;
    private readonly Mock<ILancamentoRepository> _mockLancamentoRepository;
    private readonly Mock<CompraCartaoService> _mockCompraCartaoService;
    private readonly RecorrenciaGeradorService _service;

    public RecorrenciaGeradorServiceTests()
    {
        _mockContaFixaRepository = new Mock<IContaFixaRepository>();
        _mockLancamentoRepository = new Mock<ILancamentoRepository>();
        _mockCompraCartaoService = new Mock<CompraCartaoService>(
            new Mock<ILancamentoRepository>().Object,
            new Mock<FaturaCicloService>(
                new Mock<IFaturaRepository>().Object,
                new Mock<IContaRepository>().Object).Object,
            new Mock<ValidacaoCartaoService>(
                new Mock<IContaRepository>().Object).Object);
        _service = new RecorrenciaGeradorService(
            _mockContaFixaRepository.Object,
            _mockLancamentoRepository.Object,
            _mockCompraCartaoService.Object);
    }

    #region Regra 9 (q): GerarOcorrenciaAtualEProximaAsync - Banco gera Lancamento PENDENTE

    [Fact]
    public async Task GerarOcorrenciaAtualEProximaAsync_ContaBanco_GeraLancamentoPendente()
    {
        // Arrange - ContaFixa vinculada a Conta tipo Banco
        var contaFixaId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Corrente",
            Tipo = TipoConta.Banco,
            Ativa = true,
            SaldoManual = 1000m
        };
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = contaId,
            Conta = conta,
            Descricao = "Aluguel",
            Valor = 1500m,
            DiaVencimento = 15,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            Ativa = true,
            Lancamentos = new List<Lancamento>()
        };
        var dataReferencia = new DateOnly(2026, 7, 20);

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2026, 7))
            .ReturnsAsync(false);

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2026, 8))
            .ReturnsAsync(false);

        var lancamentosCapturados = new List<Lancamento>();
        _mockLancamentoRepository
            .Setup(r => r.Adicionar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l => lancamentosCapturados.Add(l));

        // Act
        var resultado = await _service.GerarOcorrenciaAtualEProximaAsync(contaFixaId, dataReferencia);

        // Assert - deve gerar exatamente 2 lancamentos PENDENTE
        Assert.Equal(2, resultado);
        Assert.Equal(2, lancamentosCapturados.Count);

        var lancamentoJulho = lancamentosCapturados.FirstOrDefault(l => l.Data.Month == 7);
        var lancamentoAgosto = lancamentosCapturados.FirstOrDefault(l => l.Data.Month == 8);

        Assert.NotNull(lancamentoJulho);
        Assert.NotNull(lancamentoAgosto);
        Assert.Equal(StatusLancamento.Pendente, lancamentoJulho.Status);
        Assert.Equal(StatusLancamento.Pendente, lancamentoAgosto.Status);
        Assert.True(lancamentoJulho.Manual);
        Assert.True(lancamentoAgosto.Manual);
    }

    #endregion

    #region Regra 10 (r): GarantirOcorrenciasAtivasDoMesAsync varre e gera conforme EhOcorrenciaValida

    [Fact]
    public async Task GarantirOcorrenciasAtivasDoMesAsync_VarreContasEGeraQuandoValida()
    {
        // Arrange - duas ContaFixa ativas, uma Mensal (sempre valida) e uma Anual (so em julho valida)
        var contaFixaMensal = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Descricao = "Mensal",
            Valor = 100m,
            DiaVencimento = 10,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            Ativa = true,
            Lancamentos = new List<Lancamento>()
        };

        var contaFixaAnual = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Descricao = "Anual julho",
            Valor = 200m,
            DiaVencimento = 15,
            Periodicidade = PeriodicidadeContaFixa.Anual,
            MesReferencia = 7,
            Ativa = true,
            Lancamentos = new List<Lancamento>()
        };

        _mockContaFixaRepository
            .Setup(r => r.Listar(true))
            .ReturnsAsync(new[] { contaFixaMensal, contaFixaAnual });

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaMensal.Id, 2026, 7))
            .ReturnsAsync(false);

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaAnual.Id, 2026, 7))
            .ReturnsAsync(false);

        // Act
        await _service.GarantirOcorrenciasAtivasDoMesAsync(2026, 7);

        // Assert - deve chamar ExisteLancamentoGerado para ambas as contas em julho
        // porque: Mensal sempre valida, Anual so valida em julho (mes 7 = MesReferencia)
        _mockContaFixaRepository.Verify(
            r => r.ExisteLancamentoGerado(contaFixaMensal.Id, 2026, 7),
            Times.Once);

        _mockContaFixaRepository.Verify(
            r => r.ExisteLancamentoGerado(contaFixaAnual.Id, 2026, 7),
            Times.Once);
    }

    #endregion

    #region Regra 11 (s): GarantirOcorrenciaDoMesAsync garante uma unica ContaFixa

    [Fact]
    public async Task GarantirOcorrenciaDoMesAsync_ContaFixaEspecifica_VerificaIdempotencia()
    {
        // Arrange
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            Descricao = "Teste",
            Valor = 100m,
            DiaVencimento = 10,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            Ativa = true,
            Lancamentos = new List<Lancamento>()
        };

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2026, 7))
            .ReturnsAsync(false);

        // Act
        var resultado = await _service.GarantirOcorrenciaDoMesAsync(contaFixaId, 2026, 7);

        // Assert
        Assert.True(resultado);
        _mockContaFixaRepository.Verify(
            r => r.ObterPorId(contaFixaId),
            Times.Once);
    }

    #endregion

    #region Regra 12 (t): LimparOcorrenciasForaDaPeriodicidadeAsync exclui as erradas

    [Fact]
    public async Task LimparOcorrenciasForaDaPeriodicidadeAsync_MudaDeAnualParaMensal_NaoExcluiPagas()
    {
        // Arrange - ContaFixa que era Anual (julho), vira Mensal
        // Hoje = 2026-08-26 (conforme ambiente)
        // Conjunto correto sob Mensal/dia 15: [2026-08-15 (atual), 2026-09-15 (proximo)]
        // Lancamento PENDENTE em 2026-11-15 fica FORA desse conjunto, deve ser deletado
        var contaFixaId = Guid.NewGuid();
        var lancamentoPendenteForaDoPar = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Data = new DateOnly(2026, 11, 15),  // Fora do par recalculado (agosto 15 + setembro 15)
            Status = StatusLancamento.Pendente,
            ContaFixaId = contaFixaId
        };

        var lancamentoPago = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Data = new DateOnly(2026, 6, 15),
            Status = StatusLancamento.Pago,
            ContaFixaId = contaFixaId
        };

        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            Descricao = "Conta",
            Valor = 100m,
            DiaVencimento = 15,
            Periodicidade = PeriodicidadeContaFixa.Anual, // ANTES era Anual
            MesReferencia = 7,
            Ativa = true,
            Lancamentos = new List<Lancamento> { lancamentoPendenteForaDoPar, lancamentoPago }
        };

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        var lancamentosRemovidos = new List<Lancamento>();
        _mockLancamentoRepository
            .Setup(r => r.Remover(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l => lancamentosRemovidos.Add(l));

        // Act - muda para Mensal (sem MesReferencia)
        var removidos = await _service.LimparOcorrenciasForaDaPeriodicidadeAsync(
            contaFixaId,
            PeriodicidadeContaFixa.Mensal,
            null);

        // Assert - deve remover o lancamento PENDENTE de novembro que nao faz sentido sob Mensal,
        // mas NUNCA remover o PAGO (regra de fato consumado)
        Assert.True(removidos >= 0);
        _mockLancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == lancamentoPendenteForaDoPar.Id)),
            Times.Once);

        _mockLancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == lancamentoPago.Id)),
            Times.Never);
    }

    #endregion

    #region Gap 2 (CRITICO): LimparOcorrenciasForaDaPeriodicidadeAsync deve limpar destino Cartao

    [Fact]
    public async Task LimparOcorrenciasForaDaPeriodicidadeAsync_CartaoComFaturaNaoPagaForaDoConjunto_RemoveCompra()
    {
        // Arrange - Gap 2: hoje o filtro e Where(l => l.Status == Pendente), mas Compra de cartao
        // SEMPRE nasce Status=Pago. Entao pra ContaFixa-cartao esse filtro retorna vazio e a limpeza
        // nunca dispara. A regra exige: elegivel pra exclusao quando Fatura.Status != Paga.
        var contaFixaId = Guid.NewGuid();

        // Compra vinculada a Fatura ABERTA que fica FORA do conjunto recalculado
        var compraFaturaNaoPaga = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Data = new DateOnly(2026, 11, 15),  // Fora do par Mensal [agosto 15, setembro 15]
            Status = StatusLancamento.Pago,
            ContaFixaId = contaFixaId,
            Fatura = new Fatura { Status = StatusFatura.Aberta }  // NAO PAGA = elegivel pra exclusao
        };

        // Compra vinculada a Fatura JA PAGA que fica fora do conjunto -> NUNCA remove (fato consumado)
        var compraFaturaPaga = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Data = new DateOnly(2026, 10, 15),  // Fora do par recalculado
            Status = StatusLancamento.Pago,
            ContaFixaId = contaFixaId,
            Fatura = new Fatura { Status = StatusFatura.Paga }  // JA PAGA = NUNCA remove
        };

        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            Descricao = "Parcela Cartao",
            Valor = 500m,
            DiaVencimento = 15,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            Ativa = true,
            Lancamentos = new List<Lancamento> { compraFaturaNaoPaga, compraFaturaPaga }
        };

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        var lancamentosRemovidos = new List<Lancamento>();
        _mockLancamentoRepository
            .Setup(r => r.Remover(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l => lancamentosRemovidos.Add(l));

        // Act
        var removidos = await _service.LimparOcorrenciasForaDaPeriodicidadeAsync(
            contaFixaId,
            PeriodicidadeContaFixa.Mensal,
            null);

        // Assert - deve remover Compra com Fatura nao paga fora do conjunto,
        // mas NUNCA Compra com Fatura paga (fato consumado)
        Assert.True(removidos >= 0);
        _mockLancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == compraFaturaNaoPaga.Id)),
            Times.Once,
            "LimparOcorrenciasForaDaPeriodicidadeAsync deve remover Compra (Status=Pago) vinculada a Fatura NAO paga que fica fora do conjunto recalculado");

        _mockLancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == compraFaturaPaga.Id)),
            Times.Never,
            "LimparOcorrenciasForaDaPeriodicidadeAsync NUNCA deve remover Compra vinculada a Fatura JA PAGA (fato consumado)");
    }

    #endregion
}

[Collection("RecorrenciaGerador Integration Collection")]
public class RecorrenciaGeradorServiceIntegrationTests
{
    private readonly RecorrenciaGeradorIntegrationTestsFixture _fixture;

    public RecorrenciaGeradorServiceIntegrationTests(RecorrenciaGeradorIntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region Regra 9b (q-cartao): GerarOcorrenciaAtualEProximaAsync com Cartao gera Compra

    [Fact]
    public async Task GerarOcorrenciaAtualEProximaAsync_ContaCartao_GeraCompraViaService()
    {
        // Arrange - integração real com SQLite
        var contaFixaId = Guid.NewGuid();
        var contaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Credito",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 10,
            DiaVencimento = 20,
            Ativa = true
        };

        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = contaId,
            Descricao = "Parcela cartao",
            Valor = 500m,
            DiaVencimento = 10,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            Ativa = true
        };

        _fixture.DbContext.Contas.Add(conta);
        _fixture.DbContext.ContasFixas.Add(contaFixa);
        await _fixture.DbContext.SaveChangesAsync();

        // Setup serviços reais
        var contaFixaRepository = new ContaFixaRepository(_fixture.DbContext);
        var lancamentoRepository = new LancamentoRepository(_fixture.DbContext);
        var faturaRepository = new FaturaRepository(_fixture.DbContext);
        var contaRepository = new ContaRepository(_fixture.DbContext);

        var faturaCicloService = new FaturaCicloService(faturaRepository, contaRepository);
        var validacaoCartaoService = new ValidacaoCartaoService(contaRepository);
        var compraCartaoService = new CompraCartaoService(
            lancamentoRepository,
            faturaCicloService,
            validacaoCartaoService);

        var recorrenciaGeradorService = new RecorrenciaGeradorService(
            contaFixaRepository,
            lancamentoRepository,
            compraCartaoService);

        var dataReferencia = new DateOnly(2026, 7, 20);

        // Act
        var resultado = await recorrenciaGeradorService.GerarOcorrenciaAtualEProximaAsync(contaFixaId, dataReferencia);

        // Assert - deve gerar 2 compras (atual + proxima) com ContaFixaId preenchido
        Assert.Equal(2, resultado);

        // Verifica que foram criadas 2 compras com ContaFixaId preenchido
        var comprasGeradas = _fixture.DbContext.Lancamentos
            .Where(l => l.ContaFixaId == contaFixaId)
            .ToList();

        Assert.Equal(2, comprasGeradas.Count);

        // Ambas têm ContaFixaId, Status Pago e são Manuais
        Assert.All(comprasGeradas, c =>
        {
            Assert.Equal(contaFixaId, c.ContaFixaId);
            Assert.Equal(StatusLancamento.Pago, c.Status);
            Assert.True(c.Manual);
        });
    }

    #endregion
}
