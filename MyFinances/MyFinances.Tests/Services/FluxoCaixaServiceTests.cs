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

    // Testes para CalcularTotalRecebidoNoMes (REGRA: soma Credit+Pago, exclui Transferencia)

    [Fact]
    public async Task CalcularTotalRecebidoNoMes_SomaNormalmenteLancamentosCreditPago()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Salario",
                Valor = 5000m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Freelance",
                Valor = 1500m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 10),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalRecebidoNoMes(ano, mes);

        // Assert
        Assert.Equal(6500m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalRecebidoNoMes_ExcluiTransferenciaPerna()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var transferId = Guid.NewGuid();
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Salario",
                Valor = 5000m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Transferencia entrada",
                Valor = 1000m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 8),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = transferId,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalRecebidoNoMes(ano, mes);

        // Assert
        Assert.Equal(5000m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalRecebidoNoMes_ExcluiEmprestimoRecebidoPerna()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var emprestimoId = Guid.NewGuid();
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Salario",
                Valor = 5000m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Emprestimo amigo recebido",
                Valor = 500m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 10),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = emprestimoId,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalRecebidoNoMes(ano, mes);

        // Assert
        Assert.Equal(5000m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalRecebidoNoMes_IgnoraPendente()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Salario",
                Valor = 5000m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Bonus (ainda pendente)",
                Valor = 2000m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 15),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalRecebidoNoMes(ano, mes);

        // Assert
        Assert.Equal(5000m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalRecebidoNoMes_IgnoraDebit()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Salario",
                Valor = 5000m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Aluguel",
                Valor = 1000m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 10),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalRecebidoNoMes(ano, mes);

        // Assert
        Assert.Equal(5000m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalRecebidoNoMes_ListaVaziaRetornaZero()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(new List<Lancamento>());

        // Act
        var resultado = await _service.CalcularTotalRecebidoNoMes(ano, mes);

        // Assert
        Assert.Equal(0m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    // Testes para CalcularTotalPagoNoMes (REGRA: soma Debit+Pago, exclui Transferencia)

    [Fact]
    public async Task CalcularTotalPagoNoMes_SomaNormalmenteLancamentosDebitPago()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Aluguel",
                Valor = 1500m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Supermercado",
                Valor = 350m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 10),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalPagoNoMes(ano, mes);

        // Assert
        Assert.Equal(1850m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalPagoNoMes_ExcluiTransferenciaPerna()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var transferId = Guid.NewGuid();
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Aluguel",
                Valor = 1500m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Transferencia saida",
                Valor = 500m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 8),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = transferId,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalPagoNoMes(ano, mes);

        // Assert
        Assert.Equal(1500m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalPagoNoMes_ExcluiEmprestimoDadoPerna()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var emprestimoId = Guid.NewGuid();
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Aluguel",
                Valor = 1500m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Emprestimo amigo dado",
                Valor = 300m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 10),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = emprestimoId,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalPagoNoMes(ano, mes);

        // Assert
        Assert.Equal(1500m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalPagoNoMes_IgnoraPendente()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Aluguel",
                Valor = 1500m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Internet (pendente)",
                Valor = 100m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 15),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalPagoNoMes(ano, mes);

        // Assert
        Assert.Equal(1500m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalPagoNoMes_IgnoraCredit()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Aluguel",
                Valor = 1500m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Salario",
                Valor = 5000m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 10),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalPagoNoMes(ano, mes);

        // Assert
        Assert.Equal(1500m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalPagoNoMes_ListaVaziaRetornaZero()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(new List<Lancamento>());

        // Act
        var resultado = await _service.CalcularTotalPagoNoMes(ano, mes);

        // Assert
        Assert.Equal(0m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    // Testes para CalcularTotalAPagarNoMes (REGRA: soma Debit+Pendente, exclui Transferencia)

    [Fact]
    public async Task CalcularTotalAPagarNoMes_SomaNormalmenteLancamentosDebitPendente()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Fatura cartao",
                Valor = 2000m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 20),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Conta telefone",
                Valor = 150m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 25),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalAPagarNoMes(ano, mes);

        // Assert
        Assert.Equal(2150m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalAPagarNoMes_ExcluiTransferenciaPendente()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var transferId = Guid.NewGuid();
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Fatura cartao",
                Valor = 2000m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 20),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Transferencia futura",
                Valor = 500m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 25),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = transferId,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalAPagarNoMes(ano, mes);

        // Assert
        Assert.Equal(2000m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalAPagarNoMes_ExcluiEmprestimoFuturoPerna()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var emprestimoId = Guid.NewGuid();
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Fatura cartao",
                Valor = 2000m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 20),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Emprestimo amigo futuro",
                Valor = 200m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 28),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = emprestimoId,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalAPagarNoMes(ano, mes);

        // Assert
        Assert.Equal(2000m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalAPagarNoMes_IgnoraPago()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Fatura cartao (paga)",
                Valor = 2000m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 5),
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Conta de agua",
                Valor = 150m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 25),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalAPagarNoMes(ano, mes);

        // Assert
        Assert.Equal(150m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalAPagarNoMes_IgnoraCredit()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;
        var lancamentos = new List<Lancamento>
        {
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Fatura cartao",
                Valor = 2000m,
                Tipo = TipoLancamento.Debit,
                Data = new DateOnly(2025, 2, 20),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            },
            new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                Descricao = "Bonus futuro",
                Valor = 3000m,
                Tipo = TipoLancamento.Credit,
                Data = new DateOnly(2025, 2, 28),
                Status = StatusLancamento.Pendente,
                Manual = true,
                Oculto = false,
                TransferenciaId = null,
                FaturaId = null
            }
        };

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(lancamentos);

        // Act
        var resultado = await _service.CalcularTotalAPagarNoMes(ano, mes);

        // Assert
        Assert.Equal(2000m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }

    [Fact]
    public async Task CalcularTotalAPagarNoMes_ListaVaziaRetornaZero()
    {
        // Arrange
        var ano = 2025;
        var mes = 2;

        _lancamentoRepositoryMock.Setup(r => r.ListarParaFluxoCaixaDoMes(ano, mes))
            .ReturnsAsync(new List<Lancamento>());

        // Act
        var resultado = await _service.CalcularTotalAPagarNoMes(ano, mes);

        // Assert
        Assert.Equal(0m, resultado);
        _lancamentoRepositoryMock.Verify(r => r.ListarParaFluxoCaixaDoMes(ano, mes), Times.Once);
    }
}
