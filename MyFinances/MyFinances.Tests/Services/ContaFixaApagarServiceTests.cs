using Moq;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

// regra-de-negocio.md item 6 (Apagar conta fixa, Desativar corrigido) e motor
// RecorrenciaGeradorService (RemoverOcorrenciasNaoConsumadasAsync /
// DesvincularOcorrenciasConsumadasAsync). ContaFixaService usa o motor REAL
// com repositorios mockados, para que a remocao/desvinculacao das ocorrencias
// seja observada no nivel de Lancamento.
public class ContaFixaApagarServiceTests
{
    private readonly Mock<IContaFixaRepository> _contaFixaRepository;
    private readonly Mock<IContaRepository> _contaRepository;
    private readonly Mock<ILancamentoRepository> _lancamentoRepository;
    private readonly RecorrenciaGeradorService _motor;
    private readonly ContaFixaService _service;

    public ContaFixaApagarServiceTests()
    {
        _contaFixaRepository = new Mock<IContaFixaRepository>();
        _contaRepository = new Mock<IContaRepository>();
        _lancamentoRepository = new Mock<ILancamentoRepository>();

        var compraCartaoService = new Mock<CompraCartaoService>(
            new Mock<ILancamentoRepository>().Object,
            new Mock<FaturaCicloService>(
                new Mock<IFaturaRepository>().Object,
                new Mock<IContaRepository>().Object).Object,
            new Mock<ValidacaoCartaoService>(
                new Mock<IContaRepository>().Object).Object);

        _motor = new RecorrenciaGeradorService(
            _contaFixaRepository.Object,
            _lancamentoRepository.Object,
            compraCartaoService.Object);

        _service = new ContaFixaService(
            _contaFixaRepository.Object,
            _contaRepository.Object,
            _lancamentoRepository.Object,
            _motor);
    }

    #region Apagar - destino banco

    [Fact]
    public async Task ApagarAsync_Banco_RemoveLancamentoPendente()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var pendente = CriarLancamentoBanco(moldeId, StatusLancamento.Pendente, new DateOnly(2026, 11, 15));
        var pago = CriarLancamentoBanco(moldeId, StatusLancamento.Pago, new DateOnly(2026, 9, 15));
        var molde = CriarMolde(moldeId, ativa: true, pendente, pago);
        DeixarMoldeExistente(molde);

        // Act
        await _service.ApagarAsync(moldeId);

        // Assert - Pendente (nao consumada) e excluida
        _lancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == pendente.Id)),
            Times.Once);
    }

    [Fact]
    public async Task ApagarAsync_Banco_MantemLancamentoPagoDesvinculadoComValoresIntactos()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var pago = CriarLancamentoBanco(moldeId, StatusLancamento.Pago, new DateOnly(2026, 9, 15));
        var molde = CriarMolde(moldeId, ativa: true, pago);
        DeixarMoldeExistente(molde);

        var valorOriginal = pago.Valor;
        var dataOriginal = pago.Data;
        var categoriaOriginal = pago.CategoriaId;

        // Act
        await _service.ApagarAsync(moldeId);

        // Assert - Pago (consumada) nunca e removido, apenas desvinculado
        _lancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == pago.Id)),
            Times.Never);

        Assert.Null(pago.ContaFixaId);
        Assert.Equal(valorOriginal, pago.Valor);
        Assert.Equal(dataOriginal, pago.Data);
        Assert.Equal(categoriaOriginal, pago.CategoriaId);
        Assert.Equal(StatusLancamento.Pago, pago.Status);
        Assert.Null(pago.Fatura);
    }

    [Fact]
    public async Task ApagarAsync_Banco_RemoveMoldeDaContaFixa()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var pendente = CriarLancamentoBanco(moldeId, StatusLancamento.Pendente, new DateOnly(2026, 11, 15));
        var molde = CriarMolde(moldeId, ativa: true, pendente);
        DeixarMoldeExistente(molde);

        // Act
        var resultado = await _service.ApagarAsync(moldeId);

        // Assert - molde removido do repositorio
        Assert.True(resultado.Sucesso);
        Assert.Null(resultado.Erro);
        Assert.Contains(moldeId, IdsMoldesRemovidos());
    }

    #endregion

    #region Apagar - destino cartao

    [Fact]
    public async Task ApagarAsync_Cartao_RemoveCompraEmFaturaAberta()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var compraAberta = CriarLancamentoCartao(moldeId, StatusFatura.Aberta, StatusLancamento.Pago, new DateOnly(2026, 11, 10));
        var molde = CriarMolde(moldeId, ativa: true, compraAberta);
        DeixarMoldeExistente(molde);

        // Act
        await _service.ApagarAsync(moldeId);

        // Assert
        _lancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == compraAberta.Id)),
            Times.Once);
    }

    [Fact]
    public async Task ApagarAsync_Cartao_RemoveCompraEmFaturaFechada()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var compraFechada = CriarLancamentoCartao(moldeId, StatusFatura.Fechada, StatusLancamento.Pago, new DateOnly(2026, 10, 10));
        var molde = CriarMolde(moldeId, ativa: true, compraFechada);
        DeixarMoldeExistente(molde);

        // Act
        await _service.ApagarAsync(moldeId);

        // Assert
        _lancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == compraFechada.Id)),
            Times.Once);
    }

    [Fact]
    public async Task ApagarAsync_Cartao_MantemCompraEmFaturaPagaDesvinculada()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var compraPaga = CriarLancamentoCartao(moldeId, StatusFatura.Paga, StatusLancamento.Pago, new DateOnly(2026, 9, 10));
        var molde = CriarMolde(moldeId, ativa: true, compraPaga);
        DeixarMoldeExistente(molde);

        var valorOriginal = compraPaga.Valor;
        var faturaOriginal = compraPaga.Fatura;

        // Act
        await _service.ApagarAsync(moldeId);

        // Assert - fatura paga e fato consumado: nao remove, so desvincula
        _lancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == compraPaga.Id)),
            Times.Never);

        Assert.Null(compraPaga.ContaFixaId);
        Assert.Equal(valorOriginal, compraPaga.Valor);
        Assert.Same(faturaOriginal, compraPaga.Fatura);
        Assert.Equal(StatusFatura.Paga, compraPaga.Fatura!.Status);
    }

    #endregion

    #region Apagar - casos de borda

    [Fact]
    public async Task ApagarAsync_ContaFixaInexistente_RetornaFalhaNaoEncontrada()
    {
        // Arrange - repositorio nao conhece o id
        var moldeId = Guid.NewGuid();
        _contaFixaRepository
            .Setup(r => r.ObterPorId(moldeId))
            .ReturnsAsync((ContaFixa?)null);

        // Act
        var resultado = await _service.ApagarAsync(moldeId);

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Equal("Conta fixa nao encontrada", resultado.Erro);
        _lancamentoRepository.Verify(r => r.Remover(It.IsAny<Lancamento>()), Times.Never);
        Assert.Empty(IdsMoldesRemovidos());
    }

    [Fact]
    public async Task ApagarAsync_ContaFixaJaDesativada_ApagaNormalmente()
    {
        // Arrange - molde Ativa=false (ja pausado) ainda pode ser apagado
        var moldeId = Guid.NewGuid();
        var pendente = CriarLancamentoBanco(moldeId, StatusLancamento.Pendente, new DateOnly(2026, 11, 15));
        var molde = CriarMolde(moldeId, ativa: false, pendente);
        DeixarMoldeExistente(molde);

        // Act
        var resultado = await _service.ApagarAsync(moldeId);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Null(resultado.Erro);
        _lancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == pendente.Id)),
            Times.Once);
        Assert.Contains(moldeId, IdsMoldesRemovidos());
    }

    [Fact]
    public async Task ApagarAsync_NaoTocaLancamentosNemMoldeDeOutraContaFixa()
    {
        // Arrange - duas contas fixas; apaga apenas a primeira
        var moldeAlvoId = Guid.NewGuid();
        var moldeOutroId = Guid.NewGuid();

        var pendenteAlvo = CriarLancamentoBanco(moldeAlvoId, StatusLancamento.Pendente, new DateOnly(2026, 11, 15));
        var pendenteOutro = CriarLancamentoBanco(moldeOutroId, StatusLancamento.Pendente, new DateOnly(2026, 11, 15));
        var pagoOutro = CriarLancamentoBanco(moldeOutroId, StatusLancamento.Pago, new DateOnly(2026, 9, 15));

        var moldeAlvo = CriarMolde(moldeAlvoId, ativa: true, pendenteAlvo);
        var moldeOutro = CriarMolde(moldeOutroId, ativa: true, pendenteOutro, pagoOutro);

        DeixarMoldeExistente(moldeAlvo);
        _contaFixaRepository
            .Setup(r => r.ObterPorId(moldeOutroId))
            .ReturnsAsync(moldeOutro);
        _contaFixaRepository
            .Setup(r => r.Listar(It.IsAny<bool?>()))
            .ReturnsAsync(new[] { moldeAlvo, moldeOutro });

        // Act
        await _service.ApagarAsync(moldeAlvoId);

        // Assert - outra conta fixa intacta: lancamentos e molde
        _lancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.ContaFixaId == moldeOutroId)),
            Times.Never);
        Assert.Equal(moldeOutroId, pendenteOutro.ContaFixaId);
        Assert.Equal(moldeOutroId, pagoOutro.ContaFixaId);
        Assert.DoesNotContain(moldeOutroId, IdsMoldesRemovidos());
    }

    #endregion

    #region Desativar corrigido (item 6) - destino cartao

    [Fact]
    public async Task DesativarAsync_ContaCartao_RemoveCompraEmFaturaNaoPaga()
    {
        // Arrange - hoje Desativar so remove Lancamento.Status=Pendente e ignora compra de cartao
        var moldeId = Guid.NewGuid();
        var compraAberta = CriarLancamentoCartao(moldeId, StatusFatura.Aberta, StatusLancamento.Pago, new DateOnly(2026, 11, 10));
        var molde = CriarMolde(moldeId, ativa: true, compraAberta);
        DeixarMoldeExistente(molde);

        // Act
        var resultado = await _service.DesativarAsync(moldeId);

        // Assert - compra em fatura nao paga e removida e o molde fica pausado
        Assert.True(resultado.Sucesso);
        _lancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == compraAberta.Id)),
            Times.Once);
        Assert.False(molde.Ativa);
    }

    [Fact]
    public async Task DesativarAsync_ContaCartao_NaoDesvinculaCompraEmFaturaPaga()
    {
        // Arrange - desativar nao desvincula consumadas (so Apagar desvincula)
        var moldeId = Guid.NewGuid();
        var compraPaga = CriarLancamentoCartao(moldeId, StatusFatura.Paga, StatusLancamento.Pago, new DateOnly(2026, 9, 10));
        var molde = CriarMolde(moldeId, ativa: true, compraPaga);
        DeixarMoldeExistente(molde);

        // Act
        await _service.DesativarAsync(moldeId);

        // Assert
        _lancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == compraPaga.Id)),
            Times.Never);
        Assert.Equal(moldeId, compraPaga.ContaFixaId);
        Assert.False(molde.Ativa);
    }

    #endregion

    #region Motor - RemoverOcorrenciasNaoConsumadasAsync

    [Fact]
    public async Task RemoverOcorrenciasNaoConsumadasAsync_Banco_RemoveSomentePendentesERetornaContagem()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var pendente1 = CriarLancamentoBanco(moldeId, StatusLancamento.Pendente, new DateOnly(2026, 11, 15));
        var pendente2 = CriarLancamentoBanco(moldeId, StatusLancamento.Pendente, new DateOnly(2026, 12, 15));
        var pago = CriarLancamentoBanco(moldeId, StatusLancamento.Pago, new DateOnly(2026, 9, 15));
        DeixarMoldeExistente(CriarMolde(moldeId, ativa: true, pendente1, pendente2, pago));

        // Act
        var removidas = await _motor.RemoverOcorrenciasNaoConsumadasAsync(moldeId);

        // Assert
        Assert.Equal(2, removidas);
        _lancamentoRepository.Verify(r => r.Remover(It.Is<Lancamento>(l => l.Id == pendente1.Id)), Times.Once);
        _lancamentoRepository.Verify(r => r.Remover(It.Is<Lancamento>(l => l.Id == pendente2.Id)), Times.Once);
        _lancamentoRepository.Verify(r => r.Remover(It.Is<Lancamento>(l => l.Id == pago.Id)), Times.Never);
    }

    [Fact]
    public async Task RemoverOcorrenciasNaoConsumadasAsync_Cartao_RemoveComprasEmFaturaAbertaOuFechada()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var compraAberta = CriarLancamentoCartao(moldeId, StatusFatura.Aberta, StatusLancamento.Pago, new DateOnly(2026, 11, 10));
        var compraFechada = CriarLancamentoCartao(moldeId, StatusFatura.Fechada, StatusLancamento.Pago, new DateOnly(2026, 10, 10));
        var compraPaga = CriarLancamentoCartao(moldeId, StatusFatura.Paga, StatusLancamento.Pago, new DateOnly(2026, 9, 10));
        DeixarMoldeExistente(CriarMolde(moldeId, ativa: true, compraAberta, compraFechada, compraPaga));

        // Act
        var removidas = await _motor.RemoverOcorrenciasNaoConsumadasAsync(moldeId);

        // Assert
        Assert.Equal(2, removidas);
        _lancamentoRepository.Verify(r => r.Remover(It.Is<Lancamento>(l => l.Id == compraAberta.Id)), Times.Once);
        _lancamentoRepository.Verify(r => r.Remover(It.Is<Lancamento>(l => l.Id == compraFechada.Id)), Times.Once);
        _lancamentoRepository.Verify(r => r.Remover(It.Is<Lancamento>(l => l.Id == compraPaga.Id)), Times.Never);
    }

    [Fact]
    public async Task RemoverOcorrenciasNaoConsumadasAsync_CartaoPendenteEmFaturaPaga_NaoRemove()
    {
        // Arrange - Status=Pendente nao define consumada no cartao: o que vale e a Fatura
        var moldeId = Guid.NewGuid();
        var pendenteEmFaturaPaga = CriarLancamentoCartao(moldeId, StatusFatura.Paga, StatusLancamento.Pendente, new DateOnly(2026, 9, 10));
        DeixarMoldeExistente(CriarMolde(moldeId, ativa: true, pendenteEmFaturaPaga));

        // Act
        var removidas = await _motor.RemoverOcorrenciasNaoConsumadasAsync(moldeId);

        // Assert
        Assert.Equal(0, removidas);
        _lancamentoRepository.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == pendenteEmFaturaPaga.Id)),
            Times.Never);
    }

    [Fact]
    public async Task RemoverOcorrenciasNaoConsumadasAsync_ContaFixaInexistente_RetornaZero()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        _contaFixaRepository
            .Setup(r => r.ObterPorId(moldeId))
            .ReturnsAsync((ContaFixa?)null);

        // Act
        var removidas = await _motor.RemoverOcorrenciasNaoConsumadasAsync(moldeId);

        // Assert
        Assert.Equal(0, removidas);
        _lancamentoRepository.Verify(r => r.Remover(It.IsAny<Lancamento>()), Times.Never);
    }

    #endregion

    #region Motor - DesvincularOcorrenciasConsumadasAsync

    [Fact]
    public async Task DesvincularOcorrenciasConsumadasAsync_Banco_DesvinculaPagosMantendoValoresERetornaContagem()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var pago = CriarLancamentoBanco(moldeId, StatusLancamento.Pago, new DateOnly(2026, 9, 15));
        var pendente = CriarLancamentoBanco(moldeId, StatusLancamento.Pendente, new DateOnly(2026, 11, 15));
        DeixarMoldeExistente(CriarMolde(moldeId, ativa: true, pago, pendente));

        var valorOriginal = pago.Valor;
        var dataOriginal = pago.Data;
        var categoriaOriginal = pago.CategoriaId;

        // Act
        var desvinculadas = await _motor.DesvincularOcorrenciasConsumadasAsync(moldeId);

        // Assert
        Assert.Equal(1, desvinculadas);
        Assert.Null(pago.ContaFixaId);
        Assert.Equal(valorOriginal, pago.Valor);
        Assert.Equal(dataOriginal, pago.Data);
        Assert.Equal(categoriaOriginal, pago.CategoriaId);
        Assert.Equal(StatusLancamento.Pago, pago.Status);

        // Pendente nao e consumada: continua vinculada
        Assert.Equal(moldeId, pendente.ContaFixaId);
    }

    [Fact]
    public async Task DesvincularOcorrenciasConsumadasAsync_CartaoFaturaPaga_DesvinculaMantendoFatura()
    {
        // Arrange - compra em fatura Aberta nao e consumada e permanece vinculada
        var moldeId = Guid.NewGuid();
        var compraPaga = CriarLancamentoCartao(moldeId, StatusFatura.Paga, StatusLancamento.Pago, new DateOnly(2026, 9, 10));
        var compraAberta = CriarLancamentoCartao(moldeId, StatusFatura.Aberta, StatusLancamento.Pago, new DateOnly(2026, 11, 10));
        DeixarMoldeExistente(CriarMolde(moldeId, ativa: true, compraPaga, compraAberta));

        var valorOriginal = compraPaga.Valor;
        var faturaOriginal = compraPaga.Fatura;

        // Act
        var desvinculadas = await _motor.DesvincularOcorrenciasConsumadasAsync(moldeId);

        // Assert
        Assert.Equal(1, desvinculadas);
        Assert.Null(compraPaga.ContaFixaId);
        Assert.Equal(valorOriginal, compraPaga.Valor);
        Assert.Same(faturaOriginal, compraPaga.Fatura);
        Assert.Equal(moldeId, compraAberta.ContaFixaId);
    }

    [Fact]
    public async Task DesvincularOcorrenciasConsumadasAsync_ContaFixaInexistente_RetornaZero()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        _contaFixaRepository
            .Setup(r => r.ObterPorId(moldeId))
            .ReturnsAsync((ContaFixa?)null);

        // Act
        var desvinculadas = await _motor.DesvincularOcorrenciasConsumadasAsync(moldeId);

        // Assert
        Assert.Equal(0, desvinculadas);
    }

    #endregion

    #region Helpers

    private void DeixarMoldeExistente(ContaFixa molde)
    {
        _contaFixaRepository
            .Setup(r => r.ObterPorId(molde.Id))
            .ReturnsAsync(molde);
    }

    // Verifica remocao do molde pelo repositorio (IContaFixaRepository.Remover).
    // Lido via Invocations para nao depender da assinatura antes de ela existir.
    private IEnumerable<Guid> IdsMoldesRemovidos()
    {
        return _contaFixaRepository.Invocations
            .Where(i => i.Method.Name == "Remover")
            .Select(i => ((ContaFixa)i.Arguments[0]).Id)
            .ToList();
    }

    private static ContaFixa CriarMolde(Guid id, bool ativa, params Lancamento[] lancamentos)
    {
        var molde = new ContaFixa
        {
            Id = id,
            ContaId = Guid.NewGuid(),
            Descricao = "Aluguel",
            Valor = 1500m,
            DiaVencimento = 15,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            Ativa = ativa,
            Lancamentos = lancamentos.ToList()
        };

        return molde;
    }

    private static Lancamento CriarLancamentoBanco(Guid contaFixaId, StatusLancamento status, DateOnly data)
    {
        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            ContaFixaId = contaFixaId,
            CategoriaId = Guid.NewGuid(),
            Descricao = "Aluguel",
            Valor = 1500m,
            Tipo = TipoLancamento.Debit,
            Data = data,
            Status = status,
            Manual = true,
            Fatura = null
        };
    }

    private static Lancamento CriarLancamentoCartao(Guid contaFixaId, StatusFatura statusFatura, StatusLancamento status, DateOnly data)
    {
        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            ContaFixaId = contaFixaId,
            CategoriaId = Guid.NewGuid(),
            Descricao = "Parcela Cartao",
            Valor = 500m,
            Tipo = TipoLancamento.Debit,
            Data = data,
            Status = status,
            Manual = true,
            Fatura = new Fatura
            {
                Id = Guid.NewGuid(),
                Status = statusFatura
            }
        };
    }

    #endregion
}
