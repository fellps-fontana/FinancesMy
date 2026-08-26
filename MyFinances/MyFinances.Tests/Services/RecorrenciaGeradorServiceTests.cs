using Moq;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

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

    [Fact]
    public async Task GerarOcorrenciaAtualEProximaAsync_ContaCartao_GeraCompraViaService()
    {
        // Arrange - ContaFixa vinculada a Conta tipo Cartao
        var contaFixaId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Credito",
            Tipo = TipoConta.Cartao,
            Ativa = true
        };
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = contaId,
            Conta = conta,
            Descricao = "Parcela cartao",
            Valor = 500m,
            DiaVencimento = 10,
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

        // Mock CompraCartaoService.CriarCompraAsync - deve ser chamado 2 vezes (atual + proxima)
        var comprasGeradas = new List<(Guid contaId, Guid? contaFixaId, DateOnly data)>();
        _mockCompraCartaoService
            .Setup(s => s.CriarCompraAsync(It.IsAny<Guid>(), It.IsAny<CriarCompraRequest>(), It.IsAny<Guid?>()))
            .Callback<Guid, CriarCompraRequest, Guid?>((cId, req, cfId) =>
                comprasGeradas.Add((cId, cfId, req.Data)))
            .ReturnsAsync((true, new Lancamento { Id = Guid.NewGuid() }, null));

        // Act
        var resultado = await _service.GerarOcorrenciaAtualEProximaAsync(contaFixaId, dataReferencia);

        // Assert - deve chamar CriarCompraAsync 2 vezes com contaFixaId preenchido
        Assert.Equal(2, resultado);
        _mockCompraCartaoService.Verify(
            s => s.CriarCompraAsync(
                It.IsAny<Guid>(),
                It.IsAny<CriarCompraRequest>(),
                It.IsAny<Guid?>()),
            Times.Exactly(2));

        Assert.Equal(2, comprasGeradas.Count);
        Assert.All(comprasGeradas, c => Assert.Equal(contaFixaId, c.contaFixaId));
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
        // Arrange - ContaFixa que era Anual (julio), vira Mensal, tinha lancamento PENDENTE em agosto que deveria ser deletado
        var contaFixaId = Guid.NewGuid();
        var lancamentoPendente = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Data = new DateOnly(2026, 8, 15),
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
            Lancamentos = new List<Lancamento> { lancamentoPendente, lancamentoPago }
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

        // Assert - deve remover o lancamento PENDENTE de agosto que nao mais faz sentido,
        // mas NUNCA remover o PAGO
        Assert.True(removidos >= 0);
        _mockLancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == lancamentoPendente.Id)),
            Times.Once);

        _mockLancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == lancamentoPago.Id)),
            Times.Never);
    }

    #endregion
}
