using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class FaturaCicloServiceTests
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

    // Cenario 1: Ciclo normal - dia_vencimento > dia_fechamento (mesmo mes)
    [Fact]
    public async Task ResolverFaturaAbertaVigente_CicloNormal_FechaEVenceNoMesmoMes()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new FaturaCicloService(context);

        var dataReferencia = new DateOnly(2026, 3, 5);
        var (fatura, rejeitada, motivo) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

        Assert.False(rejeitada);
        Assert.Null(motivo);
        Assert.NotNull(fatura);
        Assert.Equal(new DateOnly(2026, 3, 10), fatura.DataFechamento);
        Assert.Equal(new DateOnly(2026, 3, 20), fatura.DataVencimento);
        Assert.Equal(FaturaStatusConstants.Aberta, fatura.Status);
    }

    // Cenario 2: Vencimento no mes seguinte - dia_vencimento <= dia_fechamento
    [Fact]
    public async Task ResolverFaturaAbertaVigente_VencimentoProximo_FechaNoMesAtualVenceNoProximo()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 5);
        var service = new FaturaCicloService(context);

        var dataReferencia = new DateOnly(2026, 3, 5);
        var (fatura, rejeitada, motivo) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

        Assert.False(rejeitada);
        Assert.Null(motivo);
        Assert.NotNull(fatura);
        Assert.Equal(new DateOnly(2026, 3, 10), fatura.DataFechamento);
        Assert.Equal(new DateOnly(2026, 4, 5), fatura.DataVencimento);
    }

    // Cenario 3: dataReferencia igual ao dia_fechamento cai no PROXIMO ciclo
    [Fact]
    public async Task ResolverFaturaAbertaVigente_DataReferenciaIgualDiaFechamento_CaiNoProximoCiclo()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new FaturaCicloService(context);

        var dataReferencia = new DateOnly(2026, 3, 10);
        var (fatura, rejeitada, motivo) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

        Assert.False(rejeitada);
        Assert.Null(motivo);
        Assert.NotNull(fatura);
        Assert.Equal(new DateOnly(2026, 4, 10), fatura.DataFechamento);
        Assert.Equal(new DateOnly(2026, 4, 20), fatura.DataVencimento);
    }

    // Cenario 4: Virada de mes curto para longo (fevereiro com dia_fechamento=31)
    [Fact]
    public async Task ResolverFaturaAbertaVigente_FeveireiroParaMargo_AjustaParaUltimoDiaDisponivel()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 31, diaVencimento: 15);
        var service = new FaturaCicloService(context);

        var dataReferencia = new DateOnly(2026, 2, 28);
        var (fatura, rejeitada, motivo) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

        Assert.False(rejeitada);
        Assert.Null(motivo);
        Assert.NotNull(fatura);
        Assert.Equal(new DateOnly(2026, 3, 31), fatura.DataFechamento);
        Assert.Equal(new DateOnly(2026, 4, 15), fatura.DataVencimento);
    }

    // Cenario 5: Virada de ano
    [Fact]
    public async Task ResolverFaturaAbertaVigente_ViadaDeAno_FechaNoAnoSeguinte()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 15, diaVencimento: 25);
        var service = new FaturaCicloService(context);

        var dataReferencia = new DateOnly(2025, 12, 20);
        var (fatura, rejeitada, motivo) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

        Assert.False(rejeitada);
        Assert.Null(motivo);
        Assert.NotNull(fatura);
        Assert.Equal(new DateOnly(2026, 1, 15), fatura.DataFechamento);
        Assert.Equal(new DateOnly(2026, 1, 25), fatura.DataVencimento);
    }

    // Cenario 6: Reaproveitamento - chamar duas vezes retorna a mesma Fatura
    [Fact]
    public async Task ResolverFaturaAbertaVigente_ChamadaDupla_RetornaAMesmaFatura()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new FaturaCicloService(context);

        var dataReferencia = new DateOnly(2026, 3, 5);
        var (fatura1, rejeitada1, _) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);
        var (fatura2, rejeitada2, _) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

        Assert.False(rejeitada1);
        Assert.False(rejeitada2);
        Assert.NotNull(fatura1);
        Assert.NotNull(fatura2);
        Assert.Equal(fatura1.Id, fatura2.Id);
    }

    // Cenario 7a: Conta nao existe - lanca excecao
    [Fact]
    public async Task ResolverFaturaAbertaVigente_ContaNaoExiste_LancaInvalidOperationException()
    {
        using var context = CreateInMemoryContext();
        var service = new FaturaCicloService(context);

        var contaIdInvalida = Guid.NewGuid();
        var dataReferencia = new DateOnly(2026, 3, 5);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResolverFaturaAbertaVigenteAsync(contaIdInvalida, dataReferencia));

        Assert.Contains("nao encontrada", exception.Message);
    }

    // Cenario 7b: Conta nao eh do tipo CARTAO - lanca excecao
    [Fact]
    public async Task ResolverFaturaAbertaVigente_ContaNaoECartao_LancaInvalidOperationException()
    {
        using var context = CreateInMemoryContext();
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Banco",
            Origem = OrigemConstants.Manual,
            Tipo = TipoContaConstants.Banco,
            Ativa = true
        };
        context.Contas.Add(conta);
        context.SaveChanges();

        var service = new FaturaCicloService(context);
        var dataReferencia = new DateOnly(2026, 3, 5);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia));

        Assert.Contains("nao e do tipo CARTAO", exception.Message);
    }

    // Cenario 7c: Cartao sem dia_fechamento configurado - lanca excecao
    [Fact]
    public async Task ResolverFaturaAbertaVigente_CartaoSemDiaFechamento_LancaInvalidOperationException()
    {
        using var context = CreateInMemoryContext();
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Cartao Incompleto",
            Origem = OrigemConstants.Manual,
            Tipo = TipoContaConstants.Cartao,
            DiaFechamento = null,
            DiaVencimento = 20,
            Ativa = true
        };
        context.Contas.Add(conta);
        context.SaveChanges();

        var service = new FaturaCicloService(context);
        var dataReferencia = new DateOnly(2026, 3, 5);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia));

        Assert.Contains("nao tem dia_fechamento", exception.Message);
    }

    // Cenario 7d: Cartao sem dia_vencimento configurado - lanca excecao
    [Fact]
    public async Task ResolverFaturaAbertaVigente_CartaoSemDiaVencimento_LancaInvalidOperationException()
    {
        using var context = CreateInMemoryContext();
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Cartao Incompleto",
            Origem = OrigemConstants.Manual,
            Tipo = TipoContaConstants.Cartao,
            DiaFechamento = 10,
            DiaVencimento = null,
            Ativa = true
        };
        context.Contas.Add(conta);
        context.SaveChanges();

        var service = new FaturaCicloService(context);
        var dataReferencia = new DateOnly(2026, 3, 5);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia));

        Assert.Contains("ou dia_vencimento", exception.Message);
    }

    // Cenario adicional: Ano bissexto (fevereiro com dia_fechamento=29)
    [Fact]
    public async Task ResolverFaturaAbertaVigente_FevereiroBissexto_AjustaParaDia29()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 29, diaVencimento: 15);
        var service = new FaturaCicloService(context);

        var dataReferencia = new DateOnly(2024, 2, 15);
        var (fatura, rejeitada, motivo) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

        Assert.False(rejeitada);
        Assert.Null(motivo);
        Assert.NotNull(fatura);
        Assert.Equal(new DateOnly(2024, 2, 29), fatura.DataFechamento);
        Assert.Equal(new DateOnly(2024, 3, 15), fatura.DataVencimento);
    }

    // Cenario: Compra + Estorno cancelando exatamente (valorTotalFatura == 0 mas TEM lancamentos)
    // Esperado: fatura fecha como PAGA, nao FECHADA (bug "fatura zumbi")
    [Fact]
    public async Task FecharFaturaAberta_CompraEEstornoCancelando_FechaComoPaga()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new FaturaCicloService(context);

        // Passo 1: Resolver ciclo N (marco: 10/03 - 20/03)
        var dataReferenciaMarco = new DateOnly(2026, 3, 5);
        var (faturaMarco, rejeitadaMarco, _) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferenciaMarco);
        Assert.False(rejeitadaMarco);
        Assert.NotNull(faturaMarco);
        var idFaturaMarco = faturaMarco.Id;

        // Passo 2: Adicionar uma compra de R$100 e um estorno de -R$100
        var lancamentoCompra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            CategoriaId = null,
            Descricao = "Compra teste",
            Valor = 100m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 8),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            ContaFixaId = null,
            ConciliadoCom = null,
            TransferenciaId = null,
            FaturaId = idFaturaMarco
        };

        var lancamentoEstorno = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            CategoriaId = null,
            Descricao = "Estorno teste",
            Valor = -100m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 9),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            ContaFixaId = null,
            ConciliadoCom = null,
            TransferenciaId = null,
            FaturaId = idFaturaMarco
        };

        context.Lancamentos.Add(lancamentoCompra);
        context.Lancamentos.Add(lancamentoEstorno);
        context.SaveChanges();

        // Passo 3: Verificar que a fatura tem lancamentos mas valorTotal == 0
        faturaMarco = await context.Faturas
            .Include(f => f.Lancamentos)
            .Include(f => f.Transferencias)
            .FirstOrDefaultAsync(f => f.Id == idFaturaMarco);
        Assert.NotNull(faturaMarco);
        Assert.True(faturaMarco.Lancamentos.Any(), "Fatura deve ter lancamentos");
        var saldoMarco = FaturaSaldoCalculator.Calcular(faturaMarco);
        Assert.Equal(0m, saldoMarco.ValorTotal);
        Assert.Equal(0m, saldoMarco.ValorPendente);

        // Passo 4: Resolver ciclo N+1 (abril: 10/04 - 20/04) para fechar o ciclo N
        var dataReferenciaAbril = new DateOnly(2026, 4, 15);
        var (faturaAbril, rejeitadaAbril, _) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferenciaAbril);
        Assert.False(rejeitadaAbril);
        Assert.NotNull(faturaAbril);

        // Passo 5: Verificar que fatura de marco foi FECHADA -> PAGA (nao FECHADA)
        var faturaMarcoAposFechamento = await context.Faturas
            .Include(f => f.Lancamentos)
            .Include(f => f.Transferencias)
            .FirstOrDefaultAsync(f => f.Id == idFaturaMarco);
        Assert.NotNull(faturaMarcoAposFechamento);
        Assert.Equal(FaturaStatusConstants.Paga, faturaMarcoAposFechamento.Status);
    }
}
