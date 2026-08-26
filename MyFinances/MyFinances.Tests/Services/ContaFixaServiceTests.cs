using Moq;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class ContaFixaServiceTests
{
    private readonly Mock<IContaFixaRepository> _mockContaFixaRepository;
    private readonly Mock<IContaRepository> _mockContaRepository;
    private readonly Mock<ILancamentoRepository> _mockLancamentoRepository;
    private readonly Mock<IRecorrenciaGeradorService> _mockRecorrenciaGeradorService;
    private readonly ContaFixaService _service;

    public ContaFixaServiceTests()
    {
        _mockContaFixaRepository = new Mock<IContaFixaRepository>();
        _mockContaRepository = new Mock<IContaRepository>();
        _mockLancamentoRepository = new Mock<ILancamentoRepository>();
        _mockRecorrenciaGeradorService = new Mock<IRecorrenciaGeradorService>();
        _service = new ContaFixaService(
            _mockContaFixaRepository.Object,
            _mockContaRepository.Object,
            _mockLancamentoRepository.Object,
            _mockRecorrenciaGeradorService.Object);
    }

    #region Regra 3 (k): EditarAsync atualiza lancamentos PENDENTE mas nao altera PAGO

    [Fact]
    public async Task EditarAsync_AlteraValorDiaVencimentoCategoriaLancamentoPendente()
    {
        // Arrange
        var contaFixaId = Guid.NewGuid();
        var novoValor = 3000m;
        var novoDiaVencimento = 20;
        var novaCategoriaId = Guid.NewGuid();

        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            Descricao = "Aluguel",
            Valor = 2000m,
            DiaVencimento = 15,
            Ativa = true
        };

        var lancamentoPendente = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaFixaId = contaFixaId,
            ContaId = contaFixa.ContaId,
            CategoriaId = Guid.NewGuid(),
            Descricao = "Aluguel",
            Valor = 2000m,
            Data = new DateOnly(2026, 7, 15),
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pendente,
            Manual = true
        };

        var lancamentoPago = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaFixaId = contaFixaId,
            ContaId = contaFixa.ContaId,
            CategoriaId = Guid.NewGuid(),
            Descricao = "Aluguel",
            Valor = 2000m,
            Data = new DateOnly(2026, 6, 15),
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago, // JA PAGO
            Manual = true
        };

        // Popular a colecao de lancamentos da ContaFixa
        contaFixa.Lancamentos = new List<Lancamento> { lancamentoPendente, lancamentoPago };

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        // Act
        var resultado = await _service.EditarAsync(contaFixaId, novoValor, novoDiaVencimento, novaCategoriaId);

        // Assert - retorna sucesso
        Assert.True(resultado.Sucesso);
        Assert.Null(resultado.Erro);

        // Verifica que lancamento PENDENTE foi alterado
        _mockLancamentoRepository.Verify(
            r => r.Atualizar(It.Is<Lancamento>(l =>
                l.Id == lancamentoPendente.Id &&
                l.Valor == novoValor &&
                l.CategoriaId == novaCategoriaId)),
            Times.Once);

        // Verifica que lancamento PAGO nao foi alterado
        _mockLancamentoRepository.Verify(
            r => r.Atualizar(It.Is<Lancamento>(l => l.Id == lancamentoPago.Id)),
            Times.Never);
    }

    [Fact]
    public async Task EditarAsync_NaoAlteraLancamentoPago()
    {
        // Arrange - apenas lancamento Pago
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            CategoriaId = null,
            Descricao = "Teste",
            Valor = 100m,
            DiaVencimento = 10,
            Ativa = true
        };

        var lancamentoPago = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaFixaId = contaFixaId,
            ContaId = contaFixa.ContaId,
            CategoriaId = null,
            Descricao = "Teste",
            Valor = 100m,
            Data = new DateOnly(2026, 6, 10),
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Manual = true
        };

        // Popular a colecao de lancamentos da ContaFixa
        contaFixa.Lancamentos = new List<Lancamento> { lancamentoPago };

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        // Act
        var resultado = await _service.EditarAsync(contaFixaId, 200m, 20, null);

        // Assert
        Assert.True(resultado.Sucesso);

        // Lancamento Pago nao deve ser alterado
        _mockLancamentoRepository.Verify(
            r => r.Atualizar(It.IsAny<Lancamento>()),
            Times.Never);
    }

    #endregion

    #region Regra 4 (l): DesativarAsync exclui lancamentos PENDENTE mas nao PAGO

    [Fact]
    public async Task DesativarAsync_ExcluiLancamentoPendenteMantemPago()
    {
        // Arrange
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            CategoriaId = null,
            Descricao = "Teste",
            Valor = 100m,
            DiaVencimento = 10,
            Ativa = true
        };

        var lancamentoPendente = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaFixaId = contaFixaId,
            ContaId = contaFixa.ContaId,
            CategoriaId = null,
            Descricao = "Teste",
            Valor = 100m,
            Data = new DateOnly(2026, 7, 10),
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pendente,
            Manual = true
        };

        var lancamentoPago = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaFixaId = contaFixaId,
            ContaId = contaFixa.ContaId,
            CategoriaId = null,
            Descricao = "Teste",
            Valor = 100m,
            Data = new DateOnly(2026, 6, 10),
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Manual = true
        };

        // Popular a colecao de lancamentos da ContaFixa
        contaFixa.Lancamentos = new List<Lancamento> { lancamentoPendente, lancamentoPago };

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        // Act
        var resultado = await _service.DesativarAsync(contaFixaId);

        // Assert - retorna sucesso
        Assert.True(resultado.Sucesso);
        Assert.Null(resultado.Erro);

        // Verifica que lancamento PENDENTE foi excluido
        _mockLancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == lancamentoPendente.Id)),
            Times.Once);

        // Verifica que lancamento PAGO nao foi removido
        _mockLancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == lancamentoPago.Id)),
            Times.Never);

        // Verifica que ContaFixa foi atualizada com Ativa=false
        _mockContaFixaRepository.Verify(
            r => r.Atualizar(It.Is<ContaFixa>(cf => cf.Id == contaFixaId && !cf.Ativa)),
            Times.Once);
    }

    [Fact]
    public async Task DesativarAsync_NaoExcluiLancamentoPago()
    {
        // Arrange - apenas lancamento Pago
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            CategoriaId = null,
            Descricao = "Teste",
            Valor = 100m,
            DiaVencimento = 10,
            Ativa = true
        };

        var lancamentoPago = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaFixaId = contaFixaId,
            ContaId = contaFixa.ContaId,
            CategoriaId = null,
            Descricao = "Teste",
            Valor = 100m,
            Data = new DateOnly(2026, 6, 10),
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Manual = true
        };

        // Popular a colecao de lancamentos da ContaFixa
        contaFixa.Lancamentos = new List<Lancamento> { lancamentoPago };

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        // Act
        var resultado = await _service.DesativarAsync(contaFixaId);

        // Assert
        Assert.True(resultado.Sucesso);

        // Lancamento Pago nao deve ser removido
        _mockLancamentoRepository.Verify(
            r => r.Remover(It.IsAny<Lancamento>()),
            Times.Never);

        // ContaFixa deve ser desativada
        _mockContaFixaRepository.Verify(
            r => r.Atualizar(It.Is<ContaFixa>(cf => !cf.Ativa)),
            Times.Once);
    }

    #endregion

    #region Regra 14 (v): CriarAsync rejeita Tipo=Investimento, default MesReferencia quando Anual

    [Fact]
    public async Task CriarAsync_ContaTipoInvestimento_RetornaSucessoFalse()
    {
        // Arrange - Conta tipo Investimento (nao permitido para ContaFixa)
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Investimento",
            Tipo = TipoConta.Investimento,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act
        var resultado = await _service.CriarAsync(
            contaId,
            "Conta Fixa",
            100m,
            15,
            null,
            "MENSAL",
            null);

        // Assert - deve retornar falso (Investimento nao e permitido)
        Assert.False(resultado.Sucesso);
        Assert.NotNull(resultado.Erro);
        Assert.Contains("Investimento", resultado.Erro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CriarAsync_PeriodicidadeAnualSemMesReferencia_DefaultMesDeHoje()
    {
        // Arrange - Conta Banco valida, periodicidade Anual, sem mesReferencia
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Corrente",
            Tipo = TipoConta.Banco,
            Ativa = true
        };

        var contaFixaCriada = (ContaFixa?)null;
        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        _mockContaFixaRepository
            .Setup(r => r.Adicionar(It.IsAny<ContaFixa>()))
            .Callback<ContaFixa>(cf => contaFixaCriada = cf);

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act - passar null para mesReferencia e "ANUAL" para periodicidade
        var resultado = await _service.CriarAsync(
            contaId,
            "Seguro",
            500m,
            10,
            null,
            "ANUAL",
            null); // mesReferencia = null, deve defaultar

        // Assert - ContaFixa criada com MesReferencia = mes de hoje
        Assert.True(resultado.Sucesso);
        Assert.NotNull(contaFixaCriada);
        Assert.NotNull(contaFixaCriada.MesReferencia);
        Assert.Equal(DateTime.Today.Month, contaFixaCriada.MesReferencia.Value);
    }

    #endregion

    #region Regra 15 (w): EditarAsync chama LimparOcorrenciasForaDaPeriodicidadeAsync quando periodicidade/mesReferencia mudam

    [Fact]
    public async Task EditarAsync_PeriodicidadeOuMesReferenciamudam_ChamaLimpeza()
    {
        // Arrange - ContaFixa Mensal existente, vai ser editada para Anual
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            Descricao = "Conta antiga",
            Valor = 100m,
            DiaVencimento = 10,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            MesReferencia = null,
            Ativa = true,
            Lancamentos = new List<Lancamento>()
        };

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        // Act - muda periodicidade para Anual e especifica mesReferencia = 7
        var resultado = await _service.EditarAsync(
            contaFixaId,
            150m,
            15,
            null,
            "ANUAL",
            7); // mesReferencia muda

        // Assert - deve ter chamado LimparOcorrenciasForaDaPeriodicidadeAsync
        // para remover lancamentos que nao batem com a nova periodicidade
        Assert.True(resultado.Sucesso);
        _mockRecorrenciaGeradorService.Verify(
            s => s.LimparOcorrenciasForaDaPeriodicidadeAsync(
                contaFixaId,
                PeriodicidadeContaFixa.Anual,
                7),
            Times.Once);
    }

    #endregion

    #region Gap 1 (CRITICO): CriarAsync e ReativarAsync com destino Cartao devem chamar RecorrenciaGeradorService

    [Fact]
    public async Task CriarAsync_ContaTipoCartao_ChamaGerarOcorrenciaAtualEProxima()
    {
        // Arrange - Gap 1: CriarAsync com destino Cartao deve gerar Compra (via RecorrenciaGeradorService),
        // nao Lancamento Pendente cru (via GerarLancamentosPendentes antigo).
        // A regra exige que ContaFixa com destino CARTAO respeite regime de COMPETENCIA:
        // Compra deve ter Status=Pago + FaturaId (item 12), nao Lancamento Pendente sem fatura.
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao Credito",
            Tipo = TipoConta.Cartao,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        _mockContaFixaRepository
            .Setup(r => r.Adicionar(It.IsAny<ContaFixa>()))
            .Returns(Task.CompletedTask);

        _mockContaFixaRepository
            .Setup(r => r.Salvar())
            .Returns(Task.CompletedTask);

        _mockRecorrenciaGeradorService
            .Setup(s => s.GerarOcorrenciaAtualEProximaAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(2);

        // Act
        var resultado = await _service.CriarAsync(
            contaId,
            "Parcela Cartao",
            500m,
            10,
            null,
            "MENSAL",
            null);

        // Assert - deve chamar RecorrenciaGeradorService em vez de gerar Lancamento Pendente direto
        Assert.True(resultado.Sucesso);
        _mockRecorrenciaGeradorService.Verify(
            s => s.GerarOcorrenciaAtualEProximaAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>()),
            Times.Once,
            "CriarAsync com destino Cartao deve chamar GerarOcorrenciaAtualEProximaAsync (regime de competencia), nao GerarLancamentosPendentes");
    }

    [Fact]
    public async Task ReativarAsync_ContaTipoCartao_ChamaGerarOcorrenciaAtualEProxima()
    {
        // Arrange - Gap 1: mesmo que CriarAsync, ReativarAsync (false->true) com destino Cartao
        // deve gerar Compra via RecorrenciaGeradorService, nao Lancamento Pendente cru.
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
            Descricao = "Parcela Cartao",
            Valor = 500m,
            DiaVencimento = 10,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            Ativa = false,
            Lancamentos = new List<Lancamento>()
        };

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        _mockContaFixaRepository
            .Setup(r => r.Atualizar(It.IsAny<ContaFixa>()))
            .Returns(Task.CompletedTask);

        _mockContaFixaRepository
            .Setup(r => r.Salvar())
            .Returns(Task.CompletedTask);

        _mockRecorrenciaGeradorService
            .Setup(s => s.GerarOcorrenciaAtualEProximaAsync(contaFixaId, It.IsAny<DateOnly>()))
            .ReturnsAsync(2);

        // Act
        var resultado = await _service.ReativarAsync(contaFixaId);

        // Assert - deve chamar RecorrenciaGeradorService em vez de gerar Lancamento Pendente direto
        Assert.True(resultado.Sucesso);
        _mockRecorrenciaGeradorService.Verify(
            s => s.GerarOcorrenciaAtualEProximaAsync(contaFixaId, It.IsAny<DateOnly>()),
            Times.Once,
            "ReativarAsync com destino Cartao deve chamar GerarOcorrenciaAtualEProximaAsync (regime de competencia), nao GerarLancamentosPendentes");
    }

    #endregion
}
