using Moq;
using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

// Testes para EstornoCompraParceladaService (regra critica de estorno de
// compra parcelada, item 12 subsecao "Estorno de compra parcelada").
// Cobertura: (a) cancelamento de parcelas nao pagas, (b) estorno retroativo
// em fatura paga, (c) idempotencia, (e) validacao de compra inexistente/outra conta.
public class EstornoCompraParceladaServiceTests
{
    private readonly Mock<ICompraParceladaRepository> _compraParceladaRepositoryMock;
    private readonly Mock<ILancamentoRepository> _lancamentoRepositoryMock;
    private readonly Mock<IContaRepository> _contaRepositoryMock;
    private readonly ValidacaoCartaoService _validacaoCartaoService;
    private readonly EstornoCompraParceladaService _service;

    public EstornoCompraParceladaServiceTests()
    {
        _compraParceladaRepositoryMock = new Mock<ICompraParceladaRepository>();
        _lancamentoRepositoryMock = new Mock<ILancamentoRepository>();
        _contaRepositoryMock = new Mock<IContaRepository>();

        _validacaoCartaoService = new ValidacaoCartaoService(_contaRepositoryMock.Object);

        _service = new EstornoCompraParceladaService(
            _compraParceladaRepositoryMock.Object,
            _lancamentoRepositoryMock.Object,
            _validacaoCartaoService);

        // Setup padrao: qualquer conta nao especificada e uma conta cartao ativa
        _contaRepositoryMock
            .Setup(r => r.ObterPorId(It.IsAny<Guid>()))
            .Returns<Guid>(contaId => Task.FromResult<Conta?>(new Conta
            {
                Id = contaId,
                Tipo = TipoConta.Cartao,
                Ativa = true,
                Nome = "Cartao Teste"
            }));
    }

    private void MockarContaCartaoInativa(Guid contaId)
    {
        var conta = new Conta
        {
            Id = contaId,
            Tipo = TipoConta.Cartao,
            Ativa = false,
            Nome = "Cartao Inativo"
        };

        _contaRepositoryMock
            .Setup(r => r.ObterPorId(contaId))
            .ReturnsAsync(conta);
    }

    [Fact]
    public async Task EstornarCompraParcelada_ParcelasEmFaturasNaoPagasAberta_CancelaParcelasRemovendo()
    {
        // Arrange: compra parcelada com 3 parcelas, 2 em fatura ABERTA (nao pagas), 1 em fatura PAGA
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Notebook",
            ValorTotal = 300m,
            QuantidadeParcelas = 3,
            DataCompra = new DateOnly(2025, 1, 5),
            Lancamentos = new List<Lancamento>()
        };

        var faturaNaoPaga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Status = StatusFatura.Aberta
        };

        var faturaPaga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Status = StatusFatura.Paga
        };

        var lancamento1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = faturaNaoPaga.Id,
            Fatura = faturaNaoPaga,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        var lancamento2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 2,
            FaturaId = faturaNaoPaga.Id,
            Fatura = faturaNaoPaga,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        var lancamento3 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 3,
            FaturaId = faturaPaga.Id,
            Fatura = faturaPaga,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        compraParcelada.Lancamentos.Add(lancamento1);
        compraParcelada.Lancamentos.Add(lancamento2);
        compraParcelada.Lancamentos.Add(lancamento3);

        _compraParceladaRepositoryMock
            .Setup(r => r.ObterPorId(compraParceladaId))
            .ReturnsAsync(compraParcelada);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Arrependimento",
            Data = new DateOnly(2025, 2, 1)
        };

        // Act
        var (sucesso, canceladas, estornos, erro) = await _service.EstornarCompraParceladaAsync(
            contaId,
            compraParceladaId,
            request);

        // Assert
        Assert.True(sucesso, $"Erro ao estornar: {erro}");
        Assert.Null(erro);
        Assert.NotNull(canceladas);
        Assert.Equal(2, canceladas.Count);
        Assert.Contains(lancamento1, canceladas);
        Assert.Contains(lancamento2, canceladas);

        // Verifica que os 2 lancamentos nao-pagos foram removidos
        _lancamentoRepositoryMock.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == lancamento1.Id || l.Id == lancamento2.Id)),
            Times.Exactly(2));
    }

    [Fact]
    public async Task EstornarCompraParcelada_ParcelasEmFaturaPaga_GeraSomenteUmEstornoRetroativo()
    {
        // Arrange: compra parcelada com 3 parcelas todas na mesma fatura PAGA
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Smartphone",
            ValorTotal = 300m,
            QuantidadeParcelas = 3,
            DataCompra = new DateOnly(2025, 1, 5),
            Lancamentos = new List<Lancamento>()
        };

        var faturaPaga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Status = StatusFatura.Paga
        };

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = "Eletronico",
            Tipo = TipoCategoria.Despesa
        };

        var lancamento1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = faturaPaga.Id,
            Fatura = faturaPaga,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            CategoriaId = categoria.Id,
            Categoria = categoria,
            Descricao = "Smartphone 1/3"
        };

        var lancamento2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 2,
            FaturaId = faturaPaga.Id,
            Fatura = faturaPaga,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            CategoriaId = categoria.Id,
            Categoria = categoria,
            Descricao = "Smartphone 2/3"
        };

        var lancamento3 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 3,
            FaturaId = faturaPaga.Id,
            Fatura = faturaPaga,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            CategoriaId = categoria.Id,
            Categoria = categoria,
            Descricao = "Smartphone 3/3"
        };

        compraParcelada.Lancamentos.Add(lancamento1);
        compraParcelada.Lancamentos.Add(lancamento2);
        compraParcelada.Lancamentos.Add(lancamento3);

        _compraParceladaRepositoryMock
            .Setup(r => r.ObterPorId(compraParceladaId))
            .ReturnsAsync(compraParcelada);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Defeito no produto",
            Data = new DateOnly(2025, 2, 1)
        };

        // Act
        var (sucesso, canceladas, estornos, erro) = await _service.EstornarCompraParceladaAsync(
            contaId,
            compraParceladaId,
            request);

        // Assert
        Assert.True(sucesso, $"Erro ao estornar: {erro}");
        Assert.Null(erro);
        Assert.NotNull(estornos);
        Assert.Equal(3, estornos.Count);

        // Verifica que 3 lancamentos de estorno foram criados na mesma fatura paga
        foreach (var estorno in estornos)
        {
            Assert.Equal(TipoLancamento.Credit, estorno.Tipo);
            Assert.Equal(StatusLancamento.Pago, estorno.Status);
            Assert.Equal(faturaPaga.Id, estorno.FaturaId);
            Assert.Equal(StatusFatura.Paga, faturaPaga.Status); // fatura continua paga
        }

        // Verifica que cada estorno tem os mesmos valores das parcelas originais
        var estorno1 = estornos.Single(e => e.ParcelaNumero == 1);
        Assert.Equal(100m, estorno1.Valor);
        Assert.Equal(categoria.Id, estorno1.CategoriaId);
        Assert.Equal(compraParceladaId, estorno1.CompraParceladaId);

        var estorno2 = estornos.Single(e => e.ParcelaNumero == 2);
        Assert.Equal(100m, estorno2.Valor);
        Assert.Equal(categoria.Id, estorno2.CategoriaId);

        var estorno3 = estornos.Single(e => e.ParcelaNumero == 3);
        Assert.Equal(100m, estorno3.Valor);
        Assert.Equal(categoria.Id, estorno3.CategoriaId);

        // Verifica que 3 lancamentos foram adicionados ao repository
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Exactly(3));
    }

    [Fact]
    public async Task EstornarCompraParcelada_MisturadoAbertaEPaga_CancelaAbertasEEstornaAspagas()
    {
        // Arrange: 1 parcela em fatura ABERTA, 2 parcelas em fatura PAGA
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Monitor",
            ValorTotal = 300m,
            QuantidadeParcelas = 3,
            DataCompra = new DateOnly(2025, 1, 5),
            Lancamentos = new List<Lancamento>()
        };

        var faturaAberta = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Status = StatusFatura.Aberta
        };

        var faturaPaga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Status = StatusFatura.Paga
        };

        var lancamento1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = faturaAberta.Id,
            Fatura = faturaAberta,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        var lancamento2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 2,
            FaturaId = faturaPaga.Id,
            Fatura = faturaPaga,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        var lancamento3 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 3,
            FaturaId = faturaPaga.Id,
            Fatura = faturaPaga,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        compraParcelada.Lancamentos.Add(lancamento1);
        compraParcelada.Lancamentos.Add(lancamento2);
        compraParcelada.Lancamentos.Add(lancamento3);

        _compraParceladaRepositoryMock
            .Setup(r => r.ObterPorId(compraParceladaId))
            .ReturnsAsync(compraParcelada);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Defeito",
            Data = new DateOnly(2025, 2, 1)
        };

        // Act
        var (sucesso, canceladas, estornos, erro) = await _service.EstornarCompraParceladaAsync(
            contaId,
            compraParceladaId,
            request);

        // Assert
        Assert.True(sucesso, $"Erro ao estornar: {erro}");
        Assert.NotNull(canceladas);
        Assert.NotNull(estornos);
        Assert.Single(canceladas);
        Assert.Equal(lancamento1.Id, canceladas[0].Id);
        Assert.Equal(2, estornos.Count);

        // Verifica lancamento removido
        _lancamentoRepositoryMock.Verify(
            r => r.Remover(It.Is<Lancamento>(l => l.Id == lancamento1.Id)),
            Times.Once);

        // Verifica estornos criados
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EstornarCompraParcelada_Idempotente_EstornarDuasVezesNaoDuplica()
    {
        // Arrange: 1 parcela em fatura PAGA. Estornar duas vezes nao deve duplicar o estorno.
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Mouse",
            ValorTotal = 100m,
            QuantidadeParcelas = 1,
            DataCompra = new DateOnly(2025, 1, 5),
            Lancamentos = new List<Lancamento>()
        };

        var faturaPaga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Status = StatusFatura.Paga
        };

        var lancamentoOriginal = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = faturaPaga.Id,
            Fatura = faturaPaga,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        compraParcelada.Lancamentos.Add(lancamentoOriginal);

        _compraParceladaRepositoryMock
            .Setup(r => r.ObterPorId(compraParceladaId))
            .ReturnsAsync(compraParcelada);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Arrependimento",
            Data = new DateOnly(2025, 2, 1)
        };

        // Act: chamar 2x
        var (sucesso1, _, estornos1, _) = await _service.EstornarCompraParceladaAsync(contaId, compraParceladaId, request);
        var (sucesso2, _, estornos2, _) = await _service.EstornarCompraParceladaAsync(contaId, compraParceladaId, request);

        // Assert: segunda chamada nao cria duplicado (idempotencia)
        Assert.True(sucesso1);
        Assert.True(sucesso2);

        // A segunda chamada nao deve ter criado novo lancamento de estorno
        // porque ja existe um com mesmo CompraParceladaId e ParcelaNumero
        _lancamentoRepositoryMock.Verify(
            r => r.Adicionar(It.IsAny<Lancamento>()),
            Times.Once); // adicionado so na primeira chamada
    }

    [Fact]
    public async Task EstornarCompraParcelada_CompraInexistente_RetornaErroSemAlterar()
    {
        // Arrange: compra inexistente
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        _compraParceladaRepositoryMock
            .Setup(r => r.ObterPorId(compraParceladaId))
            .ReturnsAsync((CompraParcelada?)null);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Arrependimento",
            Data = new DateOnly(2025, 2, 1)
        };

        // Act
        var (sucesso, canceladas, estornos, erro) = await _service.EstornarCompraParceladaAsync(
            contaId,
            compraParceladaId,
            request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(canceladas);
        Assert.Null(estornos);

        // Nenhuma operacao de repository deve ter acontecido alem do ObterPorId
        _lancamentoRepositoryMock.Verify(r => r.Remover(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EstornarCompraParcelada_CompraDeOutraConta_RetornaErroSemAlterar()
    {
        // Arrange: compra pertence a outra conta
        var contaId = Guid.NewGuid();
        var outraContaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Teclado",
            ValorTotal = 100m,
            QuantidadeParcelas = 1,
            DataCompra = new DateOnly(2025, 1, 5),
            Lancamentos = new List<Lancamento>()
        };

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = outraContaId, // diferente da contaId
            Status = StatusFatura.Aberta
        };

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = outraContaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = fatura.Id,
            Fatura = fatura,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        compraParcelada.Lancamentos.Add(lancamento);

        _compraParceladaRepositoryMock
            .Setup(r => r.ObterPorId(compraParceladaId))
            .ReturnsAsync(compraParcelada);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Arrependimento",
            Data = new DateOnly(2025, 2, 1)
        };

        // Act
        var (sucesso, canceladas, estornos, erro) = await _service.EstornarCompraParceladaAsync(
            contaId, // contaId diferente
            compraParceladaId,
            request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Null(canceladas);
        Assert.Null(estornos);

        // Nenhuma operacao deve ter acontecido
        _lancamentoRepositoryMock.Verify(r => r.Remover(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EstornarCompraParcelada_ContaCartaoInativa_RetornaErroSemAlterar()
    {
        // Arrange: conta cartao inativa
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Produto",
            ValorTotal = 100m,
            QuantidadeParcelas = 1,
            DataCompra = new DateOnly(2025, 1, 5),
            Lancamentos = new List<Lancamento>()
        };

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Status = StatusFatura.Aberta
        };

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = fatura.Id,
            Fatura = fatura,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        compraParcelada.Lancamentos.Add(lancamento);

        _compraParceladaRepositoryMock
            .Setup(r => r.ObterPorId(compraParceladaId))
            .ReturnsAsync(compraParcelada);

        // Setup: conta cartao inativa
        MockarContaCartaoInativa(contaId);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Teste",
            Data = new DateOnly(2025, 2, 1)
        };

        // Act
        var (sucesso, canceladas, estornos, erro) = await _service.EstornarCompraParceladaAsync(
            contaId,
            compraParceladaId,
            request);

        // Assert
        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Equal("Conta inativa nao pode ser utilizada", erro);
        Assert.Null(canceladas);
        Assert.Null(estornos);

        // Nenhuma operacao deve ter acontecido
        _lancamentoRepositoryMock.Verify(r => r.Remover(It.IsAny<Lancamento>()), Times.Never);
        _lancamentoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Lancamento>()), Times.Never);
    }

    [Fact]
    public async Task EstornarCompraParcelada_FaturasAbertaEFechada_AmbosStatusCancelamParcelas()
    {
        // Arrange: 1 parcela em fatura ABERTA, 1 parcela em fatura FECHADA
        // (nao paga), ambas devem ser canceladas
        var contaId = Guid.NewGuid();
        var compraParceladaId = Guid.NewGuid();

        var compraParcelada = new CompraParcelada
        {
            Id = compraParceladaId,
            Descricao = "Headset",
            ValorTotal = 200m,
            QuantidadeParcelas = 2,
            DataCompra = new DateOnly(2025, 1, 5),
            Lancamentos = new List<Lancamento>()
        };

        var faturaAberta = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Status = StatusFatura.Aberta
        };

        var faturafechada = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Status = StatusFatura.Fechada
        };

        var lancamento1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 1,
            FaturaId = faturaAberta.Id,
            Fatura = faturaAberta,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        var lancamento2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CompraParceladaId = compraParceladaId,
            ParcelaNumero = 2,
            FaturaId = faturafechada.Id,
            Fatura = faturafechada,
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago
        };

        compraParcelada.Lancamentos.Add(lancamento1);
        compraParcelada.Lancamentos.Add(lancamento2);

        _compraParceladaRepositoryMock
            .Setup(r => r.ObterPorId(compraParceladaId))
            .ReturnsAsync(compraParcelada);

        var request = new EstornarCompraParceladaRequest
        {
            Motivo = "Defeito",
            Data = new DateOnly(2025, 2, 1)
        };

        // Act
        var (sucesso, canceladas, estornos, erro) = await _service.EstornarCompraParceladaAsync(
            contaId,
            compraParceladaId,
            request);

        // Assert
        Assert.True(sucesso, $"Erro ao estornar: {erro}");
        Assert.NotNull(canceladas);
        Assert.Equal(2, canceladas.Count);
        Assert.Contains(lancamento1, canceladas);
        Assert.Contains(lancamento2, canceladas);

        // Ambas foram removidas
        _lancamentoRepositoryMock.Verify(
            r => r.Remover(It.IsAny<Lancamento>()),
            Times.Exactly(2));
    }
}
