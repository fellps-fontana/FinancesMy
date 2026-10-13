using Moq;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

// Regra-de-negocio item 18: so lancamento avulso edita ou exclui pelo
// LancamentoManualService. Nao avulso -> LancamentoNaoAvulsoException antes de
// qualquer validacao, sem Atualizar/Salvar/Remover.
//
// Casos de categoria x tipo (Editar e Criar, categoria inexistente) usam o
// mock de ICategoriaRepository do construtor, no fim do arquivo.
public class LancamentoAvulsoGuardServiceTests
{
    private readonly Mock<ILancamentoRepository> _lancamentoRepositoryMock;
    private readonly Mock<IContaRepository> _contaRepositoryMock;
    private readonly Mock<ICategoriaRepository> _categoriaRepositoryMock;
    private readonly LancamentoManualService _service;

    public LancamentoAvulsoGuardServiceTests()
    {
        _lancamentoRepositoryMock = new Mock<ILancamentoRepository>();
        _contaRepositoryMock = new Mock<IContaRepository>();
        _categoriaRepositoryMock = new Mock<ICategoriaRepository>();

        _service = new LancamentoManualService(
            _lancamentoRepositoryMock.Object,
            _contaRepositoryMock.Object,
            _categoriaRepositoryMock.Object);
    }

    // ---------- Helpers ----------

    private static Lancamento CriarLancamentoAvulso(Guid contaId, Guid lancamentoId)
    {
        return new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Almoco",
            Valor = 50m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2025, 3, 10),
            Status = StatusLancamento.Pendente,
            Manual = true,
            CategoriaId = null
        };
    }

    // Monta um lancamento nao avulso pela origem pedida. Manual=false cobre NaoManual.
    private static Lancamento CriarLancamentoComOrigem(Guid contaId, Guid lancamentoId, string vinculo, bool manual)
    {
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);
        lancamento.Manual = manual;

        switch (vinculo)
        {
            case "":
                break;
            case "ContaFixaId":
                lancamento.ContaFixaId = Guid.NewGuid();
                break;
            case "CompraParceladaId":
                lancamento.CompraParceladaId = Guid.NewGuid();
                break;
            case "FaturaId":
                lancamento.FaturaId = Guid.NewGuid();
                break;
            case "AssinaturaCartaoId":
                lancamento.AssinaturaCartaoId = Guid.NewGuid();
                break;
            case "ContaReceberId":
                lancamento.ContaReceberId = Guid.NewGuid();
                break;
            case "TransferenciaId":
                lancamento.TransferenciaId = Guid.NewGuid();
                break;
            case "ConciliadoCom":
                lancamento.ConciliadoCom = Guid.NewGuid();
                break;
            default:
                throw new ArgumentException($"Vinculo desconhecido no teste: {vinculo}", nameof(vinculo));
        }

        return lancamento;
    }

    private static EditarLancamentoRequest CriarRequestEdicaoValida(Guid? contaDestinoId = null)
    {
        return new EditarLancamentoRequest
        {
            Descricao = "Almoco editado",
            Valor = 75m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 3, 12),
            Status = "PAGO",
            ContaId = contaDestinoId
        };
    }

    private static Conta CriarConta(Guid id, bool ativa = true, OrigemConta origem = OrigemConta.Manual, TipoConta? tipo = TipoConta.Banco)
    {
        return new Conta
        {
            Id = id,
            Nome = "Conta " + id.ToString()[..4],
            Ativa = ativa,
            Origem = origem,
            Tipo = tipo
        };
    }

    // ---------- Guard: Editar nao avulso ----------

    [Theory]
    [InlineData("ContaFixaId", true, OrigemVinculoLancamento.ContaFixa)]
    [InlineData("CompraParceladaId", true, OrigemVinculoLancamento.CompraParcelada)]
    [InlineData("FaturaId", true, OrigemVinculoLancamento.Cartao)]
    [InlineData("AssinaturaCartaoId", true, OrigemVinculoLancamento.Cartao)]
    [InlineData("ContaReceberId", true, OrigemVinculoLancamento.ContaReceber)]
    [InlineData("TransferenciaId", true, OrigemVinculoLancamento.Transferencia)]
    [InlineData("ConciliadoCom", true, OrigemVinculoLancamento.Conciliado)]
    [InlineData("", false, OrigemVinculoLancamento.NaoManual)]
    public async Task EditarAsync_LancamentoNaoAvulso_LancaLancamentoNaoAvulsoExceptionComOrigemCorreta(
        string vinculo,
        bool manual,
        OrigemVinculoLancamento origemEsperada)
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoComOrigem(contaId, lancamentoId, vinculo, manual);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        var excecao = await Assert.ThrowsAsync<LancamentoNaoAvulsoException>(
            () => _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoValida()));

        Assert.Equal(origemEsperada, excecao.Origem);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_NaoAvulsoComDadosInvalidos_LancaExcecaoAntesDeValidar()
    {
        // Guard vem ANTES de validacao: descricao vazia e valor zero nao viram Sucesso=false.
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoComOrigem(contaId, lancamentoId, "ContaFixaId", manual: true);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        var request = new EditarLancamentoRequest
        {
            Descricao = "",
            Valor = 0m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 3, 12)
        };

        await Assert.ThrowsAsync<LancamentoNaoAvulsoException>(
            () => _service.EditarAsync(contaId, lancamentoId, request));

        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_NaoAvulsoComTrocaDeConta_LancaExcecaoSemTrocarConta()
    {
        // Guard vem ANTES de validar a conta de destino.
        var contaId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoComOrigem(contaId, lancamentoId, "CompraParceladaId", manual: true);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        await Assert.ThrowsAsync<LancamentoNaoAvulsoException>(
            () => _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoValida(contaDestinoId)));

        _contaRepositoryMock.Verify(r => r.ObterPorId(contaDestinoId), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    // ---------- Guard: Remover nao avulso ----------

    [Theory]
    [InlineData("ContaFixaId", true, OrigemVinculoLancamento.ContaFixa)]
    [InlineData("CompraParceladaId", true, OrigemVinculoLancamento.CompraParcelada)]
    [InlineData("FaturaId", true, OrigemVinculoLancamento.Cartao)]
    [InlineData("AssinaturaCartaoId", true, OrigemVinculoLancamento.Cartao)]
    [InlineData("ContaReceberId", true, OrigemVinculoLancamento.ContaReceber)]
    [InlineData("TransferenciaId", true, OrigemVinculoLancamento.Transferencia)]
    [InlineData("ConciliadoCom", true, OrigemVinculoLancamento.Conciliado)]
    [InlineData("", false, OrigemVinculoLancamento.NaoManual)]
    public async Task RemoverAsync_LancamentoNaoAvulso_LancaLancamentoNaoAvulsoExceptionSemRemover(
        string vinculo,
        bool manual,
        OrigemVinculoLancamento origemEsperada)
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoComOrigem(contaId, lancamentoId, vinculo, manual);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        var excecao = await Assert.ThrowsAsync<LancamentoNaoAvulsoException>(
            () => _service.RemoverAsync(contaId, lancamentoId));

        Assert.Equal(origemEsperada, excecao.Origem);
        _lancamentoRepositoryMock.Verify(r => r.Remover(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    // ---------- Nao encontrado: sem excecao, Sucesso=false ----------

    [Fact]
    public async Task EditarAsync_LancamentoNaoEncontrado_RetornaSucessoFalsoSemExcecao()
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync((Lancamento?)null);

        var (sucesso, lancamento, erro) = await _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoValida());

        Assert.False(sucesso);
        Assert.Null(lancamento);
        Assert.NotNull(erro);
    }

    // ---------- Avulso: Remover ----------

    [Fact]
    public async Task RemoverAsync_AvulsoSemVinculo_RemoveSemLancarExcecao()
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        var (sucesso, erro) = await _service.RemoverAsync(contaId, lancamentoId);

        Assert.True(sucesso);
        Assert.Null(erro);
        _lancamentoRepositoryMock.Verify(r => r.Remover(lancamento), Times.Once);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Once);
    }

    // ---------- Avulso: Editar (campos) ----------

    [Fact]
    public async Task EditarAsync_AvulsoComDescricaoValorEData_AtualizaCamposInformados()
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        var (sucesso, editado, erro) = await _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoValida());

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(editado);
        Assert.Equal("Almoco editado", lancamento.Descricao);
        Assert.Equal(75m, lancamento.Valor);
        Assert.Equal(new DateOnly(2025, 3, 12), lancamento.Data);
    }

    [Fact]
    public async Task EditarAsync_AvulsoTrocandoTipoParaCredit_AtualizaTipoELimpaCategoria()
    {
        // Ao trocar tipo a UI envia CategoriaId null: o lancamento nao pode manter categoria antiga.
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);
        lancamento.CategoriaId = Guid.NewGuid();

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        var request = new EditarLancamentoRequest
        {
            Descricao = "Salario",
            Valor = 1000m,
            Tipo = "CREDIT",
            Data = new DateOnly(2025, 3, 5),
            CategoriaId = null
        };

        var (sucesso, _, erro) = await _service.EditarAsync(contaId, lancamentoId, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(TipoLancamento.Credit, lancamento.Tipo);
        Assert.Null(lancamento.CategoriaId);
    }

    [Fact]
    public async Task EditarAsync_AvulsoComStatusPagoParaPendente_DesmarcaPagamento()
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);
        lancamento.Status = StatusLancamento.Pago;

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        var request = new EditarLancamentoRequest
        {
            Descricao = "Almoco",
            Valor = 50m,
            Tipo = "DEBIT",
            Data = new DateOnly(2025, 3, 10),
            Status = "PENDENTE"
        };

        var (sucesso, _, erro) = await _service.EditarAsync(contaId, lancamentoId, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(StatusLancamento.Pendente, lancamento.Status);
    }

    [Fact]
    public async Task EditarAsync_AvulsoComStatusSugerido_RetornaErroSemSalvar()
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        var request = CriarRequestEdicaoValida();
        request.Status = "SUGERIDO";

        var (sucesso, _, erro) = await _service.EditarAsync(contaId, lancamentoId, request);

        Assert.False(sucesso);
        Assert.NotNull(erro);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_AvulsoComValorZero_RetornaErroSemSalvar()
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        var request = CriarRequestEdicaoValida();
        request.Valor = 0m;

        var (sucesso, _, erro) = await _service.EditarAsync(contaId, lancamentoId, request);

        Assert.False(sucesso);
        Assert.NotNull(erro);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_AvulsoNaoAlteraSaldoManualDaConta()
    {
        // Regra item 18 / item 10: editar lancamento nao recalcula saldo_manual.
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var conta = CriarConta(contaId);
        conta.SaldoManual = 1234.56m;
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);
        lancamento.Conta = conta;

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);

        var (sucesso, _, _) = await _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoValida());

        Assert.True(sucesso);
        Assert.Equal(1234.56m, conta.SaldoManual);
    }

    // ---------- Avulso: troca de conta (request.ContaId) ----------

    [Fact]
    public async Task EditarAsync_AvulsoComContaIdOmitido_MantemConta()
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);

        var (sucesso, _, _) = await _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoValida(contaDestinoId: null));

        Assert.True(sucesso);
        Assert.Equal(contaId, lancamento.ContaId);
    }

    [Fact]
    public async Task EditarAsync_AvulsoComContaIdIgualAAtual_MantemConta()
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(CriarConta(contaId));

        var (sucesso, _, _) = await _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoValida(contaId));

        Assert.True(sucesso);
        Assert.Equal(contaId, lancamento.ContaId);
    }

    [Fact]
    public async Task EditarAsync_AvulsoComContaDestinoValida_MudaParaContaDestino()
    {
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaOrigemId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(CriarConta(contaDestinoId));

        var (sucesso, _, erro) = await _service.EditarAsync(contaOrigemId, lancamentoId, CriarRequestEdicaoValida(contaDestinoId));

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(contaDestinoId, lancamento.ContaId);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(lancamento), Times.Once);
    }

    [Fact]
    public async Task EditarAsync_AvulsoComContaDestinoInexistente_RetornaErroSemSalvar()
    {
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaOrigemId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync((Conta?)null);

        var (sucesso, _, erro) = await _service.EditarAsync(contaOrigemId, lancamentoId, CriarRequestEdicaoValida(contaDestinoId));

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Equal(contaOrigemId, lancamento.ContaId);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_AvulsoComContaDestinoInativa_RetornaErroSemSalvar()
    {
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaOrigemId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(CriarConta(contaDestinoId, ativa: false));

        var (sucesso, _, erro) = await _service.EditarAsync(contaOrigemId, lancamentoId, CriarRequestEdicaoValida(contaDestinoId));

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Equal(contaOrigemId, lancamento.ContaId);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_AvulsoComContaDestinoOpenFinance_RetornaErroSemSalvar()
    {
        // So conta MANUAL aceita lancamento avulso (item 18).
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaOrigemId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(CriarConta(contaDestinoId, origem: OrigemConta.OpenFinance));

        var (sucesso, _, erro) = await _service.EditarAsync(contaOrigemId, lancamentoId, CriarRequestEdicaoValida(contaDestinoId));

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Equal(contaOrigemId, lancamento.ContaId);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_AvulsoComContaDestinoCartao_RetornaErroSemSalvar()
    {
        // Compra de cartao e outro fluxo (item 12): conta CARTAO nao recebe avulso.
        var contaOrigemId = Guid.NewGuid();
        var contaDestinoId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaOrigemId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _contaRepositoryMock.Setup(r => r.ObterPorId(contaDestinoId))
            .ReturnsAsync(CriarConta(contaDestinoId, tipo: TipoConta.Cartao));

        var (sucesso, _, erro) = await _service.EditarAsync(contaOrigemId, lancamentoId, CriarRequestEdicaoValida(contaDestinoId));

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Equal(contaOrigemId, lancamento.ContaId);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
    }

    // ---------- Categoria x tipo (item 18 e decisao de Criar: mesma validacao) ----------

    private static Categoria CriarCategoria(Guid id, TipoCategoria tipo)
    {
        return new Categoria
        {
            Id = id,
            Nome = "Categoria " + id.ToString()[..4],
            Tipo = tipo
        };
    }

    private static EditarLancamentoRequest CriarRequestEdicaoComTipo(string tipo, Guid? categoriaId)
    {
        var request = CriarRequestEdicaoValida();
        request.Tipo = tipo;
        request.CategoriaId = categoriaId;
        return request;
    }

    private static CriarLancamentoRequest CriarRequestCriacaoComTipo(string tipo, Guid? categoriaId)
    {
        return new CriarLancamentoRequest
        {
            Descricao = "Almoco",
            Valor = 50m,
            Tipo = tipo,
            CategoriaId = categoriaId,
            Data = new DateOnly(2025, 3, 10),
            Status = "PENDENTE"
        };
    }

    [Fact]
    public async Task EditarAsync_AvulsoDebitComCategoriaReceita_RetornaErroSemSalvar()
    {
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _categoriaRepositoryMock.Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(CriarCategoria(categoriaId, TipoCategoria.Receita));

        var (sucesso, _, erro) = await _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoComTipo("DEBIT", categoriaId));

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(lancamento.CategoriaId);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_AvulsoCreditComCategoriaDespesa_RetornaErroSemSalvar()
    {
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _categoriaRepositoryMock.Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(CriarCategoria(categoriaId, TipoCategoria.Despesa));

        var (sucesso, _, erro) = await _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoComTipo("CREDIT", categoriaId));

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(lancamento.CategoriaId);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_AvulsoComCategoriaInexistente_RetornaErroSemSalvar()
    {
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _categoriaRepositoryMock.Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync((Categoria?)null);

        var (sucesso, _, erro) = await _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoComTipo("DEBIT", categoriaId));

        Assert.False(sucesso);
        Assert.NotNull(erro);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_AvulsoDebitComCategoriaDespesa_SalvaCategoria()
    {
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _categoriaRepositoryMock.Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(CriarCategoria(categoriaId, TipoCategoria.Despesa));

        var (sucesso, _, erro) = await _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoComTipo("DEBIT", categoriaId));

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(categoriaId, lancamento.CategoriaId);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(lancamento), Times.Once);
    }

    [Fact]
    public async Task EditarAsync_AvulsoCreditComCategoriaReceita_SalvaCategoria()
    {
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        var lancamento = CriarLancamentoAvulso(contaId, lancamentoId);

        _lancamentoRepositoryMock.Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(lancamento);
        _categoriaRepositoryMock.Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(CriarCategoria(categoriaId, TipoCategoria.Receita));

        var (sucesso, _, erro) = await _service.EditarAsync(contaId, lancamentoId, CriarRequestEdicaoComTipo("CREDIT", categoriaId));

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(TipoLancamento.Credit, lancamento.Tipo);
        Assert.Equal(categoriaId, lancamento.CategoriaId);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(lancamento), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_DebitComCategoriaReceita_RetornaErroSemAdicionar()
    {
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(CriarConta(contaId));
        _categoriaRepositoryMock.Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(CriarCategoria(categoriaId, TipoCategoria.Receita));

        var (sucesso, _, erro) = await _service.CriarAsync(contaId, CriarRequestCriacaoComTipo("DEBIT", categoriaId));

        Assert.False(sucesso);
        Assert.NotNull(erro);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_CreditComCategoriaDespesa_RetornaErroSemAdicionar()
    {
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(CriarConta(contaId));
        _categoriaRepositoryMock.Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(CriarCategoria(categoriaId, TipoCategoria.Despesa));

        var (sucesso, _, erro) = await _service.CriarAsync(contaId, CriarRequestCriacaoComTipo("CREDIT", categoriaId));

        Assert.False(sucesso);
        Assert.NotNull(erro);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_DebitComCategoriaDespesa_Cria()
    {
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();

        _contaRepositoryMock.Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(CriarConta(contaId));
        _categoriaRepositoryMock.Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(CriarCategoria(categoriaId, TipoCategoria.Despesa));

        var (sucesso, lancamento, erro) = await _service.CriarAsync(contaId, CriarRequestCriacaoComTipo("DEBIT", categoriaId));

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(lancamento);
        Assert.Equal(categoriaId, lancamento!.CategoriaId);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Once);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Once);
    }
}
