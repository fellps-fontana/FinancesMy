using Moq;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class ContaReceberServiceTests
{
    private readonly Mock<IContaReceberRepository> _mockContaReceberRepository;
    private readonly Mock<ITransferenciaRepository> _mockTransferenciaRepository;
    private readonly Mock<ILancamentoRepository> _mockLancamentoRepository;
    private readonly Mock<IContaRepository> _mockContaRepository;
    private readonly ContaReceberService _service;

    public ContaReceberServiceTests()
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

    #region Regra 1: RegistrarRecebivel cria ContaReceber RECEBIVEL com Status PENDENTE e NAO cria Transferencia nem Lancamento

    [Fact]
    public async Task RegistrarRecebivel_ParametrosValidos_CriaContaReceberSemTransferenciaOuLancamento()
    {
        // Arrange
        var descricao = "Consulta";
        var valorTotal = 1000m;
        var dataRegistro = new DateOnly(2026, 7, 1);
        var dataPrevista = new DateOnly(2026, 7, 31);
        var categoriaId = Guid.NewGuid();

        // Act
        var resultado = await _service.RegistrarRecebivel(
            descricao,
            valorTotal,
            dataRegistro,
            dataPrevista,
            categoriaId);

        // Assert
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(TipoContaReceber.Recebivel, resultado.Tipo);
        Assert.Equal(descricao, resultado.Descricao);
        Assert.Equal(valorTotal, resultado.ValorTotal);
        Assert.Equal(dataRegistro, resultado.DataRegistro);
        Assert.Equal(dataPrevista, resultado.DataPrevista);
        Assert.Equal(categoriaId, resultado.CategoriaId);
        Assert.Equal(StatusContaReceber.Pendente, resultado.Status);

        // Verifica que Transferencia e Lancamento NAO foram criados
        _mockTransferenciaRepository.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _mockLancamentoRepository.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);

        // Verifica que ContaReceber foi persistido
        _mockContaReceberRepository.Verify(r => r.Adicionar(It.IsAny<ContaReceber>()), Times.Once);
        _mockContaReceberRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 2: RegistrarEmprestimo cria ContaReceber EMPRESTIMO, UMA Transferencia (perna unica, ContaDestinoId=null), e UM Lancamento DEBIT

    [Fact]
    public async Task RegistrarEmprestimo_ParametrosValidos_CriaContaReceberTransferenciaEUmLancamento()
    {
        // Arrange
        var descricao = "Emprestimo para Jose";
        var pessoa = "Jose da Silva";
        var valorTotal = 5000m;
        var contaOrigemId = Guid.NewGuid();
        var dataRegistro = new DateOnly(2026, 7, 1);
        var dataPrevista = new DateOnly(2026, 8, 1);
        var categoriaId = Guid.NewGuid();

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Corrente",
            Tipo = TipoConta.Banco,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);

        // Act
        var resultado = await _service.RegistrarEmprestimo(
            descricao,
            pessoa,
            valorTotal,
            contaOrigemId,
            dataRegistro,
            dataPrevista,
            categoriaId);

        // Assert
        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(TipoContaReceber.Emprestimo, resultado.Tipo);
        Assert.Equal(pessoa, resultado.Pessoa);
        Assert.Equal(descricao, resultado.Descricao);
        Assert.Equal(valorTotal, resultado.ValorTotal);
        Assert.Equal(StatusContaReceber.Pendente, resultado.Status);

        // Verifica que Transferencia foi criada com perna unica (ContaDestinoId=null)
        _mockTransferenciaRepository.Verify(
            r => r.Adicionar(It.Is<Transferencia>(t =>
                t.ContaOrigemId == contaOrigemId &&
                t.ContaDestinoId == null &&
                t.ContaReceberId == resultado.Id &&
                t.Valor == valorTotal)),
            Times.Once);

        // Verifica que EXATAMENTE UM Lancamento foi criado (Debit, Pago)
        _mockLancamentoRepository.Verify(
            r => r.Adicionar(It.Is<Lancamento>(l =>
                l.Tipo == TipoLancamento.Debit &&
                l.Status == StatusLancamento.Pago &&
                l.ContaReceberId == resultado.Id &&
                l.Valor == valorTotal)),
            Times.Once);

        // Verifica que salvou
        _mockContaReceberRepository.Verify(r => r.Salvar(), Times.Once);
        _mockTransferenciaRepository.Verify(r => r.Salvar(), Times.Once);
        _mockLancamentoRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 3: RegistrarEmprestimo com pessoa nula ou vazia lanca PessoaObrigatoriaParaEmprestimoException e NAO persiste nada

    [Fact]
    public async Task RegistrarEmprestimo_PessoaNula_LancaPessoaObrigatoriaExcecao()
    {
        // Arrange
        var descricao = "Emprestimo";
        var valorTotal = 5000m;
        var contaOrigemId = Guid.NewGuid();
        var dataRegistro = new DateOnly(2026, 7, 1);

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Corrente",
            Tipo = TipoConta.Banco,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);

        // Act & Assert
        await Assert.ThrowsAsync<PessoaObrigatoriaParaEmprestimoException>(
            () => _service.RegistrarEmprestimo(
                descricao,
                null!,
                valorTotal,
                contaOrigemId,
                dataRegistro,
                null,
                null));

        // Verifica que nada foi persistido
        _mockContaReceberRepository.Verify(r => r.Adicionar(It.IsAny<ContaReceber>()), Times.Never);
        _mockTransferenciaRepository.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _mockLancamentoRepository.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarEmprestimo_PessoaVazia_LancaPessoaObrigatoriaExcecao()
    {
        // Arrange
        var descricao = "Emprestimo";
        var valorTotal = 5000m;
        var contaOrigemId = Guid.NewGuid();
        var dataRegistro = new DateOnly(2026, 7, 1);

        var contaOrigem = new Conta
        {
            Id = contaOrigemId,
            Nome = "Conta Corrente",
            Tipo = TipoConta.Banco,
            Ativa = true
        };

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync(contaOrigem);

        // Act & Assert
        await Assert.ThrowsAsync<PessoaObrigatoriaParaEmprestimoException>(
            () => _service.RegistrarEmprestimo(
                descricao,
                string.Empty,
                valorTotal,
                contaOrigemId,
                dataRegistro,
                null,
                null));

        // Verifica que nada foi persistido
        _mockContaReceberRepository.Verify(r => r.Adicionar(It.IsAny<ContaReceber>()), Times.Never);
        _mockTransferenciaRepository.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _mockLancamentoRepository.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    #endregion

    #region Regra 4: RegistrarRecebimento (caso feliz) cria Lancamento CREDIT PAGO vinculado ao ContaReceber

    [Fact]
    public async Task RegistrarRecebimento_ValorMenorQueSaldoPendente_CriaLancamentoCreditPago()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var valor = 300m;
        var data = new DateOnly(2026, 7, 15);

        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Consulta",
            ValorTotal = 1000m,
            Status = StatusContaReceber.Pendente
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Corrente",
            Ativa = true
        };

        _mockContaReceberRepository
            .Setup(r => r.ObterPorId(contaReceberId))
            .ReturnsAsync(contaReceber);

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        // Act
        var lancamento = await _service.RegistrarRecebimento(
            contaReceberId,
            contaDestinoId,
            valor,
            data,
            null);

        // Assert
        Assert.NotEqual(Guid.Empty, lancamento.Id);
        Assert.Equal(TipoLancamento.Credit, lancamento.Tipo);
        Assert.Equal(StatusLancamento.Pago, lancamento.Status);
        Assert.Equal(valor, lancamento.Valor);
        Assert.Equal(data, lancamento.Data);
        Assert.Equal(contaReceberId, lancamento.ContaReceberId);

        // Verifica que foi persistido
        _mockLancamentoRepository.Verify(
            r => r.Adicionar(It.Is<Lancamento>(l =>
                l.Tipo == TipoLancamento.Credit &&
                l.Status == StatusLancamento.Pago &&
                l.ContaReceberId == contaReceberId)),
            Times.Once);
    }

    #endregion

    #region Regra 4.1: RegistrarRecebimento com valor que ZERA o saldo — Status deve transicionar para RECEBIDO e persistir

    [Fact]
    public async Task RegistrarRecebimento_ValorQueZeraSaldo_AtualizaStatusParaRecebidoEPersiste()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var valorTotal = 1000m;
        var valor = 1000m; // Zera o saldo pendente
        var data = new DateOnly(2026, 7, 15);

        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Consulta",
            ValorTotal = valorTotal,
            Status = StatusContaReceber.Pendente
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Corrente",
            Ativa = true
        };

        _mockContaReceberRepository
            .Setup(r => r.ObterPorId(contaReceberId))
            .ReturnsAsync(contaReceber);

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        // Act
        var lancamento = await _service.RegistrarRecebimento(
            contaReceberId,
            contaDestinoId,
            valor,
            data,
            null);

        // Assert - Lancamento criado com sucesso
        Assert.NotEqual(Guid.Empty, lancamento.Id);
        Assert.Equal(TipoLancamento.Credit, lancamento.Tipo);
        Assert.Equal(StatusLancamento.Pago, lancamento.Status);
        Assert.Equal(valor, lancamento.Valor);

        // Verifica que ContaReceber.Status foi atualizado para RECEBIDO e persistido
        _mockContaReceberRepository.Verify(
            r => r.Atualizar(It.Is<ContaReceber>(cr =>
                cr.Id == contaReceberId &&
                cr.Status == StatusContaReceber.Recebido)),
            Times.Once);

        _mockContaReceberRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 4.2: RegistrarRecebimento com valor MENOR que saldo — Status deve transicionar para PARCIAL

    [Fact]
    public async Task RegistrarRecebimento_ValorMenorQueSaldo_AtualizaStatusParaParcialEPersiste()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var valorTotal = 1000m;
        var valor = 300m; // Menor que valor total, deixa saldo pendente
        var data = new DateOnly(2026, 7, 15);

        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Consulta",
            ValorTotal = valorTotal,
            Status = StatusContaReceber.Pendente
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Corrente",
            Ativa = true
        };

        _mockContaReceberRepository
            .Setup(r => r.ObterPorId(contaReceberId))
            .ReturnsAsync(contaReceber);

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        // Act
        var lancamento = await _service.RegistrarRecebimento(
            contaReceberId,
            contaDestinoId,
            valor,
            data,
            null);

        // Assert - Lancamento criado com sucesso
        Assert.NotEqual(Guid.Empty, lancamento.Id);
        Assert.Equal(TipoLancamento.Credit, lancamento.Tipo);

        // Verifica que ContaReceber.Status foi atualizado para PARCIAL
        _mockContaReceberRepository.Verify(
            r => r.Atualizar(It.Is<ContaReceber>(cr =>
                cr.Id == contaReceberId &&
                cr.Status == StatusContaReceber.Parcial)),
            Times.Once);

        _mockContaReceberRepository.Verify(r => r.Salvar(), Times.Once);
    }

    #endregion

    #region Regra 9: RegistrarEmprestimo com contaOrigemId inexistente lanca ContaNaoEncontradaException e NAO persiste

    [Fact]
    public async Task RegistrarEmprestimo_ContaOrigemNaoExiste_LancaContaNaoEncontradaExcecaoENaoPersiste()
    {
        // Arrange
        var descricao = "Emprestimo para Jose";
        var pessoa = "Jose da Silva";
        var valorTotal = 5000m;
        var contaOrigemId = Guid.NewGuid();
        var dataRegistro = new DateOnly(2026, 7, 1);

        // Mock retorna null para conta inexistente
        _mockContaRepository
            .Setup(r => r.ObterPorId(contaOrigemId))
            .ReturnsAsync((Conta?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ContaNaoEncontradaException>(
            () => _service.RegistrarEmprestimo(
                descricao,
                pessoa,
                valorTotal,
                contaOrigemId,
                dataRegistro,
                null,
                null));

        Assert.Equal(contaOrigemId, excecao.ContaId);

        // Verifica que nada foi persistido (validacao ocorreu ANTES da persistencia)
        _mockContaReceberRepository.Verify(r => r.Adicionar(It.IsAny<ContaReceber>()), Times.Never);
        _mockTransferenciaRepository.Verify(r => r.Adicionar(It.IsAny<Transferencia>()), Times.Never);
        _mockLancamentoRepository.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    #endregion

    #region Regra 10: RegistrarRecebimento com contaDestinoId inexistente lanca ContaNaoEncontradaException

    [Fact]
    public async Task RegistrarRecebimento_ContaDestinoNaoExiste_LancaContaNaoEncontradaExcecaoENaoPersiste()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var valor = 300m;
        var data = new DateOnly(2026, 7, 15);

        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Consulta",
            ValorTotal = 1000m,
            Status = StatusContaReceber.Pendente
        };

        _mockContaReceberRepository
            .Setup(r => r.ObterPorId(contaReceberId))
            .ReturnsAsync(contaReceber);

        // Mock retorna null para conta destino inexistente
        _mockContaRepository
            .Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync((Conta?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ContaNaoEncontradaException>(
            () => _service.RegistrarRecebimento(
                contaReceberId,
                contaDestinoId,
                valor,
                data,
                null));

        Assert.Equal(contaDestinoId, excecao.ContaId);

        // Verifica que nenhum lancamento foi criado
        _mockLancamentoRepository.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
        _mockLancamentoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 5: RegistrarRecebimento com categoriaId usa a categoria informada, sobrescrevendo a do ContaReceber

    [Fact]
    public async Task RegistrarRecebimento_ComCategoriaId_UsaCategoriaInformada()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var valor = 300m;
        var data = new DateOnly(2026, 7, 15);
        var categoriaPropria = Guid.NewGuid();
        var categoriaContaReceber = Guid.NewGuid();

        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Consulta",
            ValorTotal = 1000m,
            CategoriaId = categoriaContaReceber,
            Status = StatusContaReceber.Pendente
        };

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Corrente",
            Ativa = true
        };

        _mockContaReceberRepository
            .Setup(r => r.ObterPorId(contaReceberId))
            .ReturnsAsync(contaReceber);

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        // Act
        var lancamento = await _service.RegistrarRecebimento(
            contaReceberId,
            contaDestinoId,
            valor,
            data,
            categoriaPropria);

        // Assert - a categoria do lancamento deve ser a informada, nao a do ContaReceber
        Assert.Equal(categoriaPropria, lancamento.CategoriaId);
        Assert.NotEqual(categoriaContaReceber, lancamento.CategoriaId);

        _mockLancamentoRepository.Verify(
            r => r.Adicionar(It.Is<Lancamento>(l =>
                l.CategoriaId == categoriaPropria)),
            Times.Once);
    }

    #endregion

    #region Regra 6: RegistrarRecebimento com valor > saldo_pendente lanca ValorRecebimentoExcedeSaldoPendenteException e NAO cria Lancamento

    [Fact]
    public async Task RegistrarRecebimento_ValorMaiorQueSaldoPendente_LancaExcecao()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var saldoPendente = 500m;
        var valorRecebimento = 600m;
        var data = new DateOnly(2026, 7, 15);

        var contaReceber = new ContaReceber
        {
            Id = contaReceberId,
            Tipo = TipoContaReceber.Recebivel,
            Descricao = "Consulta",
            ValorTotal = 1000m,
            Status = StatusContaReceber.Pendente
        };

        // Simula 500 reais ja recebidos, deixando 500m pendentes
        var lancamentoExistente = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Tipo = TipoLancamento.Credit,
            Status = StatusLancamento.Pago,
            Valor = 500m,
            ContaReceberId = contaReceberId
        };

        contaReceber.Recebimentos.Add(lancamentoExistente);

        var contaDestino = new Conta
        {
            Id = contaDestinoId,
            Nome = "Conta Corrente",
            Ativa = true
        };

        _mockContaReceberRepository
            .Setup(r => r.ObterPorId(contaReceberId))
            .ReturnsAsync(contaReceber);

        _mockContaRepository
            .Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(contaDestino);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ValorRecebimentoExcedeSaldoPendenteException>(
            () => _service.RegistrarRecebimento(
                contaReceberId,
                contaDestinoId,
                valorRecebimento,
                data,
                null));

        Assert.Equal(valorRecebimento, excecao.ValorRecebimento);
        Assert.Equal(saldoPendente, excecao.SaldoPendente);

        // Verifica que nenhum lancamento foi criado
        _mockLancamentoRepository.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
        _mockLancamentoRepository.Verify(r => r.Salvar(), Times.Never);
    }

    #endregion

    #region Regra 7: ObterPorId com id inexistente lanca ContaReceberNaoEncontradaException

    [Fact]
    public async Task ObterPorId_IdInexistente_LancaContaReceberNaoEncontradaException()
    {
        // Arrange
        var contaReceberId = Guid.NewGuid();

        _mockContaReceberRepository
            .Setup(r => r.ObterPorId(contaReceberId))
            .ReturnsAsync((ContaReceber?)null);

        // Act & Assert
        var excecao = await Assert.ThrowsAsync<ContaReceberNaoEncontradaException>(
            () => _service.ObterPorId(contaReceberId));

        Assert.Equal(contaReceberId, excecao.ContaReceberId);
    }

    #endregion

    #region Regra 8: Listar sem filtro retorna todas; Listar com filtro filtra por status

    [Fact]
    public async Task Listar_SemFiltro_RetornaTodos()
    {
        // Arrange
        var contasReceber = new List<ContaReceber>
        {
            new ContaReceber { Id = Guid.NewGuid(), Tipo = TipoContaReceber.Recebivel, ValorTotal = 1000m },
            new ContaReceber { Id = Guid.NewGuid(), Tipo = TipoContaReceber.Emprestimo, ValorTotal = 5000m }
        };

        _mockContaReceberRepository
            .Setup(r => r.Listar(null))
            .ReturnsAsync(contasReceber);

        // Act
        var resultado = await _service.Listar(null);

        // Assert
        Assert.Equal(2, resultado.Count());
        _mockContaReceberRepository.Verify(r => r.Listar(null), Times.Once);
    }

    [Fact]
    public async Task Listar_ComFiltroStatus_ChamaRepositoryComFiltro()
    {
        // Arrange
        var statusFiltro = StatusContaReceber.Pendente;
        var contasReceber = new List<ContaReceber>
        {
            new ContaReceber { Id = Guid.NewGuid(), Tipo = TipoContaReceber.Recebivel, ValorTotal = 1000m, Status = StatusContaReceber.Pendente }
        };

        _mockContaReceberRepository
            .Setup(r => r.Listar(statusFiltro))
            .ReturnsAsync(contasReceber);

        // Act
        var resultado = await _service.Listar(statusFiltro);

        // Assert
        Assert.Single(resultado);
        _mockContaReceberRepository.Verify(r => r.Listar(statusFiltro), Times.Once);
    }

    #endregion
}
