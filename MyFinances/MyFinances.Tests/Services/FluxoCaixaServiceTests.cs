using Moq;
using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class FluxoCaixaServiceTests
{
    private readonly Mock<ILancamentoRepository> _lancamentoRepositoryMock;
    private readonly FluxoCaixaService _service;

    public FluxoCaixaServiceTests()
    {
        _lancamentoRepositoryMock = new Mock<ILancamentoRepository>();
        _service = new FluxoCaixaService(_lancamentoRepositoryMock.Object);
    }

    [Fact]
    public async Task ListarFluxoCaixa_SemFiltro_RetornaListaMapeada()
    {
        // Arrange
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Lancamento 1",
                Valor = 100m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 1, 10),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Lancamento 2",
                Valor = 200m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 1, 15),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixa(null))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.ListarFluxoCaixa(null);

        // Assert
        Assert.NotNull(resultado);
        var listaResultado = resultado.ToList();
        Assert.Equal(2, listaResultado.Count);

        Assert.Equal(lancamentos[0].Descricao, listaResultado[0].Descricao);
        Assert.Equal(lancamentos[0].Valor, listaResultado[0].Valor);
        Assert.Equal("DEBIT", listaResultado[0].Tipo);
        Assert.Equal("PAGO", listaResultado[0].Status);

        Assert.Equal(lancamentos[1].Descricao, listaResultado[1].Descricao);
        Assert.Equal(lancamentos[1].Valor, listaResultado[1].Valor);
        Assert.Equal("CREDIT", listaResultado[1].Tipo);
        Assert.Equal("PENDENTE", listaResultado[1].Status);

        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixa(null), Times.Once);
    }

    [Fact]
    public async Task ListarFluxoCaixa_ComFiltePorConta_RetornaListaMapeada()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Descricao = "Lancamento da Conta",
                Valor = 500m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 1, 20),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixa(contaId))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.ListarFluxoCaixa(contaId);

        // Assert
        Assert.NotNull(resultado);
        var listaResultado = resultado.ToList();
        Assert.Single(listaResultado);

        Assert.Equal("Lancamento da Conta", listaResultado[0].Descricao);
        Assert.Equal(500m, listaResultado[0].Valor);
        Assert.Equal(contaId, listaResultado[0].ContaId);

        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixa(contaId), Times.Once);
    }

    [Fact]
    public async Task ListarFluxoCaixa_SemLancamentos_RetornaListaVazia()
    {
        // Arrange
        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixa(null))
            .ReturnsAsync(new List<Lancamento>());

        // Act
        var resultado = await _service.ListarFluxoCaixa(null);

        // Assert
        Assert.NotNull(resultado);
        Assert.Empty(resultado);

        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixa(null), Times.Once);
    }

    [Fact]
    public async Task ListarFluxoCaixa_MapeiaCorretamenteCamposDoDto()
    {
        // Arrange
        var lancamentoId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();

        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = lancamentoId,
                ContaId = contaId,
                CategoriaId = categoriaId,
                Descricao = "Despesa Importante",
                Valor = 750m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixa(null))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.ListarFluxoCaixa(null);

        // Assert
        var dto = resultado.First();
        Assert.Equal(lancamentoId, dto.Id);
        Assert.Equal(contaId, dto.ContaId);
        Assert.Equal(categoriaId, dto.CategoriaId);
        Assert.Equal("Despesa Importante", dto.Descricao);
        Assert.Equal(750m, dto.Valor);
        Assert.Equal("DEBIT", dto.Tipo);
        Assert.Equal(new DateOnly(2025, 2, 5), dto.Data);
        Assert.Equal("PENDENTE", dto.Status);
        Assert.True(dto.Manual);
        Assert.False(dto.Oculto);
    }

    [Fact]
    public async Task ListarFluxoCaixa_ComMultiplosLancamentos_DelegatoAoRepository()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Descricao = "L1",
                Valor = 100m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 1, 10),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Descricao = "L2",
                Valor = 200m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 1, 15),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Descricao = "L3",
                Valor = 300m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 1, 20),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixa(contaId))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.ListarFluxoCaixa(contaId);

        // Assert
        var listaResultado = resultado.ToList();
        Assert.Equal(3, listaResultado.Count);

        Assert.Equal("L1", listaResultado[0].Descricao);
        Assert.Equal("L2", listaResultado[1].Descricao);
        Assert.Equal("L3", listaResultado[2].Descricao);

        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixa(contaId), Times.Once);
    }

    [Fact]
    public async Task ListarFluxoCaixa_NuncaMudaValoresOuTipos()
    {
        // Arrange
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Teste",
                Valor = 123.45m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 1, 15),
                Status = StatusLancamento.Sugerido,
                Manual = false,
                Oculto = true
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixa(null))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.ListarFluxoCaixa(null);

        // Assert
        var dto = resultado.First();
        Assert.Equal(123.45m, dto.Valor);
        Assert.Equal("DEBIT", dto.Tipo);
        Assert.Equal("SUGERIDO", dto.Status);
        Assert.False(dto.Manual);
        Assert.True(dto.Oculto);
    }
}
