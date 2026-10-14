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

// Item 10.2: edicao de conta (nome, icone, cor, subtipo; dias so em Cartao). Tipo imutavel.
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

    #region Regra 10.2: conta inativa PODE ser editada (edicao nao reativa a conta)

    [Fact]
    public async Task EditarConta_ContaInativa_EditaNormalmenteSemReativar()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Banco, ativa: false, subtipo: SubtipoConta.Corrente);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Conta inativa renomeada", Subtipo = "Poupanca" };

        // Act
        var resultado = await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Equal("Conta inativa renomeada", conta.Nome);
        Assert.Equal(SubtipoConta.Poupanca, conta.Subtipo);
        Assert.False(conta.Ativa);
        Assert.False(resultado.Ativa);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 10.1/10.2: subtipo so vale para Tipo = Banco (outros tipos ficam null)

    [Fact]
    public async Task EditarConta_ContaCartaoComSubtipo_SubtipoIgnoradoFicaNull()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest
        {
            Nome = "Cartao Renomeado",
            Subtipo = "Poupanca",
            DiaFechamento = 5,
            DiaVencimento = 12
        };

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
        var conta = CriarConta(TipoConta.Cartao, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Novo nome", DiaFechamento = 5, DiaVencimento = 12 };

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

    #region Regra 10.2: conta inexistente (404)

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

    #endregion

    #region Regra 10.2: dias do cartao (dia_fechamento e dia_vencimento) obrigatorios e 1..31

    [Fact]
    public async Task EditarConta_ContaCartao_AtualizaDiaFechamentoEVencimento()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Cartao", DiaFechamento = 20, DiaVencimento = 27 };

        // Act
        var resultado = await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Equal(20, conta.DiaFechamento);
        Assert.Equal(27, conta.DiaVencimento);
        Assert.Equal(20, resultado.DiaFechamento);
        Assert.Equal(27, resultado.DiaVencimento);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Theory]
    [InlineData(null, 27, "DiaFechamento")]
    [InlineData(20, null, "DiaVencimento")]
    public async Task EditarConta_ContaCartaoSemUmDosDias_LancaCampoObrigatorioENaoAltera(
        int? diaFechamento, int? diaVencimento, string campoEsperado)
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Cartao", DiaFechamento = diaFechamento, DiaVencimento = diaVencimento };

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.EditarConta(conta.Id, request));

        Assert.Equal(campoEsperado, excecao.NomeCampo);
        Assert.Equal(5, conta.DiaFechamento);
        Assert.Equal(12, conta.DiaVencimento);
        Assert.Equal("Nome antigo", conta.Nome);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task EditarConta_ContaCartaoSemAmbosOsDias_LancaCampoObrigatorioDiaFechamento()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Cartao", DiaFechamento = null, DiaVencimento = null };

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<CampoObrigatorioException>(
            () => _service.EditarConta(conta.Id, request));

        Assert.Equal("DiaFechamento", excecao.NomeCampo);
        Assert.Equal(5, conta.DiaFechamento);
        Assert.Equal(12, conta.DiaVencimento);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public async Task EditarConta_ContaCartaoDiaFechamentoForaDe1A31_LancaValorInvalidoENaoAltera(int diaFechamento)
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Cartao", DiaFechamento = diaFechamento, DiaVencimento = 27 };

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.EditarConta(conta.Id, request));

        Assert.Equal("DiaFechamento", excecao.NomeCampo);
        Assert.Equal((decimal)diaFechamento, excecao.Valor);
        Assert.Equal(5, conta.DiaFechamento);
        Assert.Equal(12, conta.DiaVencimento);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public async Task EditarConta_ContaCartaoDiaVencimentoForaDe1A31_LancaValorInvalidoENaoAltera(int diaVencimento)
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Cartao", DiaFechamento = 20, DiaVencimento = diaVencimento };

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.EditarConta(conta.Id, request));

        Assert.Equal("DiaVencimento", excecao.NomeCampo);
        Assert.Equal((decimal)diaVencimento, excecao.Valor);
        Assert.Equal(5, conta.DiaFechamento);
        Assert.Equal(12, conta.DiaVencimento);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Theory]
    [InlineData(1, 31)]
    [InlineData(31, 1)]
    public async Task EditarConta_ContaCartaoNosLimitesDe1E31_EhAceito(int diaFechamento, int diaVencimento)
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Cartao", DiaFechamento = diaFechamento, DiaVencimento = diaVencimento };

        // Act
        await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Equal(diaFechamento, conta.DiaFechamento);
        Assert.Equal(diaVencimento, conta.DiaVencimento);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task EditarConta_ContaBancoComDias_NaoGravaDias()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Banco, subtipo: SubtipoConta.Corrente);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Banco", DiaFechamento = 10, DiaVencimento = 20 };

        // Act
        var resultado = await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Null(conta.DiaFechamento);
        Assert.Null(conta.DiaVencimento);
        Assert.Null(resultado.DiaFechamento);
        Assert.Null(resultado.DiaVencimento);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Theory]
    [InlineData(TipoConta.Banco)]
    [InlineData(TipoConta.Investimento)]
    public async Task EditarConta_BancoOuInvestimentoComDiaForaDaFaixa_IgnoraDiasSemValidar(TipoConta tipo)
    {
        // Arrange
        var conta = CriarConta(tipo);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Conta", DiaFechamento = 99, DiaVencimento = 99 };

        // Act
        await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Null(conta.DiaFechamento);
        Assert.Null(conta.DiaVencimento);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task EditarConta_ContaCartaoInativa_AtualizaDias()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, ativa: false, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Cartao inativo", DiaFechamento = 20, DiaVencimento = 27 };

        // Act
        await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Equal(20, conta.DiaFechamento);
        Assert.Equal(27, conta.DiaVencimento);
        Assert.False(conta.Ativa);
        _mockRepository.Verify(r => r.Salvar(), Times.Once);
    }

    [Fact]
    public async Task EditarConta_DiaInvalidoComNomeValido_NaoAlteraNomeNemDias()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest { Nome = "Nome novo", DiaFechamento = 40, DiaVencimento = 27 };

        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(
            () => _service.EditarConta(conta.Id, request));

        Assert.Equal("Nome antigo", conta.Nome);
        Assert.Equal(5, conta.DiaFechamento);
        Assert.Equal(12, conta.DiaVencimento);
        _mockRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 10.2: campos fora da edicao nao mudam

    [Fact]
    public async Task EditarConta_ContaCartao_NaoAlteraSaldoManual()
    {
        // Arrange
        var conta = CriarConta(TipoConta.Cartao, saldoManual: -250m, diaFechamento: 5, diaVencimento: 12);
        RepositorioRetorna(conta);

        var request = new EditarContaRequest
        {
            Nome = "Cartao Novo Nome",
            Icone = "card",
            Cor = "#111111",
            DiaFechamento = 20,
            DiaVencimento = 27
        };

        // Act
        var resultado = await _service.EditarConta(conta.Id, request);

        // Assert
        Assert.Equal(-250m, conta.SaldoManual);
        Assert.Equal(-250m, resultado.SaldoManual);
        Assert.Equal(20, resultado.DiaFechamento);
        Assert.Equal(27, resultado.DiaVencimento);
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
