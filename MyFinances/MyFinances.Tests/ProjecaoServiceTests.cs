using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class ProjecaoServiceTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private Conta CriarContaCartao(
        AppDbContext context,
        string nome = "Cartao Teste",
        int diaFechamento = 10,
        int diaVencimento = 20)
    {
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Origem = OrigemConstants.Manual,
            Tipo = TipoContaConstants.Cartao,
            DiaFechamento = diaFechamento,
            DiaVencimento = diaVencimento,
            Ativa = true
        };

        context.Contas.Add(conta);
        context.SaveChanges();
        return conta;
    }

    private Conta CriarContaBanco(AppDbContext context, string nome = "Banco Teste")
    {
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Origem = OrigemConstants.Manual,
            Tipo = TipoContaConstants.Banco,
            Ativa = true
        };

        context.Contas.Add(conta);
        context.SaveChanges();
        return conta;
    }

    private Fatura CriarFatura(
        AppDbContext context,
        Guid contaId,
        DateOnly dataFechamento,
        DateOnly dataVencimento,
        string status)
    {
        var conta = context.Contas.First(c => c.Id == contaId);

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta,
            DataFechamento = dataFechamento,
            DataVencimento = dataVencimento,
            Status = status
        };

        context.Faturas.Add(fatura);
        context.SaveChanges();
        return fatura;
    }

    private Lancamento CriarCompra(
        AppDbContext context,
        Guid contaId,
        Guid faturaId,
        decimal valor)
    {
        var conta = context.Contas.First(c => c.Id == contaId);

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta,
            Valor = valor,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = faturaId,
            TransferenciaId = null,
            PierreTxnId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };

        context.Lancamentos.Add(lancamento);
        context.SaveChanges();
        return lancamento;
    }

    private Lancamento CriarPagamento(
        AppDbContext context,
        Guid contaId,
        Guid transferenciaId,
        decimal valor)
    {
        var conta = context.Contas.First(c => c.Id == contaId);

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta,
            Valor = valor,
            Tipo = TipoLancamentoConstants.Credit,
            Data = new DateOnly(2026, 3, 20),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = null,
            TransferenciaId = transferenciaId,
            PierreTxnId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };

        context.Lancamentos.Add(lancamento);
        context.SaveChanges();
        return lancamento;
    }

    // Cenario 1: Conta CARTAO com fatura cujo DataVencimento cai no mes pedido,
    // saldo totalmente pendente (nenhum pagamento) -> TemFatura=true,
    // StatusPagamento=NAO_PAGO, Valor = total da fatura
    [Fact]
    public async Task ObterProjecaoCartaoAsync_FaturaTotalmentePendente_RetornaProjecaoComStatusNaoPago()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var fatura = CriarFatura(
            context,
            conta.Id,
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 20),
            FaturaStatusConstants.Aberta);

        CriarCompra(context, conta.Id, fatura.Id, 100m);
        CriarCompra(context, conta.Id, fatura.Id, 50m);

        var service = new ProjecaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, projecao, erro) = await service.ObterProjecaoCartaoAsync(conta.Id, 3, 2026);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(projecao);
        Assert.True(projecao.TemFatura);
        Assert.Equal(conta.Id, projecao.ContaId);
        Assert.Equal(fatura.Id, projecao.FaturaId);
        Assert.Equal(StatusProjecaoConstants.NaoPago, projecao.StatusPagamento);
        Assert.Equal(150m, projecao.Valor); // total da fatura
    }

    // Cenario 2: Mesma situacao, mas com pagamento parcial ja feito -> Valor =
    // saldo PENDENTE restante (nao o total original)
    [Fact]
    public async Task ObterProjecaoCartaoAsync_FaturaComPagamentoParcial_RetornaValorPendente()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var contaOrigem = CriarContaBanco(context, "Banco Origem");
        var fatura = CriarFatura(
            context,
            conta.Id,
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 20),
            FaturaStatusConstants.Aberta);

        CriarCompra(context, conta.Id, fatura.Id, 100m);
        CriarCompra(context, conta.Id, fatura.Id, 50m);

        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            ContaOrigemId = contaOrigem.Id,
            ContaOrigem = contaOrigem,
            ContaDestinoId = conta.Id,
            ContaDestino = conta,
            FaturaId = fatura.Id,  // VINCULO com a fatura
            Data = new DateOnly(2026, 3, 15),
            Valor = 60m,
            Descricao = "Pagamento parcial fatura"
        };
        context.Transferencias.Add(transferencia);
        context.SaveChanges();

        CriarPagamento(context, conta.Id, transferencia.Id, 60m);

        var service = new ProjecaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, projecao, erro) = await service.ObterProjecaoCartaoAsync(conta.Id, 3, 2026);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(projecao);
        Assert.True(projecao.TemFatura);
        Assert.Equal(conta.Id, projecao.ContaId);
        Assert.Equal(fatura.Id, projecao.FaturaId);
        Assert.Equal(StatusProjecaoConstants.NaoPago, projecao.StatusPagamento);
        // Valor = saldo pendente = 150 - 60 = 90
        Assert.Equal(90m, projecao.Valor);
    }

    // Cenario 3: Fatura totalmente quitada (via pagamento, mesmo que Status formal
    // seja ABERTA ou FECHADA, nao necessariamente PAGA) -> StatusPagamento=PAGO,
    // Valor = total da fatura
    [Fact]
    public async Task ObterProjecaoCartaoAsync_FaturaTotalmentePaga_RetornaProjecaoComStatusPago()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var contaOrigem = CriarContaBanco(context, "Banco Origem");
        var fatura = CriarFatura(
            context,
            conta.Id,
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 20),
            FaturaStatusConstants.Aberta); // Note: status ainda e ABERTA, nao PAGA

        CriarCompra(context, conta.Id, fatura.Id, 100m);
        CriarCompra(context, conta.Id, fatura.Id, 50m);

        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            ContaOrigemId = contaOrigem.Id,
            ContaOrigem = contaOrigem,
            ContaDestinoId = conta.Id,
            ContaDestino = conta,
            FaturaId = fatura.Id,  // VINCULO com a fatura
            Data = new DateOnly(2026, 3, 15),
            Valor = 150m,
            Descricao = "Pagamento completo fatura"
        };
        context.Transferencias.Add(transferencia);
        context.SaveChanges();

        CriarPagamento(context, conta.Id, transferencia.Id, 150m);

        var service = new ProjecaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, projecao, erro) = await service.ObterProjecaoCartaoAsync(conta.Id, 3, 2026);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(projecao);
        Assert.True(projecao.TemFatura);
        Assert.Equal(conta.Id, projecao.ContaId);
        Assert.Equal(fatura.Id, projecao.FaturaId);
        Assert.Equal(StatusProjecaoConstants.Pago, projecao.StatusPagamento);
        Assert.Equal(150m, projecao.Valor); // total da fatura quitada
    }

    // Cenario 4: Conta CARTAO sem nenhuma fatura vencendo no mes pedido -> sucesso,
    // TemFatura=false, StatusPagamento=null, Valor=0 (nao erro)
    [Fact]
    public async Task ObterProjecaoCartaoAsync_SemFaturaNoMes_RetornaSucessoComTemFaturaFalso()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);

        // Criar fatura em mes diferente (fevereiro)
        var faturaOutroMes = CriarFatura(
            context,
            conta.Id,
            new DateOnly(2026, 2, 10),
            new DateOnly(2026, 2, 20),
            FaturaStatusConstants.Aberta);
        CriarCompra(context, conta.Id, faturaOutroMes.Id, 100m);

        var service = new ProjecaoService(context, new ValidacaoCartaoService(context));

        // Solicitar projecao para marco (mes 3)
        var (sucesso, projecao, erro) = await service.ObterProjecaoCartaoAsync(conta.Id, 3, 2026);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(projecao);
        Assert.False(projecao.TemFatura);
        Assert.Null(projecao.StatusPagamento);
        Assert.Equal(0m, projecao.Valor);
        Assert.Null(projecao.FaturaId);
    }

    // Cenario 5: Conta que nao existe -> rejeitado com erro claro
    [Fact]
    public async Task ObterProjecaoCartaoAsync_ContaNaoExiste_RetornaErro()
    {
        using var context = CreateInMemoryContext();
        var service = new ProjecaoService(context, new ValidacaoCartaoService(context));

        var contaIdInvalida = Guid.NewGuid();

        var (sucesso, projecao, erro) = await service.ObterProjecaoCartaoAsync(contaIdInvalida, 3, 2026);

        Assert.False(sucesso);
        Assert.Null(projecao);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrada", erro);
    }

    // Cenario 6: Conta que existe mas nao e CARTAO (ex: BANCO) -> rejeitado com erro
    // claro
    [Fact]
    public async Task ObterProjecaoCartaoAsync_ContaNaoECartao_RetornaErro()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context);
        var service = new ProjecaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, projecao, erro) = await service.ObterProjecaoCartaoAsync(contaBanco.Id, 3, 2026);

        Assert.False(sucesso);
        Assert.Null(projecao);
        Assert.NotNull(erro);
        Assert.Contains("nao e do tipo CARTAO", erro);
    }

    // Cenario 7: Compra individual (Lancamento com FaturaId setado) nunca aparece
    // isolada na resposta — so o agregado da fatura conta.
    // Teste: confirmar que resposta contem apenas agregado (FaturaId, Valor total,
    // StatusPagamento), sem desempacotar lancamentos individuais.
    [Fact]
    public async Task ObterProjecaoCartaoAsync_ComprasIndividuaisNaoAparemIsoladas_ApenasAgregadoDaFatura()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var fatura = CriarFatura(
            context,
            conta.Id,
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 20),
            FaturaStatusConstants.Aberta);

        CriarCompra(context, conta.Id, fatura.Id, 100m);
        CriarCompra(context, conta.Id, fatura.Id, 50m);
        CriarCompra(context, conta.Id, fatura.Id, 75m);

        var service = new ProjecaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, projecao, erro) = await service.ObterProjecaoCartaoAsync(conta.Id, 3, 2026);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(projecao);
        Assert.True(projecao.TemFatura);
        Assert.Equal(225m, projecao.Valor); // 100 + 50 + 75, agregado completo
        Assert.Equal(fatura.Id, projecao.FaturaId); // Uma unica fatura, nao lista
        // ProjecaoCartaoResponseDto nao lista lancamentos individuais, apenas
        // agregado com FaturaId, Valor, StatusPagamento
    }

    // Cenario 8: Mes com formato invalido. Este cenario e de validacao do controller,
    // nao da logica do service. O service recebe mes (int) e ano (int) ja validados.
    // Pula como nao aplicavel a teste unitario puro do service.
    // Comentario: teste de validacao de formato iria para teste de integracao do
    // controller com WebApplicationFactory, fora do escopo de mike (unitario puro).
}
