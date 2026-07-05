using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class SaldoCartaoServiceTests
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

    // Cenario 1: Conta CARTAO sem nenhum lancamento -> saldo = 0
    [Fact]
    public async Task CalcularSaldoAsync_ContaCartaoSemLancamento_RetornaSaldoZero()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new SaldoCartaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, saldo, erro) = await service.CalcularSaldoAsync(conta.Id);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(0m, saldo);
    }

    // Cenario 2: Conta CARTAO so com compras (100 + 50) -> saldo = 150
    [Fact]
    public async Task CalcularSaldoAsync_ContaCartaoSoComCompras_RetornaSaldoPositivo()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var fatura = CriarFatura(context, conta.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20), FaturaStatusConstants.Aberta);

        CriarCompra(context, conta.Id, fatura.Id, 100m);
        CriarCompra(context, conta.Id, fatura.Id, 50m);

        var service = new SaldoCartaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, saldo, erro) = await service.CalcularSaldoAsync(conta.Id);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(150m, saldo);
    }

    // Cenario 3: Conta CARTAO com compras + estorno (100 + 50 - 30) -> saldo = 120
    [Fact]
    public async Task CalcularSaldoAsync_ContaCartaoComComprasEEstorno_RetornaSaldoAposDesconto()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var fatura = CriarFatura(context, conta.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20), FaturaStatusConstants.Aberta);

        CriarCompra(context, conta.Id, fatura.Id, 100m);
        CriarCompra(context, conta.Id, fatura.Id, 50m);
        CriarCompra(context, conta.Id, fatura.Id, -30m); // estorno = valor negativo

        var service = new SaldoCartaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, saldo, erro) = await service.CalcularSaldoAsync(conta.Id);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.Equal(120m, saldo);
    }

    // Cenario 4: Conta CARTAO com compras + 1 pagamento parcial do historico
    // Pagamento = Lancamento CREDIT com TransferenciaId preenchido e FaturaId=null
    // Representa fatura ja paga anteriormente
    [Fact]
    public async Task CalcularSaldoAsync_ContaCartaoComComprasEPagamentoHistorico_RetornaSaldoAposPagamento()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var contaOrigem = CriarContaBanco(context, "Banco Origem");
        var fatura = CriarFatura(context, conta.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20), FaturaStatusConstants.Aberta);

        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            ContaOrigemId = contaOrigem.Id,
            ContaOrigem = contaOrigem,
            ContaDestinoId = conta.Id,
            ContaDestino = conta,
            Data = new DateOnly(2026, 3, 25),
            Valor = 80m,
            Descricao = "Pagamento fatura"
        };
        context.Transferencias.Add(transferencia);
        context.SaveChanges();

        // Compras na fatura atual
        CriarCompra(context, conta.Id, fatura.Id, 100m);
        CriarCompra(context, conta.Id, fatura.Id, 50m);

        // Pagamento do historico (FaturaId=null, TransferenciaId preenchido)
        CriarPagamento(context, conta.Id, transferencia.Id, 80m);

        var service = new SaldoCartaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, saldo, erro) = await service.CalcularSaldoAsync(conta.Id);

        Assert.True(sucesso);
        Assert.Null(erro);
        // Saldo = (100 + 50) - 80 = 70
        Assert.Equal(70m, saldo);
    }

    // Cenario 5: Conta que nao existe -> rejeitado com erro claro
    [Fact]
    public async Task CalcularSaldoAsync_ContaNaoExiste_RetornaErro()
    {
        using var context = CreateInMemoryContext();
        var service = new SaldoCartaoService(context, new ValidacaoCartaoService(context));

        var contaIdInvalida = Guid.NewGuid();

        var (sucesso, saldo, erro) = await service.CalcularSaldoAsync(contaIdInvalida);

        Assert.False(sucesso);
        Assert.Equal(0m, saldo);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrada", erro);
    }

    // Cenario 6: Conta que existe mas NAO e tipo CARTAO (ex: BANCO) -> rejeitado com erro claro
    [Fact]
    public async Task CalcularSaldoAsync_ContaNaoECartao_RetornaErro()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context);
        var service = new SaldoCartaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, saldo, erro) = await service.CalcularSaldoAsync(contaBanco.Id);

        Assert.False(sucesso);
        Assert.Equal(0m, saldo);
        Assert.NotNull(erro);
        Assert.Contains("nao e do tipo CARTAO", erro);
    }

    // Cenario 7: Cenario combinado realista com 2 faturas historicas
    // Fatura 1 (PAGA): 80 + 60 (compras) - 50 (pagamento) = 90 saldo historico
    // Fatura 2 (ABERTA): 100 + 75 (compras) = 175 saldo vigente
    // Total: 90 + 175 = 265
    [Fact]
    public async Task CalcularSaldoAsync_DuasFaturasHistoricas_RetornaSaldoTotalCorreto()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var contaOrigem = CriarContaBanco(context, "Banco Origem Historico");

        // Fatura 1: PAGA com compras e pagamento
        var faturaHistorica = CriarFatura(context, conta.Id, new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 20), FaturaStatusConstants.Paga);
        CriarCompra(context, conta.Id, faturaHistorica.Id, 80m);
        CriarCompra(context, conta.Id, faturaHistorica.Id, 60m);

        // Criar transferencia pro pagamento historico
        var transferenciaHistorica = new Transferencia
        {
            Id = Guid.NewGuid(),
            ContaOrigemId = contaOrigem.Id,
            ContaOrigem = contaOrigem,
            ContaDestinoId = conta.Id,
            ContaDestino = conta,
            Data = new DateOnly(2026, 2, 25),
            Valor = 50m,
            Descricao = "Pagamento fatura historica"
        };
        context.Transferencias.Add(transferenciaHistorica);
        context.SaveChanges();

        // Pagamento do historico (representa fatura ja paga)
        CriarPagamento(context, conta.Id, transferenciaHistorica.Id, 50m);

        // Fatura 2: ABERTA com compras
        var faturaAberta = CriarFatura(context, conta.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20), FaturaStatusConstants.Aberta);
        CriarCompra(context, conta.Id, faturaAberta.Id, 100m);
        CriarCompra(context, conta.Id, faturaAberta.Id, 75m);

        var service = new SaldoCartaoService(context, new ValidacaoCartaoService(context));

        var (sucesso, saldo, erro) = await service.CalcularSaldoAsync(conta.Id);

        Assert.True(sucesso);
        Assert.Null(erro);
        // Saldo total = (80 + 60 - 50) + (100 + 75) = 90 + 175 = 265
        Assert.Equal(265m, saldo);
    }
}
