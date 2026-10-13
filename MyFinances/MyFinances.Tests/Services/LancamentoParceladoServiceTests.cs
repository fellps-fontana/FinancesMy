using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

// Regra-de-negocio item 17 (lancamento parcelado em conta banco) e item 18
// (guarda de avulso em parcela). Unitario puro: repositorios mockados com Moq,
// sem banco.
//
// Leitura das parcelas gravadas: o teste considera tanto o que foi passado a
// ILancamentoRepository.Adicionar quanto o que foi anexado a
// CompraParcelada.Lancamentos no Adicionar da compra. Assim o teste nao amarra
// o desenho de persistencia que o levi escolher.
public class LancamentoParceladoServiceTests
{
    private readonly Mock<ILancamentoRepository> _lancamentoRepositoryMock;
    private readonly Mock<ICompraParceladaRepository> _compraRepositoryMock;
    private readonly Mock<IContaRepository> _contaRepositoryMock;
    private readonly Mock<ICategoriaRepository> _categoriaRepositoryMock;
    private readonly LancamentoParceladoService _service;

    private readonly List<Lancamento> _lancamentosAdicionados = new();
    private CompraParcelada? _compraAdicionada;

    public LancamentoParceladoServiceTests()
    {
        _lancamentoRepositoryMock = new Mock<ILancamentoRepository>();
        _compraRepositoryMock = new Mock<ICompraParceladaRepository>();
        _contaRepositoryMock = new Mock<IContaRepository>();
        _categoriaRepositoryMock = new Mock<ICategoriaRepository>();

        _compraRepositoryMock
            .Setup(r => r.BeginTransactionAsync())
            .ReturnsAsync(new Mock<IDbContextTransaction>().Object);

        _lancamentoRepositoryMock
            .Setup(r => r.Adicionar(It.IsAny<Lancamento>()))
            .Returns(Task.CompletedTask)
            .Callback<Lancamento>(l => _lancamentosAdicionados.Add(l));

        _compraRepositoryMock
            .Setup(r => r.Adicionar(It.IsAny<CompraParcelada>()))
            .Returns(Task.CompletedTask)
            .Callback<CompraParcelada>(c => _compraAdicionada = c);

        _service = new LancamentoParceladoService(
            _lancamentoRepositoryMock.Object,
            _compraRepositoryMock.Object,
            _contaRepositoryMock.Object,
            _categoriaRepositoryMock.Object);
    }

    // ---------- Helpers: CriarAsync ----------

    private Guid ConfigurarConta(
        bool ativa = true,
        OrigemConta origem = OrigemConta.Manual,
        TipoConta? tipo = TipoConta.Banco)
    {
        var contaId = Guid.NewGuid();
        _contaRepositoryMock
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(new Conta
            {
                Id = contaId,
                Nome = "Conta teste",
                Ativa = ativa,
                Origem = origem,
                Tipo = tipo
            });
        return contaId;
    }

    private void ConfigurarCategoria(Guid categoriaId, TipoCategoria tipo)
    {
        _categoriaRepositoryMock
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync(new Categoria { Id = categoriaId, Nome = "Categoria teste", Tipo = tipo });
    }

    private static CriarLancamentoParceladoRequest CriarRequest(
        decimal valor = 100m,
        int quantidade = 3,
        DateOnly? dataPrimeiraParcela = null,
        string status = "PENDENTE",
        Guid? categoriaId = null,
        string descricao = "Notebook")
    {
        return new CriarLancamentoParceladoRequest
        {
            Descricao = descricao,
            ValorTotal = valor,
            QuantidadeParcelas = quantidade,
            DataPrimeiraParcela = dataPrimeiraParcela ?? new DateOnly(2026, 1, 15),
            CategoriaId = categoriaId,
            Status = status
        };
    }

    // Parcelas gravadas, ordenadas por ParcelaNumero (ver comentario da classe).
    private IReadOnlyList<Lancamento> LancamentosGravados()
    {
        var anexadosNaCompra = (IEnumerable<Lancamento>?)_compraAdicionada?.Lancamentos ?? Array.Empty<Lancamento>();

        return _lancamentosAdicionados
            .Concat(anexadosNaCompra)
            .Distinct()
            .OrderBy(l => l.ParcelaNumero)
            .ToList();
    }

    private void VerificarNadaGravado()
    {
        _compraRepositoryMock.Verify(r => r.Adicionar(It.IsAny<CompraParcelada>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
        _compraRepositoryMock.Verify(r => r.Salvar(), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
        _compraRepositoryMock.Verify(r => r.CommitAsync(), Times.Never);
    }

    // ---------- Helpers: CancelarAsync ----------

    private static CompraParcelada CriarCompra(
        Guid contaId,
        StatusLancamento[] statusParcelas,
        Guid? faturaId = null)
    {
        var dataCompra = new DateOnly(2026, 1, 15);
        var compra = new CompraParcelada
        {
            Id = Guid.NewGuid(),
            Descricao = "Notebook",
            ValorTotal = 100m * statusParcelas.Length,
            QuantidadeParcelas = statusParcelas.Length,
            DataCompra = dataCompra
        };

        for (var i = 0; i < statusParcelas.Length; i++)
        {
            compra.Lancamentos.Add(new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Descricao = "Notebook",
                Valor = 100m,
                Tipo = TipoLancamento.Debit,
                Data = dataCompra.AddMonths(i),
                Status = statusParcelas[i],
                Manual = true,
                FaturaId = faturaId,
                CompraParceladaId = compra.Id,
                ParcelaNumero = i + 1,
                CompraParcelada = compra
            });
        }

        return compra;
    }

    private void ConfigurarCompraExistente(CompraParcelada compra)
    {
        _compraRepositoryMock
            .Setup(r => r.ObterPorId(compra.Id))
            .ReturnsAsync(compra);
    }

    private void VerificarRemovidos(int quantidade)
    {
        _lancamentoRepositoryMock.Verify(r => r.Remover(It.IsAny<Lancamento>()), Times.Exactly(quantidade));
    }

    // ---------- CriarAsync: caminho feliz ----------

    [Fact]
    public async Task CriarAsync_ParcelaUmPaga_GravaTresDebitosComPrimeiraPagaEDemaisPendentes()
    {
        var contaId = ConfigurarConta();

        var (sucesso, resposta, erro) = await _service.CriarAsync(
            contaId,
            CriarRequest(valor: 100m, quantidade: 3, status: "PAGO"));

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(resposta);

        var parcelas = LancamentosGravados();
        Assert.Equal(3, parcelas.Count);
        Assert.All(parcelas, l => Assert.Equal(TipoLancamento.Debit, l.Tipo));
        Assert.Equal(new int?[] { 1, 2, 3 }, parcelas.Select(l => l.ParcelaNumero));
        Assert.Equal(StatusLancamento.Pago, parcelas[0].Status);
        Assert.Equal(StatusLancamento.Pendente, parcelas[1].Status);
        Assert.Equal(StatusLancamento.Pendente, parcelas[2].Status);

        var compraId = parcelas[0].CompraParceladaId;
        Assert.NotNull(compraId);
        Assert.All(parcelas, l => Assert.Equal(compraId, l.CompraParceladaId));
        Assert.All(parcelas, l => Assert.Null(l.FaturaId));
        Assert.All(parcelas, l => Assert.True(l.Manual));
        Assert.Equal(compraId, resposta!.CompraParceladaId);
    }

    [Fact]
    public async Task CriarAsync_StatusPendente_TodasAsParcelasNascemPendentes()
    {
        var contaId = ConfigurarConta();

        var (sucesso, _, erro) = await _service.CriarAsync(
            contaId,
            CriarRequest(valor: 100m, quantidade: 3, status: "PENDENTE"));

        Assert.True(sucesso);
        Assert.Null(erro);

        var parcelas = LancamentosGravados();
        Assert.Equal(3, parcelas.Count);
        Assert.All(parcelas, l => Assert.Equal(StatusLancamento.Pendente, l.Status));
    }

    [Fact]
    public async Task CriarAsync_ValorNaoDivisivelEmTres_UltimaParcelaRecebeOResto()
    {
        var contaId = ConfigurarConta();

        await _service.CriarAsync(contaId, CriarRequest(valor: 100m, quantidade: 3));

        var parcelas = LancamentosGravados();
        Assert.Equal(new[] { 33.33m, 33.33m, 33.34m }, parcelas.Select(l => l.Valor));
        Assert.Equal(100m, parcelas.Sum(l => l.Valor));
    }

    [Fact]
    public async Task CriarAsync_Sucesso_PersisteCompraParceladaComValorQuantidadeEDataCompra()
    {
        var contaId = ConfigurarConta();

        await _service.CriarAsync(
            contaId,
            CriarRequest(valor: 100m, quantidade: 3, dataPrimeiraParcela: new DateOnly(2026, 1, 15)));

        Assert.NotNull(_compraAdicionada);
        Assert.Equal("Notebook", _compraAdicionada!.Descricao);
        Assert.Equal(100m, _compraAdicionada.ValorTotal);
        Assert.Equal(3, _compraAdicionada.QuantidadeParcelas);
        Assert.Equal(new DateOnly(2026, 1, 15), _compraAdicionada.DataCompra);
    }

    [Fact]
    public async Task CriarAsync_DataDiaTrintaEUmTresParcelas_DatasSeguemCalculoDeParcelamento()
    {
        var contaId = ConfigurarConta();

        await _service.CriarAsync(
            contaId,
            CriarRequest(valor: 300m, quantidade: 3, dataPrimeiraParcela: new DateOnly(2026, 1, 31)));

        var parcelas = LancamentosGravados();
        Assert.Equal(
            new[] { new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31) },
            parcelas.Select(l => l.Data));
    }

    [Fact]
    public async Task CriarAsync_Sucesso_ParcelasHerdamDescricaoCategoriaEConta()
    {
        var contaId = ConfigurarConta();
        var categoriaId = Guid.NewGuid();
        ConfigurarCategoria(categoriaId, TipoCategoria.Despesa);

        await _service.CriarAsync(
            contaId,
            CriarRequest(valor: 100m, quantidade: 3, categoriaId: categoriaId, descricao: "Notebook"));

        var parcelas = LancamentosGravados();
        Assert.Equal(3, parcelas.Count);
        Assert.All(parcelas, l => Assert.Equal("Notebook", l.Descricao));
        Assert.All(parcelas, l => Assert.Equal(categoriaId, l.CategoriaId));
        Assert.All(parcelas, l => Assert.Equal(contaId, l.ContaId));
    }

    [Fact]
    public async Task CriarAsync_Sucesso_RespostaTrazTotalParcelasIgualAQuantidade()
    {
        var contaId = ConfigurarConta();

        var (sucesso, resposta, _) = await _service.CriarAsync(
            contaId,
            CriarRequest(valor: 100m, quantidade: 3));

        Assert.True(sucesso);
        Assert.NotNull(resposta);
        Assert.Equal(3, resposta!.Parcelas.Count);
        Assert.All(resposta.Parcelas, p => Assert.Equal(3, p.TotalParcelas));
    }

    [Fact]
    public async Task CriarAsync_CategoriaNula_AceitaELancaSemCategoria()
    {
        var contaId = ConfigurarConta();

        var (sucesso, _, erro) = await _service.CriarAsync(
            contaId,
            CriarRequest(valor: 100m, quantidade: 3, categoriaId: null));

        Assert.True(sucesso);
        Assert.Null(erro);
        var parcelas = LancamentosGravados();
        Assert.Equal(3, parcelas.Count);
        Assert.All(parcelas, l => Assert.Null(l.CategoriaId));
    }

    [Fact]
    public async Task CriarAsync_ContaInvestimento_AceitaOLancamento()
    {
        var contaId = ConfigurarConta(tipo: TipoConta.Investimento);

        var (sucesso, _, erro) = await _service.CriarAsync(contaId, CriarRequest());

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(3, LancamentosGravados().Count);
    }

    // ---------- CriarAsync: validacao de quantidade, valor, descricao, status ----------

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public async Task CriarAsync_QuantidadeMenorQueDois_RetornaErroSemGravarNada(int quantidade)
    {
        var contaId = ConfigurarConta();

        var (sucesso, resposta, erro) = await _service.CriarAsync(
            contaId,
            CriarRequest(quantidade: quantidade));

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    [Fact]
    public async Task CriarAsync_ValorZero_RetornaErroSemGravarNada()
    {
        var contaId = ConfigurarConta();

        var (sucesso, resposta, erro) = await _service.CriarAsync(
            contaId,
            CriarRequest(valor: 0m));

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    [Fact]
    public async Task CriarAsync_ValorNegativo_RetornaErroSemGravarNada()
    {
        var contaId = ConfigurarConta();

        var (sucesso, resposta, erro) = await _service.CriarAsync(
            contaId,
            CriarRequest(valor: -50m));

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CriarAsync_DescricaoVaziaOuEmBranco_RetornaErroSemGravarNada(string descricao)
    {
        var contaId = ConfigurarConta();

        var (sucesso, resposta, erro) = await _service.CriarAsync(
            contaId,
            CriarRequest(descricao: descricao));

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    [Theory]
    [InlineData("SUGERIDO")]
    [InlineData("XYZ")]
    public async Task CriarAsync_StatusNaoPendenteNemPago_RetornaErroSemGravarNada(string status)
    {
        var contaId = ConfigurarConta();

        var (sucesso, resposta, erro) = await _service.CriarAsync(
            contaId,
            CriarRequest(status: status));

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    // ---------- CriarAsync: conta ----------

    [Fact]
    public async Task CriarAsync_ContaInexistente_RetornaErroSemGravarNada()
    {
        var contaId = Guid.NewGuid();
        _contaRepositoryMock
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync((Conta?)null);

        var (sucesso, resposta, erro) = await _service.CriarAsync(contaId, CriarRequest());

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    [Fact]
    public async Task CriarAsync_ContaInativa_RetornaErroSemGravarNada()
    {
        var contaId = ConfigurarConta(ativa: false);

        var (sucesso, resposta, erro) = await _service.CriarAsync(contaId, CriarRequest());

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    [Fact]
    public async Task CriarAsync_ContaOpenFinance_NaoManual_RetornaErroSemGravarNada()
    {
        var contaId = ConfigurarConta(origem: OrigemConta.OpenFinance);

        var (sucesso, resposta, erro) = await _service.CriarAsync(contaId, CriarRequest());

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    [Fact]
    public async Task CriarAsync_ContaCartao_RetornaErroSemGravarNada()
    {
        var contaId = ConfigurarConta(tipo: TipoConta.Cartao);

        var (sucesso, resposta, erro) = await _service.CriarAsync(contaId, CriarRequest());

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    // ---------- CriarAsync: categoria ----------

    [Fact]
    public async Task CriarAsync_CategoriaInexistente_RetornaErroSemGravarNada()
    {
        var contaId = ConfigurarConta();
        var categoriaId = Guid.NewGuid();
        _categoriaRepositoryMock
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync((Categoria?)null);

        var (sucesso, resposta, erro) = await _service.CriarAsync(
            contaId,
            CriarRequest(categoriaId: categoriaId));

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    [Fact]
    public async Task CriarAsync_CategoriaDeReceita_RetornaErroSemGravarNada()
    {
        var contaId = ConfigurarConta();
        var categoriaId = Guid.NewGuid();
        ConfigurarCategoria(categoriaId, TipoCategoria.Receita);

        var (sucesso, resposta, erro) = await _service.CriarAsync(
            contaId,
            CriarRequest(categoriaId: categoriaId));

        Assert.False(sucesso);
        Assert.Null(resposta);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarNadaGravado();
    }

    // ---------- CriarAsync: falha no fim da validacao nao grava nada ----------

    [Fact]
    public async Task CriarAsync_FalhaDeValidacaoDepoisDeOutrasChecagens_NaoGravaCompraNemParcelas()
    {
        // Conta valida e quantidade valida; so a categoria (checada por ultimo) falha.
        var contaId = ConfigurarConta();
        var categoriaId = Guid.NewGuid();
        _categoriaRepositoryMock
            .Setup(r => r.ObterPorId(categoriaId))
            .ReturnsAsync((Categoria?)null);

        var (sucesso, _, _) = await _service.CriarAsync(
            contaId,
            CriarRequest(valor: 300m, quantidade: 3, categoriaId: categoriaId));

        Assert.False(sucesso);
        Assert.Empty(_lancamentosAdicionados);
        Assert.Null(_compraAdicionada);
        VerificarNadaGravado();
    }

    // ---------- CancelarAsync ----------

    [Fact]
    public async Task CancelarAsync_TresPendentes_ExcluiTresEMantemZero()
    {
        var contaId = Guid.NewGuid();
        var compra = CriarCompra(contaId, new[]
        {
            StatusLancamento.Pendente,
            StatusLancamento.Pendente,
            StatusLancamento.Pendente
        });
        ConfigurarCompraExistente(compra);

        var (sucesso, excluidas, mantidas, erro) = await _service.CancelarAsync(contaId, compra.Id);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(3, excluidas);
        Assert.Equal(0, mantidas);
        VerificarRemovidos(3);
    }

    [Fact]
    public async Task CancelarAsync_UmPagoEDoisPendentes_ExcluiDoisEMantemAParcelaPagaIntacta()
    {
        var contaId = Guid.NewGuid();
        var compra = CriarCompra(contaId, new[]
        {
            StatusLancamento.Pago,
            StatusLancamento.Pendente,
            StatusLancamento.Pendente
        });
        ConfigurarCompraExistente(compra);

        var (sucesso, excluidas, mantidas, erro) = await _service.CancelarAsync(contaId, compra.Id);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(2, excluidas);
        Assert.Equal(1, mantidas);
        VerificarRemovidos(2);
        _lancamentoRepositoryMock.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Status == StatusLancamento.Pago)),
            Times.Never);
    }

    [Fact]
    public async Task CancelarAsync_TodasPagas_RecusaESemRemoverNada()
    {
        var contaId = Guid.NewGuid();
        var compra = CriarCompra(contaId, new[]
        {
            StatusLancamento.Pago,
            StatusLancamento.Pago,
            StatusLancamento.Pago
        });
        ConfigurarCompraExistente(compra);

        var (sucesso, excluidas, _, erro) = await _service.CancelarAsync(contaId, compra.Id);

        Assert.False(sucesso);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        Assert.Equal(0, excluidas);
        VerificarRemovidos(0);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task CancelarAsync_CompraInexistente_RetornaErroCompraNaoEncontrada()
    {
        var contaId = Guid.NewGuid();
        var compraId = Guid.NewGuid();
        _compraRepositoryMock
            .Setup(r => r.ObterPorId(compraId))
            .ReturnsAsync((CompraParcelada?)null);

        var (sucesso, _, _, erro) = await _service.CancelarAsync(contaId, compraId);

        Assert.False(sucesso);
        Assert.Equal(LancamentoParceladoService.ErroCompraNaoEncontrada, erro);
        VerificarRemovidos(0);
    }

    [Fact]
    public async Task CancelarAsync_CompraDeOutraConta_RetornaErroCompraNaoEncontradaESemRemoverNada()
    {
        var contaDona = Guid.NewGuid();
        var contaChamadora = Guid.NewGuid();
        var compra = CriarCompra(contaDona, new[]
        {
            StatusLancamento.Pendente,
            StatusLancamento.Pendente,
            StatusLancamento.Pendente
        });
        ConfigurarCompraExistente(compra);

        var (sucesso, _, _, erro) = await _service.CancelarAsync(contaChamadora, compra.Id);

        Assert.False(sucesso);
        Assert.Equal(LancamentoParceladoService.ErroCompraNaoEncontrada, erro);
        VerificarRemovidos(0);
    }

    [Fact]
    public async Task CancelarAsync_CompraDeCartaoComFaturaId_RecusaESemRemoverNada()
    {
        var contaId = Guid.NewGuid();
        var compra = CriarCompra(
            contaId,
            new[]
            {
                StatusLancamento.Pendente,
                StatusLancamento.Pendente,
                StatusLancamento.Pendente
            },
            faturaId: Guid.NewGuid());
        ConfigurarCompraExistente(compra);

        var (sucesso, _, _, erro) = await _service.CancelarAsync(contaId, compra.Id);

        Assert.False(sucesso);
        Assert.False(string.IsNullOrWhiteSpace(erro));
        VerificarRemovidos(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelarAsync_LinhaCompraParceladaSempreMantida_ComEsemPagoRestante(bool comPagoRestante)
    {
        // Historico: a linha compra_parcelada nunca e removida pelo cancelamento.
        // ICompraParceladaRepository nao expoe metodo de remocao, entao aqui so se
        // prova que a entidade segue intacta e nenhuma compra nova foi criada.
        var contaId = Guid.NewGuid();
        var statusParcelas = comPagoRestante
            ? new[] { StatusLancamento.Pago, StatusLancamento.Pendente, StatusLancamento.Pendente }
            : new[] { StatusLancamento.Pendente, StatusLancamento.Pendente, StatusLancamento.Pendente };
        var compra = CriarCompra(contaId, statusParcelas);
        ConfigurarCompraExistente(compra);

        var (sucesso, _, _, _) = await _service.CancelarAsync(contaId, compra.Id);

        Assert.True(sucesso);
        Assert.Equal(3, compra.QuantidadeParcelas);
        Assert.Equal(300m, compra.ValorTotal);
        _compraRepositoryMock.Verify(r => r.Adicionar(It.IsAny<CompraParcelada>()), Times.Never);
    }

    [Fact]
    public async Task CancelarAsync_SegundoCancelamento_RetornaErroSemRemoverDeNovo()
    {
        var contaId = Guid.NewGuid();
        var compra = CriarCompra(contaId, new[]
        {
            StatusLancamento.Pago,
            StatusLancamento.Pendente,
            StatusLancamento.Pendente
        });
        ConfigurarCompraExistente(compra);

        var primeiro = await _service.CancelarAsync(contaId, compra.Id);
        Assert.True(primeiro.Sucesso);
        VerificarRemovidos(2);

        // Estado persistido apos o primeiro cancelamento: so sobrou a PAGO.
        foreach (var pendente in compra.Lancamentos.Where(l => l.Status == StatusLancamento.Pendente).ToList())
        {
            compra.Lancamentos.Remove(pendente);
        }

        var segundo = await _service.CancelarAsync(contaId, compra.Id);

        Assert.False(segundo.Sucesso);
        Assert.False(string.IsNullOrWhiteSpace(segundo.Erro));
        VerificarRemovidos(2);
    }

    // ---------- Regressao item 18: parcela nao e editada/excluida pelo LancamentoManualService ----------

    private static Lancamento CriarParcelaDeCompraParcelada(Guid contaId, Guid lancamentoId)
    {
        return new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            Descricao = "Notebook",
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = new DateOnly(2026, 1, 15),
            Status = StatusLancamento.Pendente,
            Manual = true,
            CompraParceladaId = Guid.NewGuid(),
            ParcelaNumero = 1
        };
    }

    [Fact]
    public async Task RemoverAsync_ParcelaDeCompraParcelada_LancaLancamentoNaoAvulsoComOrigemCompraParcelada()
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        _lancamentoRepositoryMock
            .Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(CriarParcelaDeCompraParcelada(contaId, lancamentoId));
        var manualService = new LancamentoManualService(
            _lancamentoRepositoryMock.Object,
            _contaRepositoryMock.Object,
            _categoriaRepositoryMock.Object);

        var excecao = await Assert.ThrowsAsync<LancamentoNaoAvulsoException>(
            () => manualService.RemoverAsync(contaId, lancamentoId));

        Assert.Equal(OrigemVinculoLancamento.CompraParcelada, excecao.Origem);
        _lancamentoRepositoryMock.Verify(r => r.Remover(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }

    [Fact]
    public async Task EditarAsync_ParcelaDeCompraParcelada_LancaLancamentoNaoAvulsoComOrigemCompraParcelada()
    {
        var contaId = Guid.NewGuid();
        var lancamentoId = Guid.NewGuid();
        _lancamentoRepositoryMock
            .Setup(r => r.ObterPorId(lancamentoId))
            .ReturnsAsync(CriarParcelaDeCompraParcelada(contaId, lancamentoId));
        var manualService = new LancamentoManualService(
            _lancamentoRepositoryMock.Object,
            _contaRepositoryMock.Object,
            _categoriaRepositoryMock.Object);

        var request = new EditarLancamentoRequest
        {
            Descricao = "Notebook editado",
            Valor = 75m,
            Tipo = "DEBIT",
            Data = new DateOnly(2026, 1, 16),
            Status = "PAGO",
            ContaId = null
        };

        var excecao = await Assert.ThrowsAsync<LancamentoNaoAvulsoException>(
            () => manualService.EditarAsync(contaId, lancamentoId, request));

        Assert.Equal(OrigemVinculoLancamento.CompraParcelada, excecao.Origem);
        _lancamentoRepositoryMock.Verify(r => r.Atualizar(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Salvar(), Times.Never);
    }
}
