using Microsoft.EntityFrameworkCore;
using Moq;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

// Regra-de-negocio.md itens 9, 13 e 15 — EditarAsync de ContaReceber.
public class ContaReceberEditarServiceTests
{
    private readonly Mock<IContaReceberRepository> _mockContaReceberRepository;
    private readonly Mock<ITransferenciaRepository> _mockTransferenciaRepository;
    private readonly Mock<ILancamentoRepository> _mockLancamentoRepository;
    private readonly Mock<IContaRepository> _mockContaRepository;
    private readonly ContaReceberService _service;

    public ContaReceberEditarServiceTests()
    {
        _mockContaReceberRepository = new Mock<IContaReceberRepository>();
        _mockTransferenciaRepository = new Mock<ITransferenciaRepository>();
        _mockLancamentoRepository = new Mock<ILancamentoRepository>();
        _mockContaRepository = new Mock<IContaRepository>();
        _service = new ContaReceberService(
            _mockContaReceberRepository.Object,
            _mockTransferenciaRepository.Object,
            _mockLancamentoRepository.Object,
            _mockContaRepository.Object);
    }

    private static ContaReceber CriarContaReceber(
        TipoContaReceber tipo,
        StatusContaReceber status,
        decimal valorTotal = 1000m,
        string? pessoa = null,
        DateOnly? dataPrevista = null,
        Guid? recebivelRecorrenteId = null)
    {
        var contaReceber = new ContaReceber
        {
            Id = Guid.NewGuid(),
            Tipo = tipo,
            Descricao = "Descricao original",
            Pessoa = pessoa,
            ValorTotal = valorTotal,
            DataRegistro = new DateOnly(2026, 1, 1),
            DataPrevista = dataPrevista,
            Status = status,
            RecebivelRecorrenteId = recebivelRecorrenteId
        };

        if (status == StatusContaReceber.Parcial)
        {
            contaReceber.Recebimentos.Add(CriarCredito(contaReceber.Id, 400m));
        }
        else if (status == StatusContaReceber.Recebido)
        {
            contaReceber.Recebimentos.Add(CriarCredito(contaReceber.Id, valorTotal));
        }

        return contaReceber;
    }

    private static Lancamento CriarCredito(Guid contaReceberId, decimal valor)
    {
        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            ContaReceberId = contaReceberId,
            Tipo = TipoLancamento.Credit,
            Status = StatusLancamento.Pago,
            Valor = valor,
            Data = new DateOnly(2026, 2, 1),
            Manual = true
        };
    }

    private void ConfigurarContaExistente(ContaReceber contaReceber)
    {
        _mockContaReceberRepository
            .Setup(r => r.ObterPorId(contaReceber.Id))
            .ReturnsAsync(contaReceber);
    }

    #region Item 9/13: conta inexistente

    [Fact]
    public async Task EditarAsync_ContaInexistente_LancaContaReceberNaoEncontradaException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockContaReceberRepository
            .Setup(r => r.ObterPorId(id))
            .ReturnsAsync((ContaReceber?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ContaReceberNaoEncontradaException>(() =>
            _service.EditarAsync(id, "Nova descricao", null, null, null));
    }

    #endregion

    #region Item 15: ocorrencia de recebivel recorrente nao e editada individualmente

    [Fact]
    public async Task EditarAsync_ContaComRecebivelRecorrenteId_LancaContaReceberRecorrenteNaoEditavelException()
    {
        // Arrange
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Recebivel,
            StatusContaReceber.Pendente,
            recebivelRecorrenteId: Guid.NewGuid());
        ConfigurarContaExistente(contaReceber);

        // Act & Assert
        await Assert.ThrowsAsync<ContaReceberRecorrenteNaoEditavelException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, null));
    }

    [Fact]
    public async Task EditarAsync_ContaRecorrenteComValorDiferenteEmParcial_LancaRecorrenteAntesDeValorTotal()
    {
        // Arrange - precedencia: a checagem de recorrente vem antes da de valor_total
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Recebivel,
            StatusContaReceber.Parcial,
            recebivelRecorrenteId: Guid.NewGuid());
        ConfigurarContaExistente(contaReceber);

        // Act & Assert
        await Assert.ThrowsAsync<ContaReceberRecorrenteNaoEditavelException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, 2000m));
    }

    [Fact]
    public async Task EditarAsync_ContaRecorrente_NaoSalvaNada()
    {
        // Arrange
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Recebivel,
            StatusContaReceber.Pendente,
            recebivelRecorrenteId: Guid.NewGuid());
        ConfigurarContaExistente(contaReceber);

        // Act
        await Assert.ThrowsAsync<ContaReceberRecorrenteNaoEditavelException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, null));

        // Assert
        _mockContaReceberRepository.Verify(r => r.Atualizar(It.IsAny<ContaReceber>()), Times.Never);
        _mockContaReceberRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Item 13: pessoa obrigatoria em EMPRESTIMO e ignorada em RECEBIVEL

    [Fact]
    public async Task EditarAsync_EmprestimoComPessoaNula_LancaPessoaObrigatoriaParaEmprestimoException()
    {
        // Arrange
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Emprestimo,
            StatusContaReceber.Pendente,
            pessoa: "Jose da Silva");
        ConfigurarContaExistente(contaReceber);

        // Act & Assert
        await Assert.ThrowsAsync<PessoaObrigatoriaParaEmprestimoException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, null));
    }

    [Fact]
    public async Task EditarAsync_EmprestimoComPessoaSoEspacos_LancaPessoaObrigatoriaParaEmprestimoException()
    {
        // Arrange
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Emprestimo,
            StatusContaReceber.Pendente,
            pessoa: "Jose da Silva");
        ConfigurarContaExistente(contaReceber);

        // Act & Assert
        await Assert.ThrowsAsync<PessoaObrigatoriaParaEmprestimoException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", "   ", null, null));
    }

    [Fact]
    public async Task EditarAsync_EmprestimoComPessoaValida_GravaNovaPessoa()
    {
        // Arrange
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Emprestimo,
            StatusContaReceber.Pendente,
            pessoa: "Jose da Silva");
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Nova descricao", "Maria Souza", null, null);

        // Assert
        Assert.Equal("Maria Souza", resultado.Pessoa);
    }

    [Fact]
    public async Task EditarAsync_RecebivelComPessoaInformada_IgnoraPessoaEFicaNula()
    {
        // Arrange
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Pendente);
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Nova descricao", "Fulano", null, null);

        // Assert
        Assert.Null(resultado.Pessoa);
    }

    #endregion

    #region Item 13: valor_total so muda em RECEBIVEL Pendente

    [Fact]
    public async Task EditarAsync_EmprestimoPendenteComValorDiferente_LancaValorTotalNaoEditavelException()
    {
        // Arrange
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Emprestimo,
            StatusContaReceber.Pendente,
            pessoa: "Jose da Silva");
        ConfigurarContaExistente(contaReceber);

        // Act & Assert
        await Assert.ThrowsAsync<ValorTotalNaoEditavelException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", "Jose da Silva", null, 2000m));
    }

    [Fact]
    public async Task EditarAsync_EmprestimoParcialComValorDiferente_LancaValorTotalNaoEditavelException()
    {
        // Arrange
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Emprestimo,
            StatusContaReceber.Parcial,
            pessoa: "Jose da Silva");
        ConfigurarContaExistente(contaReceber);

        // Act & Assert
        await Assert.ThrowsAsync<ValorTotalNaoEditavelException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", "Jose da Silva", null, 2000m));
    }

    [Fact]
    public async Task EditarAsync_RecebivelParcialComValorDiferente_LancaValorTotalNaoEditavelException()
    {
        // Arrange
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Parcial);
        ConfigurarContaExistente(contaReceber);

        // Act & Assert
        await Assert.ThrowsAsync<ValorTotalNaoEditavelException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, 2000m));
    }

    [Fact]
    public async Task EditarAsync_RecebivelRecebidoComValorDiferente_LancaValorTotalNaoEditavelException()
    {
        // Arrange
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Recebido);
        ConfigurarContaExistente(contaReceber);

        // Act & Assert
        await Assert.ThrowsAsync<ValorTotalNaoEditavelException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, 2000m));
    }

    [Fact]
    public async Task EditarAsync_RecebivelPendenteComValorDiferente_GravaNovoValorTotal()
    {
        // Arrange
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Pendente, valorTotal: 1000m);
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, 1500m);

        // Assert
        Assert.Equal(1500m, resultado.ValorTotal);
    }

    [Fact]
    public async Task EditarAsync_RecebivelParcialComMesmoValor_NaoLancaExcecaoENaoAlteraValor()
    {
        // Arrange - igual ao atual e no-op, mesmo fora de Pendente
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Parcial, valorTotal: 1000m);
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, 1000m);

        // Assert
        Assert.Equal(1000m, resultado.ValorTotal);
    }

    [Fact]
    public async Task EditarAsync_EmprestimoParcialComMesmoValor_NaoLancaExcecaoENaoAlteraValor()
    {
        // Arrange - igual ao atual e no-op, mesmo em EMPRESTIMO
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Emprestimo,
            StatusContaReceber.Parcial,
            valorTotal: 1000m,
            pessoa: "Jose da Silva");
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Nova descricao", "Jose da Silva", null, 1000m);

        // Assert
        Assert.Equal(1000m, resultado.ValorTotal);
    }

    [Fact]
    public async Task EditarAsync_ValorTotalNulo_NaoAlteraValorTotal()
    {
        // Arrange - null significa nao alterar, mesmo em conta fora de Pendente
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Parcial, valorTotal: 1000m);
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, null);

        // Assert
        Assert.Equal(1000m, resultado.ValorTotal);
    }

    [Fact]
    public async Task EditarAsync_ErroDeValorTotal_NaoAtualizaNemSalva()
    {
        // Arrange
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Parcial);
        ConfigurarContaExistente(contaReceber);

        // Act
        await Assert.ThrowsAsync<ValorTotalNaoEditavelException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, 2000m));

        // Assert
        _mockContaReceberRepository.Verify(r => r.Atualizar(It.IsAny<ContaReceber>()), Times.Never);
        _mockContaReceberRepository.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_ErroDePessoa_NaoAtualizaNemSalva()
    {
        // Arrange
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Emprestimo,
            StatusContaReceber.Pendente,
            pessoa: "Jose da Silva");
        ConfigurarContaExistente(contaReceber);

        // Act
        await Assert.ThrowsAsync<PessoaObrigatoriaParaEmprestimoException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", "", null, null));

        // Assert
        _mockContaReceberRepository.Verify(r => r.Atualizar(It.IsAny<ContaReceber>()), Times.Never);
        _mockContaReceberRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Item 13: data_prevista e descricao editaveis em qualquer status; tipo e status imutaveis

    [Fact]
    public async Task EditarAsync_DataPrevistaNula_LimpaDataPrevista()
    {
        // Arrange
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Recebivel,
            StatusContaReceber.Pendente,
            dataPrevista: new DateOnly(2026, 3, 15));
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, null);

        // Assert
        Assert.Null(resultado.DataPrevista);
    }

    [Fact]
    public async Task EditarAsync_DataPrevistaNovaEmParcial_GravaNovaDataPrevista()
    {
        // Arrange
        var contaReceber = CriarContaReceber(
            TipoContaReceber.Recebivel,
            StatusContaReceber.Parcial,
            dataPrevista: new DateOnly(2026, 3, 15));
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Nova descricao", null, new DateOnly(2026, 4, 10), null);

        // Assert
        Assert.Equal(new DateOnly(2026, 4, 10), resultado.DataPrevista);
    }

    [Fact]
    public async Task EditarAsync_DescricaoNova_GravaDescricao()
    {
        // Arrange
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Parcial);
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Consulta de maio", null, null, null);

        // Assert
        Assert.Equal("Consulta de maio", resultado.Descricao);
    }

    [Fact]
    public async Task EditarAsync_QualquerEdicao_NaoAlteraTipoNemStatus()
    {
        // Arrange
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Parcial);
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Nova descricao", null, new DateOnly(2026, 5, 1), null);

        // Assert
        Assert.Equal(TipoContaReceber.Recebivel, resultado.Tipo);
        Assert.Equal(StatusContaReceber.Parcial, resultado.Status);
    }

    #endregion

    #region Item 13: saldo pendente reflete o novo valor apos editar em Pendente

    [Fact]
    public async Task EditarAsync_RecebivelPendenteComValorDiferente_SaldoPendenteUsaNovoValor()
    {
        // Arrange
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Pendente, valorTotal: 1000m);
        ConfigurarContaExistente(contaReceber);

        // Act
        var resultado = await _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, 1500m);

        // Assert
        var saldo = ContaReceberSaldoCalculator.Calcular(resultado);
        Assert.Equal(1500m, saldo.SaldoPendente);
    }

    #endregion

    #region Suposicao (contrato omisso): valor e descricao invalidos sao rejeitados

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task EditarAsync_RecebivelPendenteComValorNaoPositivo_LancaValorInvalidoException(decimal valorInvalido)
    {
        // SUPOSICAO: contrato omisso sobre valor <= 0 na edicao. Assume-se rejeicao com a excecao de validacao existente.
        // Arrange
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Pendente, valorTotal: 1000m);
        ConfigurarContaExistente(contaReceber);

        // Act & Assert
        await Assert.ThrowsAsync<ValorInvalidoException>(() =>
            _service.EditarAsync(contaReceber.Id, "Nova descricao", null, null, valorInvalido));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EditarAsync_DescricaoVazia_LancaCampoObrigatorioException(string descricaoVazia)
    {
        // SUPOSICAO: contrato omisso sobre descricao vazia na edicao. Assume-se rejeicao com a excecao de validacao existente.
        // Arrange
        var contaReceber = CriarContaReceber(TipoContaReceber.Recebivel, StatusContaReceber.Pendente);
        ConfigurarContaExistente(contaReceber);

        // Act & Assert
        await Assert.ThrowsAsync<CampoObrigatorioException>(() =>
            _service.EditarAsync(contaReceber.Id, descricaoVazia, null, null, null));
    }

    #endregion
}

// Item 9: mudar data_prevista de marco para abril move o saldo na projecao do mes (teste de integracao com EF InMemory).
public class ContaReceberEditarProjecaoIntegrationTests : IDisposable
{
    private readonly MyFinancesDbContext _context;
    private readonly ContaReceberService _service;

    public ContaReceberEditarProjecaoIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new MyFinancesDbContext(options);

        _service = new ContaReceberService(
            new ContaReceberRepository(_context),
            new Mock<ITransferenciaRepository>().Object,
            new Mock<ILancamentoRepository>().Object,
            new Mock<IContaRepository>().Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task EditarAsync_DataPrevistaDeMarcoParaAbril_MoveSaldoDaProjecaoDeMarcoParaAbril()
    {
        // Arrange
        var contaReceber = new ContaReceber
        {
            Id = Guid.NewGuid(),
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Recebivel projetado",
            ValorTotal = 1000m,
            DataRegistro = new DateOnly(2026, 1, 1),
            DataPrevista = new DateOnly(2026, 3, 15),
            Status = StatusContaReceber.Pendente
        };
        _context.ContasReceber.Add(contaReceber);
        await _context.SaveChangesAsync();

        var totalMarcoAntes = await _service.CalcularTotalAReceberEsperadoNoMes(2026, 3);
        Assert.Equal(1000m, totalMarcoAntes);

        // Act
        await _service.EditarAsync(contaReceber.Id, "Recebivel projetado", null, new DateOnly(2026, 4, 10), null);

        // Assert
        var totalMarcoDepois = await _service.CalcularTotalAReceberEsperadoNoMes(2026, 3);
        var totalAbrilDepois = await _service.CalcularTotalAReceberEsperadoNoMes(2026, 4);
        Assert.Equal(0m, totalMarcoDepois);
        Assert.Equal(1000m, totalAbrilDepois);
    }
}
