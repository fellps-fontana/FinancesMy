using Moq;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class CompraCartaoServiceTests
{
    private readonly Mock<ILancamentoRepository> _mockLancamentoRepository;
    private readonly Mock<FaturaCicloService> _mockFaturaCicloService;
    private readonly Mock<ValidacaoCartaoService> _mockValidacaoCartaoService;
    private readonly CompraCartaoService _service;

    public CompraCartaoServiceTests()
    {
        _mockLancamentoRepository = new Mock<ILancamentoRepository>();
        _mockFaturaCicloService = new Mock<FaturaCicloService>(
            new Mock<IFaturaRepository>().Object,
            new Mock<IContaRepository>().Object);
        _mockValidacaoCartaoService = new Mock<ValidacaoCartaoService>(
            new Mock<IContaRepository>().Object);
        _service = new CompraCartaoService(
            _mockLancamentoRepository.Object,
            _mockFaturaCicloService.Object,
            _mockValidacaoCartaoService.Object);
    }

    #region Regra 13 (u): CriarCompraAsync com contaFixaId preenchido vincula a compra

    [Fact]
    public async Task CriarCompraAsync_ContaFixaIdInformado_PreencheContaFixaIdNaCompra()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var contaFixaId = Guid.NewGuid();
        var faturaid = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao",
            Tipo = TipoConta.Cartao,
            Ativa = true
        };

        var fatura = new Fatura
        {
            Id = faturaid,
            ContaId = contaId,
            Status = StatusFatura.Aberta
        };

        var request = new CriarCompraRequest
        {
            Descricao = "Compra recorrente",
            Valor = 150m,
            Data = new DateOnly(2026, 7, 15),
            CategoriaId = categoriaId
        };

        // Mock validacao
        _mockValidacaoCartaoService
            .Setup(s => s.ValidarOperacaoCartaoAsync(contaId, request.Descricao, request.Valor))
            .ReturnsAsync((true, conta, null));

        // Mock fatura
        _mockFaturaCicloService
            .Setup(s => s.ResolverFaturaParaLancamentoAsync(contaId, request.Data))
            .ReturnsAsync((fatura, false, null));

        var compraCapturada = (Lancamento?)null;
        _mockLancamentoRepository
            .Setup(r => r.Adicionar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(c => compraCapturada = c);

        // Act
        var (sucesso, compra, erro) = await _service.CriarCompraAsync(contaId, request, contaFixaId);

        // Assert - compra criada com sucesso e ContaFixaId preenchido
        Assert.True(sucesso);
        Assert.NotNull(compra);
        Assert.Null(erro);
        Assert.Equal(contaFixaId, compra.ContaFixaId);
        Assert.Equal(contaFixaId, compraCapturada?.ContaFixaId);
    }

    [Fact]
    public async Task CriarCompraAsync_SemContaFixaId_PreencheNull()
    {
        // Arrange - sem passar contaFixaId, deve ficar null como antes
        var contaId = Guid.NewGuid();
        var faturaid = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Cartao",
            Tipo = TipoConta.Cartao,
            Ativa = true
        };

        var fatura = new Fatura
        {
            Id = faturaid,
            ContaId = contaId,
            Status = StatusFatura.Aberta
        };

        var request = new CriarCompraRequest
        {
            Descricao = "Compra manual",
            Valor = 200m,
            Data = new DateOnly(2026, 7, 15),
            CategoriaId = categoriaId
        };

        _mockValidacaoCartaoService
            .Setup(s => s.ValidarOperacaoCartaoAsync(contaId, request.Descricao, request.Valor))
            .ReturnsAsync((true, conta, null));

        _mockFaturaCicloService
            .Setup(s => s.ResolverFaturaParaLancamentoAsync(contaId, request.Data))
            .ReturnsAsync((fatura, false, null));

        var compraCapturada = (Lancamento?)null;
        _mockLancamentoRepository
            .Setup(r => r.Adicionar(It.IsAny<Lancamento>()))
            .Callback<Lancamento>(c => compraCapturada = c);

        // Act - nao informar contaFixaId (ou passar null explicitamente)
        var (sucesso, compra, erro) = await _service.CriarCompraAsync(contaId, request);

        // Assert - deve ter ContaFixaId = null
        Assert.True(sucesso);
        Assert.NotNull(compra);
        Assert.Null(compra.ContaFixaId);
        Assert.Null(compraCapturada?.ContaFixaId);
    }

    #endregion
}
