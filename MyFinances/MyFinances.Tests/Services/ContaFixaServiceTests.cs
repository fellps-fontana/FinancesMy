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
    private readonly ContaFixaService _service;

    public ContaFixaServiceTests()
    {
        _mockContaFixaRepository = new Mock<IContaFixaRepository>();
        _mockContaRepository = new Mock<IContaRepository>();
        _mockLancamentoRepository = new Mock<ILancamentoRepository>();
        _service = new ContaFixaService(
            _mockContaFixaRepository.Object,
            _mockContaRepository.Object,
            _mockLancamentoRepository.Object);
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
}
