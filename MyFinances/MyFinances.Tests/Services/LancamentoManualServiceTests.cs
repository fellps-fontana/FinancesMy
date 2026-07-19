using Moq;
using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class LancamentoManualServiceTests
{
    private readonly Mock<ILancamentoRepository> _lancamentoRepositoryMock;
    private readonly Mock<IContaRepository> _contaRepositoryMock;
    private readonly LancamentoManualService _service;

    public LancamentoManualServiceTests()
    {
        _lancamentoRepositoryMock = new Mock<ILancamentoRepository>();
        _contaRepositoryMock = new Mock<IContaRepository>();

        _service = new LancamentoManualService(
            _lancamentoRepositoryMock.Object,
            _contaRepositoryMock.Object);
    }

    [Fact]
    public async Task CriarAsync_ComDadosValidos_CriaLancamentoComSucesso()
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

        var request = new CriarLancamentoRequest
        {
            Descricao = "Lancamento Teste",
            Valor = 100m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 1, 15),
            Status = "PENDENTE"
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act
        var (sucesso, lancamento, erro) = await _service.CriarAsync(contaId, request);

        // Assert
        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(lancamento);
        Assert.Equal("Lancamento Teste", lancamento.Descricao);
        Assert.Equal(100m, lancamento.Valor);
        Assert.Equal("DEBIT", lancamento.Tipo);
        Assert.Equal("PENDENTE", lancamento.Status);
        Assert.True(lancamento.Manual);
        Assert.False(lancamento.Oculto);

        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Once);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_ComContaInativa_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Conta Inativa",
            Ativa = false,
            Origem = OrigemConta.Manual
        };

        var request = new CriarLancamentoRequest
        {
            Descricao = "Lancamento Teste",
            Valor = 100m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 1, 15),
            Status = "PENDENTE"
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act
        var (sucesso, lancamento, erro) = await _service.CriarAsync(contaId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(lancamento);
        Assert.Contains("inativa", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ComContaNaoExistente_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();

        var request = new CriarLancamentoRequest
        {
            Descricao = "Lancamento Teste",
            Valor = 100m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 1, 15),
            Status = "PENDENTE"
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync((Conta?)null);

        // Act
        var (sucesso, lancamento, erro) = await _service.CriarAsync(contaId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(lancamento);
        Assert.Contains("nao encontrada", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ComStatusSugerido_RetornaErro()
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

        var request = new CriarLancamentoRequest
        {
            Descricao = "Lancamento Teste",
            Valor = 100m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 1, 15),
            Status = "SUGERIDO"
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act
        var (sucesso, lancamento, erro) = await _service.CriarAsync(contaId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(lancamento);
        Assert.Contains("SUGERIDO", erro);
        Assert.Contains("nao e permitido", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ComValorZeroOuNegativo_RetornaErro()
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

        var request = new CriarLancamentoRequest
        {
            Descricao = "Lancamento Teste",
            Valor = 0m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 1, 15),
            Status = "PENDENTE"
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act
        var (sucesso, lancamento, erro) = await _service.CriarAsync(contaId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(lancamento);
        Assert.Contains("deve ser maior que zero", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ComDescricaoVazia_RetornaErro()
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

        var request = new CriarLancamentoRequest
        {
            Descricao = "",
            Valor = 100m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 1, 15),
            Status = "PENDENTE"
        };

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act
        var (sucesso, lancamento, erro) = await _service.CriarAsync(contaId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(lancamento);
        Assert.Contains("obrigatoria", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_ComDadosValidos_EditaLancamentoComSucesso()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Descricao Antiga",
            Valor = 50m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 10),
            Status = StatusLancamento.Pendente,
            Manual = true
        };

        var request = new EditarLancamentoRequest
        {
            Descricao = "Descricao Nova",
            Valor = 100m,
            Tipo = "CREDIT",
            Data = new DateOnly(2025, 1, 15),
            Status = "PAGO"
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        // Act
        var (sucesso, lancamentoEditado, erro) = await _service.EditarAsync(contaId, lancamentoId, request);

        // Assert
        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(lancamentoEditado);
        Assert.Equal("Descricao Nova", lancamentoEditado.Descricao);
        Assert.Equal(100m, lancamentoEditado.Valor);
        Assert.Equal("CREDIT", lancamentoEditado.Tipo);
        Assert.Equal("PAGO", lancamentoEditado.Status);

        _lancamentoRepositoryMock.Verify(r => r.Atualizar(lancamento), Times.Once);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task EditarAsync_ComLancamentoDeOutraConta_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var outraContaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = outraContaId,
            Descricao = "Lancamento",
            Valor = 50m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 10),
            Status = StatusLancamento.Pendente
        };

        var request = new EditarLancamentoRequest
        {
            Descricao = "Descricao",
            Valor = 100m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 1, 15)
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        // Act
        var (sucesso, lancamentoEditado, erro) = await _service.EditarAsync(contaId, lancamentoId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(lancamentoEditado);
        Assert.Contains("nao encontrado", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_ComStatusSugerido_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Lancamento",
            Valor = 50m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 10),
            Status = StatusLancamento.Pendente
        };

        var request = new EditarLancamentoRequest
        {
            Descricao = "Descricao",
            Valor = 100m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 1, 15),
            Status = "SUGERIDO"
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        // Act
        var (sucesso, lancamentoEditado, erro) = await _service.EditarAsync(contaId, lancamentoId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(lancamentoEditado);
        Assert.Contains("SUGERIDO", erro);

        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task MarcarComoPagoAsync_ComLancamentoPendente_MarcaComoPago()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Lancamento",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 15),
            Status = StatusLancamento.Pendente
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        // Act
        var (sucesso, erro) = await _service.MarcarComoPagoAsync(contaId, lancamentoId);

        // Assert
        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(StatusLancamento.Pago, lancamento.Status);

        _lancamentoRepositoryMock.Verify(r => r.Atualizar(lancamento), Times.Once);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task MarcarComoPagoAsync_ComLancamentoJaPago_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Lancamento",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 15),
            Status = StatusLancamento.Pago
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        // Act
        var (sucesso, erro) = await _service.MarcarComoPagoAsync(contaId, lancamentoId);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("PENDENTE", erro);

        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task MarcarComoPagoAsync_ComLancamentoSugerido_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Lancamento",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 15),
            Status = StatusLancamento.Sugerido
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        // Act
        var (sucesso, erro) = await _service.MarcarComoPagoAsync(contaId, lancamentoId);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("PENDENTE", erro);

        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task MarcarComoPagoAsync_ComLancamentoDeOutraConta_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var outraContaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = outraContaId,
            Descricao = "Lancamento",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 15),
            Status = StatusLancamento.Pendente
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        // Act
        var (sucesso, erro) = await _service.MarcarComoPagoAsync(contaId, lancamentoId);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrado", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task RemoverAsync_ComLancamentoExistente_RemoveComSucesso()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Lancamento",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 15),
            Status = StatusLancamento.Pendente
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        // Act
        var (sucesso, erro) = await _service.RemoverAsync(contaId, lancamentoId);

        // Assert
        Assert.True(sucesso);
        Assert.Null(erro);

        _lancamentoRepositoryMock.Verify(r => r.Remover(lancamento), Times.Once);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_ComLancamentoDeOutraConta_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var outraContaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = outraContaId,
            Descricao = "Lancamento",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 15),
            Status = StatusLancamento.Pendente
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        // Act
        var (sucesso, erro) = await _service.RemoverAsync(contaId, lancamentoId);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrado", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Remover(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task RemoverAsync_ComLancamentoNaoExistente_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync((Lancamento?)null);

        // Act
        var (sucesso, erro) = await _service.RemoverAsync(contaId, lancamentoId);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrado", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Remover(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task MarcarComoPagoAsync_ComContaInativa_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var contaInativa = new Conta
        {
            Id = contaId,
            Nome = "Conta Inativa",
            Ativa = false,
            Origem = OrigemConta.Manual
        };

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Conta = contaInativa,
            Descricao = "Lancamento",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 15),
            Status = StatusLancamento.Pendente
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(contaInativa);

        // Act
        var (sucesso, erro) = await _service.MarcarComoPagoAsync(contaId, lancamentoId);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("inativa", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_ComContaInativa_RetornaErro()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var contaInativa = new Conta
        {
            Id = contaId,
            Nome = "Conta Inativa",
            Ativa = false,
            Origem = OrigemConta.Manual
        };

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Conta = contaInativa,
            Descricao = "Descricao Original",
            Valor = 50m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 1, 10),
            Status = StatusLancamento.Pendente,
            Manual = true
        };

        var request = new EditarLancamentoRequest
        {
            Descricao = "Descricao Nova",
            Valor = 100m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 1, 15)
        };

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(contaInativa);

        // Act
        var (sucesso, lancamentoEditado, erro) = await _service.EditarAsync(contaId, lancamentoId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(lancamentoEditado);
        Assert.Contains("inativa", erro.ToLower());

        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }
}
