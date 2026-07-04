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
        var fatura = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

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
        var fatura = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

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
        var fatura = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

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
        var fatura = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

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
        var fatura = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

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
        var fatura1 = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);
        var fatura2 = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

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
        var fatura = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferencia);

        Assert.Equal(new DateOnly(2024, 2, 29), fatura.DataFechamento);
        Assert.Equal(new DateOnly(2024, 3, 15), fatura.DataVencimento);
    }
}
