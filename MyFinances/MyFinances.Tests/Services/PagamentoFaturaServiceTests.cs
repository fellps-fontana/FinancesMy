using Moq;
using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class PagamentoFaturaServiceTests
{
    private readonly Mock<IFaturaRepository> _faturaRepositoryMock;
    private readonly Mock<ITransferenciaRepository> _transferenciaRepositoryMock;
    private readonly Mock<ILancamentoRepository> _lancamentoRepositoryMock;
    private readonly Mock<IContaRepository> _contaRepositoryMock;
    private readonly PagamentoFaturaService _service;

    public PagamentoFaturaServiceTests()
    {
        _faturaRepositoryMock = new Mock<IFaturaRepository>();
        _transferenciaRepositoryMock = new Mock<ITransferenciaRepository>();
        _lancamentoRepositoryMock = new Mock<ILancamentoRepository>();
        _contaRepositoryMock = new Mock<IContaRepository>();

        _service = new PagamentoFaturaService(
            _faturaRepositoryMock.Object,
            _transferenciaRepositoryMock.Object,
            _lancamentoRepositoryMock.Object,
            _contaRepositoryMock.Object,
            new FaturaCreditoService(_faturaRepositoryMock.Object));
    }

    [Fact]
    public async Task PagarFatura_ComPagamentoTotal_MarcaFaturaComoPaga()
    {
        // Arrange
        var faturaId = Guid.NewGuid();
        var contaCartaoId = Guid.NewGuid();
        var contaOrigemId = Guid.NewGuid();

        var fatura = new Fatura
        {
            Id = faturaId,
            ContaId = contaCartaoId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    Tipo = TipoLancamento.Debit,
                    Valor = 500m
                }
            }
        };

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaOrigemId,
            Valor = 500m,
            Data = new DateOnly(2025, 1, 25)
        };

        _faturaRepositoryMock.Setup(r => r.ObterPorId(faturaId))
            .ReturnsAsync(fatura);
        _faturaRepositoryMock.Setup(r => r.ListarPorConta(contaCartaoId))
            .ReturnsAsync(new List<Fatura> { fatura });
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);

        // Act
        var (sucesso, transferencia, erro) = await _service.PagarFaturaAsync(faturaId, request);

        // Assert
        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(transferencia);
        Assert.Equal(500m, transferencia.Valor);
        Assert.Equal(StatusFatura.Paga, fatura.Status);

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Once);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Exactly(2));
        _faturaRepositoryMock.Verify(r => r.Atualizar(fatura), Times.Once);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task PagarFatura_ComPagamentoParcial_MantemFaturaAberta()
    {
        // Arrange
        var faturaId = Guid.NewGuid();
        var contaCartaoId = Guid.NewGuid();
        var contaOrigemId = Guid.NewGuid();

        var fatura = new Fatura
        {
            Id = faturaId,
            ContaId = contaCartaoId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    Tipo = TipoLancamento.Debit,
                    Valor = 1000m
                }
            }
        };

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaOrigemId,
            Valor = 300m,
            Data = new DateOnly(2025, 1, 25)
        };

        _faturaRepositoryMock.Setup(r => r.ObterPorId(faturaId))
            .ReturnsAsync(fatura);
        _faturaRepositoryMock.Setup(r => r.ListarPorConta(contaCartaoId))
            .ReturnsAsync(new List<Fatura> { fatura });
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);

        // Act
        var (sucesso, transferencia, erro) = await _service.PagarFaturaAsync(faturaId, request);

        // Assert
        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(transferencia);
        Assert.Equal(300m, transferencia.Valor);
        // Fatura permanece Aberta
        Assert.Equal(StatusFatura.Aberta, fatura.Status);

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Once);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Exactly(2));
        _faturaRepositoryMock.Verify(r => r.Atualizar(fatura), Times.Once);
    }

    [Fact]
    public async Task PagarFatura_ComValorMaiorQueSaldo_RetornaErro()
    {
        // Arrange
        var faturaId = Guid.NewGuid();
        var contaCartaoId = Guid.NewGuid();
        var contaOrigemId = Guid.NewGuid();

        var fatura = new Fatura
        {
            Id = faturaId,
            ContaId = contaCartaoId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    Tipo = TipoLancamento.Debit,
                    Valor = 500m
                }
            }
        };

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaOrigemId,
            Valor = 1000m, // Maior que o saldo pendente
            Data = new DateOnly(2025, 1, 25)
        };

        _faturaRepositoryMock.Setup(r => r.ObterPorId(faturaId))
            .ReturnsAsync(fatura);
        _faturaRepositoryMock.Setup(r => r.ListarPorConta(contaCartaoId))
            .ReturnsAsync(new List<Fatura> { fatura });
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);

        // Act
        var (sucesso, transferencia, erro) = await _service.PagarFaturaAsync(faturaId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("nao pode exceder", erro);

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task PagarFatura_FaturaPagaNaoAceitaPagamento()
    {
        // Arrange
        var faturaId = Guid.NewGuid();
        var contaCartaoId = Guid.NewGuid();
        var contaOrigemId = Guid.NewGuid();

        var fatura = new Fatura
        {
            Id = faturaId,
            ContaId = contaCartaoId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Paga, // Ja paga
            Lancamentos = new List<Lancamento>()
        };

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaOrigemId,
            Valor = 100m,
            Data = new DateOnly(2025, 1, 25)
        };

        _faturaRepositoryMock.Setup(r => r.ObterPorId(faturaId))
            .ReturnsAsync(fatura);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);

        // Act
        var (sucesso, transferencia, erro) = await _service.PagarFaturaAsync(faturaId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("ja foi paga", erro);

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
    }

    [Fact]
    public async Task PagarFatura_ComValorZeroOuNegativo_RetornaErro()
    {
        // Arrange
        var faturaId = Guid.NewGuid();
        var contaOrigemId = Guid.NewGuid();

        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaOrigemId,
            Valor = 0m,
            Data = new DateOnly(2025, 1, 25)
        };

        // Act
        var (sucesso, transferencia, erro) = await _service.PagarFaturaAsync(faturaId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("deve ser maior que zero", erro);

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
    }

    [Fact]
    public async Task PagarFatura_FaturaComEstornos_CalculaSaldoCorretamente()
    {
        // Arrange - fatura com compra de 1000 e estorno de 200 = saldo 800
        var faturaId = Guid.NewGuid();
        var contaCartaoId = Guid.NewGuid();
        var contaOrigemId = Guid.NewGuid();

        var fatura = new Fatura
        {
            Id = faturaId,
            ContaId = contaCartaoId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>
            {
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    Tipo = TipoLancamento.Debit,
                    Valor = 1000m
                },
                new Lancamento
                {
                    Id = Guid.NewGuid(),
                    Tipo = TipoLancamento.Credit,
                    Valor = 200m
                }
            }
        };

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Banco",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaOrigemId,
            Valor = 800m,
            Data = new DateOnly(2025, 1, 25)
        };

        _faturaRepositoryMock.Setup(r => r.ObterPorId(faturaId))
            .ReturnsAsync(fatura);
        _faturaRepositoryMock.Setup(r => r.ListarPorConta(contaCartaoId))
            .ReturnsAsync(new List<Fatura> { fatura });
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);

        // Act
        var (sucesso, transferencia, erro) = await _service.PagarFaturaAsync(faturaId, request);

        // Assert
        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(transferencia);
        Assert.Equal(800m, transferencia.Valor);
        Assert.Equal(StatusFatura.Paga, fatura.Status);
    }

    [Fact]
    public async Task PagarFatura_ContaOrigemNaoExiste_RetornaErro()
    {
        // Arrange
        var faturaId = Guid.NewGuid();
        var contaCartaoId = Guid.NewGuid();
        var contaOrigemId = Guid.NewGuid();

        var fatura = new Fatura
        {
            Id = faturaId,
            ContaId = contaCartaoId,
            DataFechamento = new DateOnly(2025, 1, 10),
            DataVencimento = new DateOnly(2025, 1, 20),
            Status = StatusFatura.Aberta,
            Lancamentos = new List<Lancamento>()
        };

        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaOrigemId,
            Valor = 100m,
            Data = new DateOnly(2025, 1, 25)
        };

        _faturaRepositoryMock.Setup(r => r.ObterPorId(faturaId))
            .ReturnsAsync(fatura);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync((Conta?)null);

        // Act
        var (sucesso, transferencia, erro) = await _service.PagarFaturaAsync(faturaId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("Conta de origem nao encontrada", erro);

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
    }

    [Fact]
    public async Task PagarFatura_FaturaNaoExiste_RetornaErro()
    {
        // Arrange
        var faturaId = Guid.NewGuid();
        var contaOrigemId = Guid.NewGuid();

        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaOrigemId,
            Valor = 100m,
            Data = new DateOnly(2025, 1, 25)
        };

        _faturaRepositoryMock.Setup(r => r.ObterPorId(faturaId))
            .ReturnsAsync((Fatura?)null);

        // Act
        var (sucesso, transferencia, erro) = await _service.PagarFaturaAsync(faturaId, request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(transferencia);
        Assert.Contains("Fatura nao encontrada", erro);

        _transferenciaRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
    }
}
