using Moq;
using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class TransferenciaServiceTests
{
    private readonly Mock<ITransferenciaRepository> _transferenciaRepositoryMock;
    private readonly Mock<ILancamentoRepository> _lancamentoRepositoryMock;
    private readonly Mock<IContaRepository> _contaRepositoryMock;
    private readonly TransferenciaService _service;

    public TransferenciaServiceTests()
    {
        _transferenciaRepositoryMock = new Mock<ITransferenciaRepository>();
        _lancamentoRepositoryMock = new Mock<ILancamentoRepository>();
        _contaRepositoryMock = new Mock<IContaRepository>();

        _service = new TransferenciaService(
            _transferenciaRepositoryMock.Object,
            _lancamentoRepositoryMock.Object,
            _contaRepositoryMock.Object);
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_ComDadosValidos_CriaTransferenciaE2Lancamentos()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 500m,
            Data = new DateOnly(2025, 1, 15),
            Descricao = "Transferencia Teste"
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        // Act
        var (sucesso, transferencia, erro) = await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(transferencia);
        Assert.Equal(500m, transferencia.Valor);
        Assert.Equal(contaOrigemId, transferencia.ContaOrigemId);
        Assert.Equal(contaDestinoId, transferencia.ContaDestinoId);

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Once);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Exactly(2));
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_Cria2LancamentosComMesmoTransferenciaId()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 250m,
            Data = new DateOnly(2025, 1, 15)
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        Transferencia? transferenciaCriada = null;
        _transferenciaRepositoryMock.Setup(r => r.Adicionar(It.IsAny<Transferencia>()))
            .Callback<Transferencia>(t => transferenciaCriada = t);

        var lancamentosCapturados = new List<Lancamento>();
        _lancamentoRepositoryMock.Setup(r => r.Adicionar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l => lancamentosCapturados.Add(l));

        // Act
        var (sucesso, transferencia, erro) = await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.True(sucesso);
        Assert.NotNull(transferenciaCriada);
        Assert.Equal(2, lancamentosCapturados.Count);

        var lancamentoSaida = lancamentosCapturados[0];
        var lancamentoEntrada = lancamentosCapturados[1];

        Assert.Equal(transferenciaCriada.Id, lancamentoSaida.TransferenciaId);
        Assert.Equal(transferenciaCriada.Id, lancamentoEntrada.TransferenciaId);

        Assert.Equal(TipoLancamento.Debit, lancamentoSaida.Tipo);
        Assert.Equal(TipoLancamento.Credit, lancamentoEntrada.Tipo);

        Assert.Equal(contaOrigemId, lancamentoSaida.ContaId);
        Assert.Equal(contaDestinoId, lancamentoEntrada.ContaId);

        Assert.Equal(250m, lancamentoSaida.Valor);
        Assert.Equal(250m, lancamentoEntrada.Valor);
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_Com2LancamentosStatusPago()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 100m,
            Data = new DateOnly(2025, 1, 15)
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        var lancamentosCapturados = new List<Lancamento>();
        _lancamentoRepositoryMock.Setup(r => r.Adicionar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l => lancamentosCapturados.Add(l));

        // Act
        var (sucesso, transferencia, erro) = await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.True(sucesso);
        Assert.All(lancamentosCapturados, l => Assert.Equal(StatusLancamento.Pago, l.Status));
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_ComValorZero_RetornaErro()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 0m,
            Data = new DateOnly(2025, 1, 15)
        };

        // Act
        var (sucesso, transferencia, erro) = await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("deve ser maior que zero", erro.ToLower());

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_ComValorNegativo_RetornaErro()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = -100m,
            Data = new DateOnly(2025, 1, 15)
        };

        // Act
        var (sucesso, transferencia, erro) = await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("deve ser maior que zero", erro.ToLower());

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_ComContaOrigemNaoExistente_RetornaErro()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 100m,
            Data = new DateOnly(2025, 1, 15)
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync((Conta?)null);

        // Act
        var (sucesso, transferencia, erro) = await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("origem", erro.ToLower());
        Assert.Contains("nao encontrada", erro.ToLower());

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_ComContaDestinoNaoExistente_RetornaErro()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 100m,
            Data = new DateOnly(2025, 1, 15)
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync((Conta?)null);

        // Act
        var (sucesso, transferencia, erro) = await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("destino", erro.ToLower());
        Assert.Contains("nao encontrada", erro.ToLower());

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_ComContaOrigemInativa_RetornaErro()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Ativa = false,
            Origem = OrigemConta.Manual
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 100m,
            Data = new DateOnly(2025, 1, 15)
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        // Act
        var (sucesso, transferencia, erro) = await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("origem", erro.ToLower());
        Assert.Contains("inativa", erro.ToLower());

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_ComContaDestinoInativa_RetornaErro()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Ativa = false,
            Origem = OrigemConta.Manual
        };

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 100m,
            Data = new DateOnly(2025, 1, 15)
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        // Act
        var (sucesso, transferencia, erro) = await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("destino", erro.ToLower());
        Assert.Contains("inativa", erro.ToLower());

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_ComContaOrigemIgualDestino_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Teste",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaId,
            ContaDestinoId = contaId,
            Valor = 100m,
            Data = new DateOnly(2025, 1, 15)
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act
        var (sucesso, transferencia, erro) = await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("origem", erro.ToLower());
        Assert.Contains("destino", erro.ToLower());
        Assert.Contains("nao pode ser igual", erro.ToLower());

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarTransferenciaManualAsync_LancamentosMarkedAsManual()
    {
        // Arrange
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Origem",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Destino",
            Ativa = true,
            Origem = OrigemConta.Manual
        };

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = contaDestinoId,
            Valor = 300m,
            Data = new DateOnly(2025, 1, 15)
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        var lancamentosCapturados = new List<Lancamento>();
        _lancamentoRepositoryMock.Setup(r => r.Adicionar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(l => lancamentosCapturados.Add(l));

        // Act
        await _service.RegistrarTransferenciaManualAsync(request);

        // Assert
        Assert.All(lancamentosCapturados, l => Assert.True(l.Manual));
        Assert.All(lancamentosCapturados, l => Assert.False(l.Oculto));
    }
}
