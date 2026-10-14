using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyFinances.Domain;
using MyFinances.DTOs.Conta;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using MyFinances.Tests.Helpers;
using Xunit;

namespace MyFinances.Tests.Services;

// Item 10.2: edicao de conta (nome, icone, cor, subtipo). Tipo imutavel.
public class ContaEditarServiceTests
{
    private readonly Mock<IContaRepository> _mockRepository;
    private readonly ContaService _service;

    public ContaEditarServiceTests()
    {
        _mockRepository = new Mock<IContaRepository>();
        _service = new ContaService(_mockRepository.Object, new FakeAtivoRepository(), NullLogger<ContaService>.Instance);
    }

    #region Regra 10.2: edita nome, icone, cor e subtipo de conta Banco

    [Fact]
    public async Task EditarConta_ContaBanco_AtualizaNomeIconeCorESubtipoPoupanca()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Banco, subtipo: SubtipoConta.Corrente);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest
        {
            Nome = "Nubank Poupanca",
            Subtipo = "Poupanca",
            Icone = "piggy",
            Cor = "#7C3AED"
        };

        // Act
        var resultado = await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Equal("Nubank Poupanca", conta.Nome);
        Assert.Equal("piggy", conta.Icone);
        Assert.Equal("#7C3AED", conta.Cor);
        Assert.Equal(SubtipoConta.Poupanca, conta.Subtipo);
        Assert.Equal("Nubank Poupanca", resultado.Nome);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task EditarConta_ContaBancoSemSubtipo_LimpaSubtipoParaNull()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Banco, subtipo: SubtipoConta.Corrente);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Banco sem subtipo", Subtipo = null };

        // Act
        await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Null(conta.Subtipo);
        Assert.Equal("Banco sem subtipo", conta.Nome);
    }

    #endregion

    #region Regra 10.1/10.2: subtipo so vale para Tipo = Banco (outros tipos ficam null)

    [Fact]
    public async Task EditarConta_ContaCartaoComSubtipo_SubtipoIgnoradoFicaNull()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Cartao Renomeado", Subtipo = "Poupanca" };

        // Act
        var resultado = await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Null(conta.Subtipo);
        Assert.Null(resultado.Subtipo);
        Assert.Equal("Cartao Renomeado", conta.Nome);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task EditarConta_ContaInvestimentoComSubtipo_SubtipoIgnoradoFicaNull()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Investimento);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Corretora Renomeada", Subtipo = "Corrente" };

        // Act
        var resultado = await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Null(conta.Subtipo);
        Assert.Null(resultado.Subtipo);
        Assert.Equal("Corretora Renomeada", conta.Nome);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 10.2: Tipo e imutavel

    [Fact]
    public async Task EditarConta_ContaBanco_TipoPermaneceBanco()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Banco, subtipo: SubtipoConta.Corrente);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Novo nome", Subtipo = "Poupanca" };

        // Act
        var resultado = await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Equal(TipoConta.Banco, conta.Tipo);
        Assert.Equal(TipoConta.Banco, resultado.Tipo);
    }

    [Fact]
    public async Task EditarConta_ContaCartao_TipoPermaneceCartao()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Novo nome" };

        // Act
        var resultado = await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Equal(TipoConta.Cartao, conta.Tipo);
        Assert.Equal(TipoConta.Cartao, resultado.Tipo);
    }

    #endregion

    #region Regra 10.2: Nome obrigatorio (vazio ou so espacos -> CampoObrigatorioException)

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EditarConta_NomeVazioOuSoEspacos_LancaCampoObrigatorioException(string nome)
    {
        // Arrange
        var conta = CriarConta(TipoConta.Banco, subtipo: SubtipoConta.Corrente);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = nome, Subtipo = "Poupanca" };

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.EditarConta(conta.Id, request));

        Assert.Equal("Nome", excecao.NomeCampo);
        Assert.Equal("Nome antigo", conta.Nome);
        Assert.Equal(SubtipoConta.Corrente, conta.Subtipo);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 10.1/10.2: subtipo informado e nao reconhecido -> SubtipoContaInvalidoException

    [Fact]
    public async Task EditarConta_SubtipoNaoReconhecidoEmBanco_LancaSubtipoContaInvalidoException()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Banco, subtipo: SubtipoConta.Corrente);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Nome novo", Subtipo = "SubtipoDesconhecido" };

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<SubtipoContaInvalidoException>(
            () => _service.EditarConta(conta.Id, request));

        Assert.Equal("SubtipoDesconhecido", excecao.Subtipo);
        Assert.Equal("Nome antigo", conta.Nome);
        Assert.Equal(SubtipoConta.Corrente, conta.Subtipo);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 10.2: conta inexistente (404) e conta inativa (409)

    [Fact]
    public async Task EditarConta_ContaInexistente_LancaContaNaoEncontradaException()
    {
        // Arrange
        var contaIdInexistente = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.ObterPorId(contaIdInexistente))
            .ReturnsAsync((Conta?)null);

        var request = new EditarContaRequest { Nome = "Qualquer" };

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ContaNaoEncontradaException>(
            () => _service.EditarConta(contaIdInexistente, request));

        Assert.Equal(contaIdInexistente, excecao.ContaId);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task EditarConta_ContaInativa_LancaContaInativaException()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Banco, ativa: false, subtipo: SubtipoConta.Corrente);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Tentativa em conta inativa", Subtipo = "Poupanca" };

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ContaInativaException>(
            () => _service.EditarConta(conta.Id, request));

        Assert.Equal(conta.Id, excecao.ContaId);
        Assert.Equal("Nome antigo", conta.Nome);
        Assert.Equal(SubtipoConta.Corrente, conta.Subtipo);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 10.2: campos fora da edicao nao mudam

    [Fact]
    public async Task EditarConta_NaoAlteraSaldoManualDiaFechamentoNemDiaVencimento()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, saldoManual: -250m, diaFechamento: 10, diaVencimento: 17);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Cartao Novo Nome", Icone = "card", Cor = "#111111" };

        // Act
        var resultado = await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Equal(-250m, conta.SaldoManual);
        Assert.Equal(10, conta.DiaFechamento);
        Assert.Equal(17, conta.DiaVencimento);
        Assert.Equal(-250m, resultado.SaldoManual);
        Assert.Equal(10, resultado.DiaFechamento);
        Assert.Equal(17, resultado.DiaVencimento);
    }

    #endregion

    #region Helpers

    private static Conta CriarConta(
        TipoConta tipo,
        bool ativa = true,
        SubtipoConta? subtipo = null,
        decimal? saldoManual = 1000m,
        int? diaFechamento = null,
        int? diaVencimento = null)
    {
        return new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Nome antigo",
            Origem = OrigemConta.Manual,
            Tipo = tipo,
            Subtipo = subtipo,
            Icone = "icone-antigo",
            Cor = "#000000",
            SaldoManual = saldoManual,
            DiaFechamento = diaFechamento,
            DiaVencimento = diaVencimento,
            Ativa = ativa
        };
    }

    private void RepositorioRetorna(Conta conta)
    {
        _mockRepository
            .Setup(r => r.ObterPorId(conta.Id))
            .ReturnsAsync(conta);
    }

    #endregion
}
