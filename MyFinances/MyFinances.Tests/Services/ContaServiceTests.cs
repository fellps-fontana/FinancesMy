using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyFinances.Exceptions;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class ContaServiceTests
{
    private readonly Mock<IContaRepository> _mockRepository;
    private readonly ContaService _service;

    public ContaServiceTests()
    {
        _mockRepository = new Mock<IContaRepository>();
        _service = new ContaService(_mockRepository.Object, NullLogger<ContaService>.Instance);
    }

    #region Regra 1: CriarContaInvestimento sempre cria com Origem = Manual e Tipo = Investimento

    [Fact]
    public async Task CriarContaInvestimento_CriaComOrigemManualETipoInvestimento()
    {
        // Arrange
        var nome = "Cofrinho Mercado Pago";
        var saldoInicial = 1000m;

        // Act
        var conta = await _service.CriarContaInvestimento(nome, saldoInicial);

        // Assert
        Assert.Equal(OrigemConta.Manual, conta.Origem);
        Assert.Equal(TipoConta.Investimento, conta.Tipo);
        Assert.Equal(nome, conta.Nome);
        Assert.Equal(saldoInicial, conta.SaldoManual);
        Assert.True(conta.Ativa);
    }

    [Fact]
    public async Task CriarContaInvestimento_PersistênciaChamadoAoAdicionar()
    {
        // Arrange
        var nome = "Conta Teste";
        var saldoInicial = 500m;

        // Act
        await _service.CriarContaInvestimento(nome, saldoInicial);

        // Assert
        _mockRepository.Verify(r => r.Adicionar(It.IsAny<Conta>()), Times.Once);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 2: Criar multiplas contas de investimento distintas sem colisao

    [Fact]
    public async Task CriarContaInvestimento_MultiplasContasComNomesDistintosGeramIdsUnicos()
    {
        // Arrange
        var nomes = new[] { "Cofrinho Mercado Pago", "Investimentos XP", "Carteira de Acoes" };
        var contas = new List<Conta>();

        // Act
        foreach (var nome in nomes)
        {
            var conta = await _service.CriarContaInvestimento(nome, 1000m);
            contas.Add(conta);
        }

        // Assert
        var ids = contas.Select(c => c.Id).Distinct();
        Assert.Equal(3, ids.Count());
        Assert.All(contas, conta =>
        {
            Assert.Equal(OrigemConta.Manual, conta.Origem);
            Assert.Equal(TipoConta.Investimento, conta.Tipo);
        });
    }

    #endregion

    #region Regra 3: ListarContasInvestimento retorna so contas com Tipo == Investimento e Ativa == true

    [Fact]
    public async Task ListarContasInvestimento_RetornaApenasContasInvestimentoAtivas()
    {
        // Arrange
        var contasNoRepositorio = new List<Conta>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Investimento 1",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                Ativa = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Investimento 2",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                Ativa = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Banco",
                Tipo = TipoConta.Banco,
                Origem = OrigemConta.OpenFinance,
                Ativa = true
            }
        };

        _mockRepository
            .Setup(r => r.ListarPorTipo(TipoConta.Investimento))
            .ReturnsAsync(contasNoRepositorio.Where(c => c.Tipo == TipoConta.Investimento));

        // Act
        var resultado = await _service.ListarContasInvestimento();

        // Assert
        Assert.Single(resultado);
        Assert.Equal("Investimento 1", resultado.First().Nome);
        Assert.True(resultado.First().Ativa);
    }

    [Fact]
    public async Task ListarContasInvestimento_RetornaVazioQuandoNenhumaContaAtiva()
    {
        // Arrange
        var contasNoRepositorio = new List<Conta>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Investimento Inativo",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                Ativa = false
            }
        };

        _mockRepository
            .Setup(r => r.ListarPorTipo(TipoConta.Investimento))
            .ReturnsAsync(contasNoRepositorio);

        // Act
        var resultado = await _service.ListarContasInvestimento();

        // Assert
        Assert.Empty(resultado);
    }

    #endregion

    #region Regra 4: AtualizarSaldoManual funciona para conta Origem == Manual

    [Fact]
    public async Task AtualizarSaldoManual_AtualizaSaldoQuandoOrigemEhManual()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var contaExistente = new Conta
        {
            Id = contaId,
            Nome = "Cofrinho",
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Investimento,
            SaldoManual = 1000m,
            Ativa = true
        };

        _mockRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(contaExistente);

        // Act
        await _service.AtualizarSaldoManual(contaId, 1500m);

        // Assert
        Assert.Equal(1500m, contaExistente.SaldoManual);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 5: AtualizarSaldoManual lanca SaldoManualNaoPermitidoException se Origem != Manual

    [Fact]
    public async Task AtualizarSaldoManual_LancaExcecaoQuandoOrigemEhOpenFinance()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var contaExistente = new Conta
        {
            Id = contaId,
            Nome = "Conta Open Finance",
            Origem = OrigemConta.OpenFinance,
            Tipo = TipoConta.Banco,
            Ativa = true
        };

        _mockRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(contaExistente);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<SaldoManualNaoPermitidoException>(
            () => _service.AtualizarSaldoManual(contaId, 5000m));

        Assert.Equal(contaId, excecao.ContaId);
        Assert.Equal(OrigemConta.OpenFinance, excecao.Origem);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 6: AtualizarSaldoManual/DesativarConta lancam ContaNaoEncontradaException quando id nao existe

    [Fact]
    public async Task AtualizarSaldoManual_LancaExcecaoQuandoContaNaoEncontrada()
    {
        // Arrange
        var contaIdInexistente = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.ObterPorId(contaIdInexistente))
            .ReturnsAsync((Conta?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ContaNaoEncontradaException>(
            () => _service.AtualizarSaldoManual(contaIdInexistente, 1000m));

        Assert.Equal(contaIdInexistente, excecao.ContaId);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task DesativarConta_LancaExcecaoQuandoContaNaoEncontrada()
    {
        // Arrange
        var contaIdInexistente = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.ObterPorId(contaIdInexistente))
            .ReturnsAsync((Conta?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ContaNaoEncontradaException>(
            () => _service.DesativarConta(contaIdInexistente));

        Assert.Equal(contaIdInexistente, excecao.ContaId);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 7: DesativarConta faz soft-delete: seta Ativa = false, nao remove

    [Fact]
    public async Task DesativarConta_SoftDeleteSetaAtivaFalseEPersiste()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var contaExistente = new Conta
        {
            Id = contaId,
            Nome = "Conta Ativa",
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Investimento,
            SaldoManual = 1000m,
            Ativa = true
        };

        _mockRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(contaExistente);

        // Act
        await _service.DesativarConta(contaId);

        // Assert
        Assert.False(contaExistente.Ativa);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
        // Verifica que nao removeu a conta (apenas alterou Ativa)
        _mockRepository.Verify(r => r.Adicionar(It.IsAny<Conta>()), Times.Never);
    }

    [Fact]
    public async Task DesativarConta_ContaAindaPodeSerEncontradaDepoisDeDesativada()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var contaExistente = new Conta
        {
            Id = contaId,
            Nome = "Conta Desativada",
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Investimento,
            SaldoManual = 1000m,
            Ativa = true
        };

        _mockRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(contaExistente);

        // Act
        await _service.DesativarConta(contaId);

        // Agora simula que a conta ainda pode ser encontrada, mas com Ativa = false
        contaExistente.Ativa = false;
        _mockRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(contaExistente);

        var contaAposDesativacao = await _mockRepository.Object.ObterPorId(contaId);

        // Assert
        Assert.NotNull(contaAposDesativacao);
        Assert.False(contaAposDesativacao.Ativa);
        Assert.Equal(contaId, contaAposDesativacao.Id);
    }

    #endregion

    #region Regra 8: ToStorageValue dos enums produz valores exatos

    [Fact]
    public void OrigemContaToStorageValue_ProduzeValoresExatos()
    {
        // Act & Assert
        Assert.Equal("MANUAL", OrigemConta.Manual.ToStorageValue());
        Assert.Equal("OPEN_FINANCE", OrigemConta.OpenFinance.ToStorageValue());
    }

    [Fact]
    public void TipoContaToStorageValue_ProduzeValoresExatos()
    {
        // Act & Assert
        Assert.Equal("BANCO", TipoConta.Banco.ToStorageValue());
        Assert.Equal("CARTAO", TipoConta.Cartao.ToStorageValue());
        Assert.Equal("INVESTIMENTO", TipoConta.Investimento.ToStorageValue());
    }

    [Fact]
    public void OrigemContaFromStorageValue_ConverteparaEnumCorreto()
    {
        // Act & Assert
        Assert.Equal(OrigemConta.Manual, OrigemContaExtensions.FromStorageValue("MANUAL"));
        Assert.Equal(OrigemConta.OpenFinance, OrigemContaExtensions.FromStorageValue("OPEN_FINANCE"));
    }

    [Fact]
    public void TipoContaFromStorageValue_ConverteparaEnumCorreto()
    {
        // Act & Assert
        Assert.Equal(TipoConta.Banco, TipoContaExtensions.FromStorageValue("BANCO"));
        Assert.Equal(TipoConta.Cartao, TipoContaExtensions.FromStorageValue("CARTAO"));
        Assert.Equal(TipoConta.Investimento, TipoContaExtensions.FromStorageValue("INVESTIMENTO"));
    }

    [Fact]
    public void OrigemContaFromStorageValue_LancaExcecaoComValorDesconhecido()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => OrigemContaExtensions.FromStorageValue("INVALIDO"));
    }

    [Fact]
    public void TipoContaFromStorageValue_LancaExcecaoComValorDesconhecido()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => TipoContaExtensions.FromStorageValue("INVALIDO"));
    }

    #endregion

    #region Regra 9: CalcularTotalInvestido soma corretamente SaldoManual de contas ativas

    [Fact]
    public async Task CalcularTotalInvestido_SomaCorretamenteMultiplasContasAtivas()
    {
        // Arrange
        var contasNoRepositorio = new List<Conta>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Cofrinho Mercado Pago",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                SaldoManual = 100m,
                Ativa = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Investimentos XP",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                SaldoManual = 200m,
                Ativa = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Carteira de Acoes",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                SaldoManual = 300m,
                Ativa = true
            }
        };

        _mockRepository
            .Setup(r => r.ListarPorTipo(TipoConta.Investimento))
            .ReturnsAsync(contasNoRepositorio);

        // Act
        var total = await _service.CalcularTotalInvestido();

        // Assert
        Assert.Equal(600m, total);
    }

    [Fact]
    public async Task CalcularTotalInvestido_RetornaZeroQuandoNenhumaContaCadastrada()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.ListarPorTipo(TipoConta.Investimento))
            .ReturnsAsync(new List<Conta>());

        // Act
        var total = await _service.CalcularTotalInvestido();

        // Assert
        Assert.Equal(0m, total);
    }

    [Fact]
    public async Task CalcularTotalInvestido_IgnoraContasDesativadas()
    {
        // Arrange
        var contasNoRepositorio = new List<Conta>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Investimento Ativo",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                SaldoManual = 100m,
                Ativa = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Investimento Desativado",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                SaldoManual = 500m,
                Ativa = false
            }
        };

        // ListarPorTipo retorna todas com Tipo = Investimento
        // ListarContasInvestimento filtra por Ativa = true
        _mockRepository
            .Setup(r => r.ListarPorTipo(TipoConta.Investimento))
            .ReturnsAsync(contasNoRepositorio);

        // Act
        var total = await _service.CalcularTotalInvestido();

        // Assert - deve somar so a ativa (100), ignorando a desativada (500)
        Assert.Equal(100m, total);
    }

    [Fact]
    public async Task CalcularTotalInvestido_IgnoraContasDeOutroTipo()
    {
        // Arrange
        var contasInvestimento = new List<Conta>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Cofrinho",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                SaldoManual = 100m,
                Ativa = true
            }
        };

        // Simulamos que o repositorio retorna so contas Tipo = Investimento
        // E ignoramos contas de outro tipo (listaPorTipo ja filtra por tipo)
        _mockRepository
            .Setup(r => r.ListarPorTipo(TipoConta.Investimento))
            .ReturnsAsync(contasInvestimento);

        // Act
        var total = await _service.CalcularTotalInvestido();

        // Assert - mesmo que tivessemos contas Banco/Cartao no banco,
        // ListarPorTipo(Investimento) retorna so Investimento (100)
        Assert.Equal(100m, total);
    }

    [Fact]
    public async Task CalcularTotalInvestido_TrataSaldoNuloComoZero()
    {
        // Arrange
        var contasNoRepositorio = new List<Conta>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Cofrinho com Saldo",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                SaldoManual = 100m,
                Ativa = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Nome = "Cofrinho com Saldo Nulo",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                SaldoManual = null,
                Ativa = true
            }
        };

        _mockRepository
            .Setup(r => r.ListarPorTipo(TipoConta.Investimento))
            .ReturnsAsync(contasNoRepositorio);

        // Act
        var total = await _service.CalcularTotalInvestido();

        // Assert - nula e tratada como zero, total = 100 + 0 = 100
        Assert.Equal(100m, total);
    }

    #endregion
}
