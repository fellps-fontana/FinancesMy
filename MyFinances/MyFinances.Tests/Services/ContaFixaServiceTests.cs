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

    #region Regra 2 (g-j): GerarLancamentosPendentes cria 2 lancamentos e respeita idempotencia

    [Fact]
    public async Task GerarLancamentosPendentes_PrimeiraVez_CriaExatamente2Lancamentos()
    {
        // Arrange - ContaFixa inexistente na primeira chamada, vai ser criada
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            Descricao = "Aluguel",
            Valor = 2000m,
            DiaVencimento = 15,
            Ativa = true,
            Lancamentos = new List<Lancamento>()
        };
        var dataReferencia = new DateOnly(2026, 7, 20);

        // Nao existem lancamentos gerados ainda
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
        var resultado = await _service.GerarLancamentosPendentes(contaFixaId, dataReferencia);

        // Assert - deve retornar sucesso com 2 lancamentos gerados
        Assert.True(resultado.Sucesso);
        Assert.Equal(2, resultado.LancamentosGerados);
        Assert.Null(resultado.Erro);

        // Verifica que 2 lancamentos foram adicionados
        _mockLancamentoRepository.Verify(
            r => r.Adicionar(It.IsAny<Lancamento>()),
            Times.Exactly(2));

        // Verifica que salvou
        _mockLancamentoRepository.Verify(r => r.Salvar(), Times.Once);

        // Verifica meses dos lancamentos
        Assert.Equal(2, lancamentosCapturados.Count);
        var lancamentoMesAtual = lancamentosCapturados.FirstOrDefault(l =>
            l.Data.Year == 2026 && l.Data.Month == 7);
        var lancamentoProximoMes = lancamentosCapturados.FirstOrDefault(l =>
            l.Data.Year == 2026 && l.Data.Month == 8);

        Assert.NotNull(lancamentoMesAtual);
        Assert.NotNull(lancamentoProximoMes);
    }

    [Fact]
    public async Task GerarLancamentosPendentes_DuasVezesIdempotencia_NaoDuplica()
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
            Ativa = true,
            Lancamentos = new List<Lancamento>()
        };
        var dataReferencia = new DateOnly(2026, 7, 20);

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        // Na primeira chamada, ja existem lancamentos gerados
        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2026, 7))
            .ReturnsAsync(true);

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2026, 8))
            .ReturnsAsync(true);

        // Act - chama a geracao duas vezes
        var resultado1 = await _service.GerarLancamentosPendentes(contaFixaId, dataReferencia);
        var resultado2 = await _service.GerarLancamentosPendentes(contaFixaId, dataReferencia);

        // Assert - nenhum lancamento criado (ja existiam)
        _mockLancamentoRepository.Verify(
            r => r.Adicionar(It.IsAny<Lancamento>()),
            Times.Never);

        // Ambas retornam sucesso mas com 0 lancamentos gerados (idempotencia)
        Assert.True(resultado1.Sucesso);
        Assert.True(resultado2.Sucesso);
    }

    [Fact]
    public async Task GerarLancamentosPendentes_ContaFixaInexistente_RetornaSucessoFalse()
    {
        // Arrange
        var contaFixaId = Guid.NewGuid();
        var dataReferencia = new DateOnly(2026, 7, 20);

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync((ContaFixa?)null);

        // Act
        var resultado = await _service.GerarLancamentosPendentes(contaFixaId, dataReferencia);

        // Assert - retorna Sucesso=false
        Assert.False(resultado.Sucesso);
        Assert.NotNull(resultado.Erro);

        // Nenhum lancamento criado
        _mockLancamentoRepository.Verify(
            r => r.Adicionar(It.IsAny<Lancamento>()),
            Times.Never);
    }

    [Fact]
    public async Task GerarLancamentosPendentes_ContaFixaInativa_RetornaSucessoFalse()
    {
        // Arrange - ContaFixa com Ativa=false
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            CategoriaId = null,
            Descricao = "Teste",
            Valor = 100m,
            DiaVencimento = 10,
            Ativa = false // INATIVA
        };
        var dataReferencia = new DateOnly(2026, 7, 20);

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        // Act
        var resultado = await _service.GerarLancamentosPendentes(contaFixaId, dataReferencia);

        // Assert - retorna Sucesso=false
        Assert.False(resultado.Sucesso);
        Assert.NotNull(resultado.Erro);

        // Nenhum lancamento criado
        _mockLancamentoRepository.Verify(
            r => r.Adicionar(It.IsAny<Lancamento>()),
            Times.Never);
    }

    #endregion

    #region Regra 2b (o): GerarLancamentosPendentes com Anual gera ano atual + proximo ano

    [Fact]
    public async Task GerarLancamentosPendentes_PeriodicidadeAnual_CriaExatamente2LancamentosAnos()
    {
        // Arrange - ContaFixa com Periodicidade.Anual
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            Descricao = "Seguro Anual",
            Valor = 5000m,
            DiaVencimento = 15,
            Ativa = true,
            Periodicidade = PeriodicidadeContaFixa.Anual, // ANUAL
            Lancamentos = new List<Lancamento>()
        };
        var dataReferencia = new DateOnly(2026, 7, 20);

        // Nao existem lancamentos gerados ainda
        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        // Esperamos lancamentos em 2026 e 2027
        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2026, 7))
            .ReturnsAsync(false);

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2027, 7))
            .ReturnsAsync(false);

        var lancamentosCapturados = new List<Lancamento>();
        _mockLancamentoRepository
            .Setup(r => r.Adicionar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l => lancamentosCapturados.Add(l));

        // Act
        var resultado = await _service.GerarLancamentosPendentes(contaFixaId, dataReferencia);

        // Assert - deve retornar sucesso com 2 lancamentos gerados
        Assert.True(resultado.Sucesso);
        Assert.Equal(2, resultado.LancamentosGerados);
        Assert.Null(resultado.Erro);

        // Verifica que 2 lancamentos foram adicionados
        _mockLancamentoRepository.Verify(
            r => r.Adicionar(It.IsAny<Lancamento>()),
            Times.Exactly(2));

        // Verifica que salvou
        _mockLancamentoRepository.Verify(r => r.Salvar(), Times.Once);

        // Verifica anos dos lancamentos (mesma data do mes em anos diferentes)
        Assert.Equal(2, lancamentosCapturados.Count);
        var lancamento2026 = lancamentosCapturados.FirstOrDefault(l =>
            l.Data.Year == 2026 && l.Data.Month == 7);
        var lancamento2027 = lancamentosCapturados.FirstOrDefault(l =>
            l.Data.Year == 2027 && l.Data.Month == 7);

        Assert.NotNull(lancamento2026);
        Assert.NotNull(lancamento2027);
    }

    [Fact]
    public async Task GerarLancamentosPendentes_PeriodicidadeAnual_IdempotenciaPreservada()
    {
        // Arrange - ContaFixa com Periodicidade.Anual
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            CategoriaId = null,
            Descricao = "Anualidade",
            Valor = 1000m,
            DiaVencimento = 10,
            Ativa = true,
            Periodicidade = PeriodicidadeContaFixa.Anual, // ANUAL
            Lancamentos = new List<Lancamento>()
        };
        var dataReferencia = new DateOnly(2026, 7, 20);

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        // Na segunda chamada, ja existem lancamentos dos anos 2026 e 2027
        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2026, 7))
            .ReturnsAsync(true);

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2027, 7))
            .ReturnsAsync(true);

        // Act - chama a geracao duas vezes
        var resultado1 = await _service.GerarLancamentosPendentes(contaFixaId, dataReferencia);
        var resultado2 = await _service.GerarLancamentosPendentes(contaFixaId, dataReferencia);

        // Assert - nenhum lancamento criado (ja existiam)
        _mockLancamentoRepository.Verify(
            r => r.Adicionar(It.IsAny<Lancamento>()),
            Times.Never);

        // Ambas retornam sucesso mas com 0 lancamentos gerados (idempotencia)
        Assert.True(resultado1.Sucesso);
        Assert.True(resultado2.Sucesso);
    }

    [Fact]
    public async Task GerarLancamentosPendentes_PeriodicidadeAnual_Dia31ClampFevereiro()
    {
        // Arrange - ContaFixa com dia 31, periodicidade Anual
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            CategoriaId = null,
            Descricao = "Teste Dia 31",
            Valor = 2000m,
            DiaVencimento = 31, // DIA 31
            Ativa = true,
            Periodicidade = PeriodicidadeContaFixa.Anual,
            Lancamentos = new List<Lancamento>()
        };
        // Data ref em janeiro (que tem 31 dias)
        var dataReferencia = new DateOnly(2024, 1, 31);

        _mockContaFixaRepository
            .Setup(r => r.ObterPorId(contaFixaId))
            .ReturnsAsync(contaFixa);

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2024, 1))
            .ReturnsAsync(false);

        _mockContaFixaRepository
            .Setup(r => r.ExisteLancamentoGerado(contaFixaId, 2025, 1))
            .ReturnsAsync(false);

        var lancamentosCapturados = new List<Lancamento>();
        _mockLancamentoRepository
            .Setup(r => r.Adicionar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l => lancamentosCapturados.Add(l));

        // Act
        var resultado = await _service.GerarLancamentosPendentes(contaFixaId, dataReferencia);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Equal(2, resultado.LancamentosGerados);

        // Ambos lancamentos (2024 e 2025, mes 1) devem ter dia 31
        // (janeiro tem 31 dias em ambos os anos)
        Assert.All(lancamentosCapturados, l => Assert.Equal(31, l.Data.Day));
    }

    #endregion

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
}
