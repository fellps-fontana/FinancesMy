using Moq;
using MyFinances.Exceptions;
using MyFinances.Models;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class AtivoServiceTests
{
    private readonly Mock<IAtivoRepository> _mockAtivoRepository;
    private readonly Mock<IContaRepository> _mockContaRepository;
    private readonly AtivoService _service;

    public AtivoServiceTests()
    {
        _mockAtivoRepository = new Mock<IAtivoRepository>();
        _mockContaRepository = new Mock<IContaRepository>();
        _service = new AtivoService(_mockAtivoRepository.Object, _mockContaRepository.Object);
    }

    #region Regra 1: Compra de ticker novo cria Ativo com PrecoMedio == PrecoAtual == precoDaCompra e Quantidade correta

    [Fact]
    public async Task RegistrarCompra_TickerNovo_CriaAtivoComValoresCorretos()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var ticker = "PETR4";
        var quantidade = 100m;
        var precoUnitario = 25.50m;
        var data = new DateOnly(2026, 7, 1);
        var nome = "Petrobras";

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Carteira XP",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        _mockAtivoRepository
            .Setup(r => r.ObterAtivoAtivoPorTicker(contaId, ticker))
            .ReturnsAsync((Ativo?)null);

        // Act
        var ativo = await _service.RegistrarCompra(contaId, ticker, quantidade, precoUnitario, data, nome);

        // Assert
        Assert.Equal(ticker, ativo.Ticker);
        Assert.Equal(quantidade, ativo.Quantidade);
        Assert.Equal(precoUnitario, ativo.PrecoMedio);
        Assert.Equal(precoUnitario, ativo.PrecoAtual);
        Assert.True(ativo.Ativa);
        Assert.Equal(nome, ativo.Nome);

        _mockAtivoRepository.Verify(r => r.Adicionar(It.IsAny<Ativo>()), Times.Once);
        _mockAtivoRepository.Verify(r => r.AdicionarMovimentacao(It.IsAny<MovimentacaoAtivo>()), Times.Once);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 2: Compra de ticker existente incrementa Quantidade e recalcula PrecoMedio pela formula ponderada

    [Fact]
    public async Task RegistrarCompra_TickerExistente_RecalculaPrecoMedioPonderado()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var ativoId = Guid.NewGuid();
        var ticker = "PETR4";

        // Estado inicial: 100 acoes a 20 reais = media 20
        var ativoExistente = new Ativo
        {
            Id = ativoId,
            ContaId = contaId,
            Ticker = ticker,
            Quantidade = 100m,
            PrecoMedio = 20m,
            PrecoAtual = 20m,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Carteira XP",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        _mockAtivoRepository
            .Setup(r => r.ObterAtivoAtivoPorTicker(contaId, ticker))
            .ReturnsAsync(ativoExistente);

        var data = new DateOnly(2026, 7, 2);
        var quantidadeCompra = 50m;
        var precoCompra = 22m;

        // Act
        var ativo = await _service.RegistrarCompra(contaId, ticker, quantidadeCompra, precoCompra, data, null);

        // Assert
        // Calculo esperado: (100 * 20 + 50 * 22) / (100 + 50) = (2000 + 1100) / 150 = 3100 / 150 = 20.6666...
        var precoMedioEsperado = (100m * 20m + 50m * 22m) / (100m + 50m);
        Assert.Equal(150m, ativo.Quantidade);
        Assert.Equal(precoMedioEsperado, ativo.PrecoMedio, precision: 5);
        Assert.Equal(precoCompra, ativo.PrecoAtual);

        _mockAtivoRepository.Verify(r => r.AdicionarMovimentacao(It.IsAny<MovimentacaoAtivo>()), Times.Once);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 3: Cenario completo com recalculo apos venda — quantidade POS-VENDA e peso na proxima media

    [Fact]
    public async Task RegistrarVenda_ComCompraSequente_UsaQuantidadePosVendaComopesoDoPrecoMedio()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var ativoId = Guid.NewGuid();
        var ticker = "PETR4";

        // Passo 1: compra 100@20
        var ativoApos1Compra = new Ativo
        {
            Id = ativoId,
            ContaId = contaId,
            Ticker = ticker,
            Quantidade = 100m,
            PrecoMedio = 20m,
            PrecoAtual = 20m,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        // Passo 2: compra 50@22 (media agora = 20.6667)
        var precoMedioApos2Compra = (100m * 20m + 50m * 22m) / 150m; // ~20.6667
        var ativoApos2Compra = new Ativo
        {
            Id = ativoId,
            ContaId = contaId,
            Ticker = ticker,
            Quantidade = 150m,
            PrecoMedio = precoMedioApos2Compra,
            PrecoAtual = 22m,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Carteira XP",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Passo 3: venda de 50 unidades
        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativoApos2Compra);

        // Act venda
        var ativoAposVenda = await _service.RegistrarVenda(ativoId, 50m, 20m, new DateOnly(2026, 7, 3), null);

        // Assert venda
        Assert.Equal(100m, ativoAposVenda.Quantidade);
        Assert.Equal(precoMedioApos2Compra, ativoAposVenda.PrecoMedio, precision: 5); // Media NAO muda em venda
        Assert.True(ativoAposVenda.Ativa);

        // Passo 4: compra 30@21 (usando quantidade POS-VENDA = 100 como peso)
        // Reset mock para retornar o ativo com a quantidade pos-venda
        var ativoAposVendaParaProximaCompra = new Ativo
        {
            Id = ativoId,
            ContaId = contaId,
            Ticker = ticker,
            Quantidade = 100m,
            PrecoMedio = precoMedioApos2Compra,
            PrecoAtual = 22m,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        _mockAtivoRepository
            .Setup(r => r.ObterAtivoAtivoPorTicker(contaId, ticker))
            .ReturnsAsync(ativoAposVendaParaProximaCompra);

        // Act compra
        var ativoApos3Compra = await _service.RegistrarCompra(contaId, ticker, 30m, 21m, new DateOnly(2026, 7, 4), null);

        // Assert compra
        // Calculo esperado: (100 * 20.6667 + 30 * 21) / (100 + 30)
        var precoMedioEsperado = (100m * precoMedioApos2Compra + 30m * 21m) / 130m;
        Assert.Equal(130m, ativoApos3Compra.Quantidade);
        Assert.Equal(precoMedioEsperado, ativoApos3Compra.PrecoMedio, precision: 5);
        Assert.Equal(21m, ativoApos3Compra.PrecoAtual);
    }

    #endregion

    #region Regra 4: PrecoAtual sempre reflete o preco da ULTIMA compra, nao muda em venda

    [Fact]
    public async Task RegistrarVenda_NaoAlteraPrecoAtual()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var precoAtualAntes = 25.50m;

        var ativo = new Ativo
        {
            Id = ativoId,
            Quantidade = 100m,
            PrecoMedio = 20m,
            PrecoAtual = precoAtualAntes,
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativo);

        // Act
        var ativoAposVenda = await _service.RegistrarVenda(ativoId, 30m, 18m, new DateOnly(2026, 7, 5), null);

        // Assert
        Assert.Equal(precoAtualAntes, ativoAposVenda.PrecoAtual);
    }

    #endregion

    #region Regra 5: Venda parcial reduz Quantidade e NAO altera PrecoMedio nem PrecoAtual

    [Fact]
    public async Task RegistrarVenda_Parcial_ApenasReduzQuantidade()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var precoMedioAntes = 22.35m;
        var precoAtualAntes = 25m;

        var ativo = new Ativo
        {
            Id = ativoId,
            Quantidade = 100m,
            PrecoMedio = precoMedioAntes,
            PrecoAtual = precoAtualAntes,
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativo);

        // Act
        var ativoAposVenda = await _service.RegistrarVenda(ativoId, 40m, 24m, new DateOnly(2026, 7, 6), null);

        // Assert
        Assert.Equal(60m, ativoAposVenda.Quantidade);
        Assert.Equal(precoMedioAntes, ativoAposVenda.PrecoMedio);
        Assert.Equal(precoAtualAntes, ativoAposVenda.PrecoAtual);
        Assert.True(ativoAposVenda.Ativa);
    }

    #endregion

    #region Regra 6: Venda total (Quantidade = 0) seta Ativa = false

    [Fact]
    public async Task RegistrarVenda_Completa_SetaAtivaFalse()
    {
        // Arrange
        var ativoId = Guid.NewGuid();

        var ativo = new Ativo
        {
            Id = ativoId,
            Quantidade = 50m,
            PrecoMedio = 20m,
            PrecoAtual = 22m,
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativo);

        // Act
        var ativoAposVenda = await _service.RegistrarVenda(ativoId, 50m, 21m, new DateOnly(2026, 7, 7), null);

        // Assert
        Assert.Equal(0m, ativoAposVenda.Quantidade);
        Assert.False(ativoAposVenda.Ativa);
    }

    #endregion

    #region Regra 7: Venda > quantidade disponivel lanca QuantidadeVendaInvalidaException e NAO altera nada

    [Fact]
    public async Task RegistrarVenda_QuantidadeMaiorQueDisponivel_LancaExcecaoESemAlteracao()
    {
        // Arrange
        var ativoId = Guid.NewGuid();
        var quantidadeDisponivel = 50m;

        var ativo = new Ativo
        {
            Id = ativoId,
            Quantidade = quantidadeDisponivel,
            PrecoMedio = 20m,
            PrecoAtual = 22m,
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativo);

        var quantidadeVenda = 100m;

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<QuantidadeVendaInvalidaException>(
            () => _service.RegistrarVenda(ativoId, quantidadeVenda, 21m, new DateOnly(2026, 7, 8), null));

        Assert.Equal(ativoId, excecao.AtivoId);
        Assert.Equal(quantidadeVenda, excecao.QuantidadeVenda);
        Assert.Equal(quantidadeDisponivel, excecao.QuantidadeDisponivel);

        // Verifica que nenhuma movimentacao foi registrada
        _mockAtivoRepository.Verify(r => r.AdicionarMovimentacao(It.IsAny<MovimentacaoAtivo>()), Times.Never);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 8: Compra do mesmo ticker apos venda total cria Ativo NOVO (nao reaproveita o antigo)

    [Fact]
    public async Task RegistrarCompra_AposvVendaTotal_CriaNovoAtivo()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var ativoIdAntigo = Guid.NewGuid();
        var ticker = "PETR4";

        var conta = new Conta
        {
            Id = contaId,
            Nome = "Carteira XP",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // ObterAtivoAtivoPorTicker retorna null porque o ativo antigo esta Ativa=false
        _mockAtivoRepository
            .Setup(r => r.ObterAtivoAtivoPorTicker(contaId, ticker))
            .ReturnsAsync((Ativo?)null);

        // Act
        var ativoNovo = await _service.RegistrarCompra(contaId, ticker, 50m, 25m, new DateOnly(2026, 7, 9), "Petrobras Novo");

        // Assert
        Assert.NotEqual(ativoIdAntigo, ativoNovo.Id);
        Assert.Equal(ticker, ativoNovo.Ticker);
        Assert.Equal(50m, ativoNovo.Quantidade);
        Assert.Equal(25m, ativoNovo.PrecoMedio);
        Assert.True(ativoNovo.Ativa);

        _mockAtivoRepository.Verify(r => r.Adicionar(It.IsAny<Ativo>()), Times.Once);
    }

    #endregion

    #region Regra 9: RegistrarVenda nunca toca Conta/Lancamento/Transferencia — so IAtivoRepository

    [Fact]
    public async Task RegistrarVenda_NaoTocaIContaRepository()
    {
        // Arrange
        var ativoId = Guid.NewGuid();

        var ativo = new Ativo
        {
            Id = ativoId,
            Quantidade = 100m,
            PrecoMedio = 20m,
            PrecoAtual = 22m,
            Ativa = true
        };

        _mockAtivoRepository
            .Setup(r => r.ObterPorId(ativoId))
            .ReturnsAsync(ativo);

        // Act
        await _service.RegistrarVenda(ativoId, 30m, 21m, new DateOnly(2026, 7, 10), null);

        // Assert
        _mockContaRepository.Verify(r => r.ObterPorId(It.IsAny<Guid>()), Times.Never);
        _mockAtivoRepository.Verify(r => r.AdicionarMovimentacao(It.IsAny<MovimentacaoAtivo>()), Times.Once);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 10: RegistrarCompra em conta inexistente e nao-Investimento

    [Fact]
    public async Task RegistrarCompra_ContaNaoEncontrada_LancaExcecao()
    {
        // Arrange
        var contaIdInexistente = Guid.NewGuid();

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaIdInexistente))
            .ReturnsAsync((Conta?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ContaNaoEncontradaException>(
            () => _service.RegistrarCompra(contaIdInexistente, "PETR4", 100m, 20m, new DateOnly(2026, 7, 11), null));

        Assert.Equal(contaIdInexistente, excecao.ContaId);
        _mockAtivoRepository.Verify(r => r.Adicionar(It.IsAny<Ativo>()), Times.Never);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task RegistrarCompra_ContaNaoEhInvestimento_LancaExcecao()
    {
        // Arrange
        var contaId = Guid.NewGuid();

        var contaBanco = new Conta
        {
            Id = contaId,
            Nome = "Conta Corrente",
            Tipo = TipoConta.Banco,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(contaBanco);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ContaNaoEhInvestimentoException>(
            () => _service.RegistrarCompra(contaId, "PETR4", 100m, 20m, new DateOnly(2026, 7, 12), null));

        Assert.Equal(contaId, excecao.ContaId);
        Assert.Equal(TipoConta.Banco, excecao.TipoConta);
        _mockAtivoRepository.Verify(r => r.Adicionar(It.IsAny<Ativo>()), Times.Never);
        _mockAtivoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 11: RegistrarCompra/RegistrarVenda com quantidade ou preco <= 0 lancam ValorInvalidoException

    [Fact]
    public async Task RegistrarCompra_QuantidadeZero_LancaValorInvalidoException()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Carteira XP",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarCompra(contaId, "PETR4", 0m, 20m, new DateOnly(2026, 7, 13), null));

        Assert.Equal("quantidade", excecao.NomeCampo);
        _mockAtivoRepository.Verify(r => r.Adicionar(It.IsAny<Ativo>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarCompra_QuantidadeNegativa_LancaValorInvalidoException()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Carteira XP",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarCompra(contaId, "PETR4", -50m, 20m, new DateOnly(2026, 7, 14), null));

        Assert.Equal("quantidade", excecao.NomeCampo);
    }

    [Fact]
    public async Task RegistrarCompra_PrecoUnitarioZero_LancaValorInvalidoException()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Carteira XP",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarCompra(contaId, "PETR4", 100m, 0m, new DateOnly(2026, 7, 15), null));

        Assert.Equal("precoUnitario", excecao.NomeCampo);
    }

    [Fact]
    public async Task RegistrarCompra_PrecoUnitarioNegativo_LancaValorInvalidoException()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = new Conta
        {
            Id = contaId,
            Nome = "Carteira XP",
            Tipo = TipoConta.Investimento,
            Origem = OrigemConta.Manual,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarCompra(contaId, "PETR4", 100m, -25m, new DateOnly(2026, 7, 16), null));

        Assert.Equal("precoUnitario", excecao.NomeCampo);
    }

    [Fact]
    public async Task RegistrarVenda_QuantidadeZero_LancaValorInvalidoException()
    {
        // Arrange
        var ativoId = Guid.NewGuid();

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarVenda(ativoId, 0m, 20m, new DateOnly(2026, 7, 17), null));

        Assert.Equal("quantidade", excecao.NomeCampo);
        _mockAtivoRepository.Verify(r => r.ObterPorId(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarVenda_QuantidadeNegativa_LancaValorInvalidoException()
    {
        // Arrange
        var ativoId = Guid.NewGuid();

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarVenda(ativoId, -30m, 20m, new DateOnly(2026, 7, 18), null));

        Assert.Equal("quantidade", excecao.NomeCampo);
    }

    [Fact]
    public async Task RegistrarVenda_PrecoUnitarioZero_LancaValorInvalidoException()
    {
        // Arrange
        var ativoId = Guid.NewGuid();

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarVenda(ativoId, 50m, 0m, new DateOnly(2026, 7, 19), null));

        Assert.Equal("precoUnitario", excecao.NomeCampo);
    }

    [Fact]
    public async Task RegistrarVenda_PrecoUnitarioNegativo_LancaValorInvalidoException()
    {
        // Arrange
        var ativoId = Guid.NewGuid();

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.RegistrarVenda(ativoId, 50m, -18m, new DateOnly(2026, 7, 20), null));

        Assert.Equal("precoUnitario", excecao.NomeCampo);
    }

    #endregion
}
