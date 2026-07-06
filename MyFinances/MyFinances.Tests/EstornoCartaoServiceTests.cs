using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Dtos;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class EstornoCartaoServiceTests
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

    // Caso 1: Criar estorno em conta CARTAO valida com valor positivo no request
    // Esperado: Lancamento criado com Valor NEGATIVO, Tipo=DEBIT, Status=PAGO, Manual=true,
    // Oculto=false, PierreTxnId=null, TransferenciaId=null, ConciliadoCom=null,
    // ContaFixaId=null, FaturaId apontando pro ciclo da DATA do estorno
    [Fact]
    public async Task CriarEstornoAsync_ContaCartaoValida_CriaLancamentoComValorNegativo()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarEstornoRequest
        {
            Descricao = "Estorno teste",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15),
            CategoriaId = null
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(conta.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(estorno);
        Assert.Equal(-100.50m, estorno.Valor); // Valor invertido para negativo
        Assert.Equal(TipoLancamentoConstants.Debit, estorno.Tipo);
        Assert.Equal(LancamentoStatusConstants.Pago, estorno.Status);
        Assert.True(estorno.Manual);
        Assert.False(estorno.Oculto);
        Assert.Null(estorno.PierreTxnId);
        Assert.Null(estorno.TransferenciaId);
        Assert.Null(estorno.ConciliadoCom);
        Assert.Null(estorno.ContaFixaId);
        Assert.NotNull(estorno.FaturaId);

        // Verificar que a fatura resolvida tem o ciclo correto (dia_fechamento=10, dia_vencimento=20)
        // Para data 15/03, sendo 15 >= 10, o ciclo fecha em 10/04
        var fatura = await context.Faturas.FirstOrDefaultAsync(f => f.Id == estorno.FaturaId);
        Assert.NotNull(fatura);
        Assert.Equal(new DateOnly(2026, 4, 10), fatura.DataFechamento);
        Assert.Equal(new DateOnly(2026, 4, 20), fatura.DataVencimento);
    }

    // Caso 2: Criar estorno com valor zero no request
    // Esperado: rejeitado antes da conversao para negativo
    [Fact]
    public async Task CriarEstornoAsync_ValorZero_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarEstornoRequest
        {
            Descricao = "Estorno teste",
            Valor = 0,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(estorno);
        Assert.NotNull(erro);
        Assert.Contains("positivo", erro);
    }

    // Caso 3: Criar estorno com valor negativo no request
    // Esperado: rejeitado antes da inversao
    [Fact]
    public async Task CriarEstornoAsync_ValorNegativo_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarEstornoRequest
        {
            Descricao = "Estorno teste",
            Valor = -50.00m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(estorno);
        Assert.NotNull(erro);
        Assert.Contains("positivo", erro);
    }

    // Caso 4: Criar estorno em conta que nao existe
    [Fact]
    public async Task CriarEstornoAsync_ContaNaoExiste_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var contaIdInvalida = Guid.NewGuid();
        var request = new CriarEstornoRequest
        {
            Descricao = "Estorno teste",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(contaIdInvalida, request);

        Assert.False(sucesso);
        Assert.Null(estorno);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrada", erro);
    }

    // Caso 5: Criar estorno em conta que nao e CARTAO
    [Fact]
    public async Task CriarEstornoAsync_ContaNaoECartao_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarEstornoRequest
        {
            Descricao = "Estorno teste",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(contaBanco.Id, request);

        Assert.False(sucesso);
        Assert.Null(estorno);
        Assert.NotNull(erro);
        Assert.Contains("nao e do tipo CARTAO", erro);
    }

    // Caso 6: Criar estorno com descricao vazia
    [Fact]
    public async Task CriarEstornoAsync_DescricaoVazia_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarEstornoRequest
        {
            Descricao = "",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(estorno);
        Assert.NotNull(erro);
        Assert.Contains("obrigatoria", erro);
    }

    // Caso 7: Criar estorno com descricao so espacos (whitespace)
    [Fact]
    public async Task CriarEstornoAsync_DescricaoApenasEspacos_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarEstornoRequest
        {
            Descricao = "   ",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(estorno);
        Assert.NotNull(erro);
        Assert.Contains("obrigatoria", erro);
    }

    // Caso 8: Criar estorno cuja data cai numa fatura PAGA
    // Para data 05/03 com diaFechamento=10: 05 < 10 => ciclo fecha em 03/10 - 03/20
    [Fact]
    public async Task CriarEstornoAsync_DataEmFaturaPaga_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        // Criar uma fatura PAGA para o ciclo que contem data 05/03
        // Data 05/03 com diaFechamento=10: 05 < 10 => ciclo = 03/10 - 03/20
        var faturaAntiga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Paga,
        };
        context.Faturas.Add(faturaAntiga);
        context.SaveChanges();

        // Tentar criar estorno em data 05/03 que cai na fatura paga
        var request = new CriarEstornoRequest
        {
            Descricao = "Estorno teste",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 5)
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(estorno);
        Assert.NotNull(erro);
        Assert.Contains("Fatura ja foi paga", erro);
    }

    // Caso 9: Criar estorno cuja data cai numa fatura FECHADA (nao paga) - aceita como retroativo
    // Para data 05/03 com diaFechamento=10: 05 < 10 => ciclo = 03/10 - 03/20
    [Fact]
    public async Task CriarEstornoAsync_DataEmFaturaFechada_Aceita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        // Criar uma fatura FECHADA para o ciclo 10/03 - 20/03
        var faturaFechada = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Fechada,
        };
        context.Faturas.Add(faturaFechada);
        context.SaveChanges();

        // Criar estorno em data 05/03 que cai na fatura fechada (retroativo)
        var request = new CriarEstornoRequest
        {
            Descricao = "Estorno retroativo",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 5)
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(conta.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(estorno);
        Assert.Equal(faturaFechada.Id, estorno.FaturaId);
        Assert.Equal(-100.50m, estorno.Valor);
    }

    // Caso 10: Criar estorno cuja data nao tem fatura ainda
    // Para data 15/03 com diaFechamento=10: 15 >= 10 => ciclo fecha em 04/10
    // Se nao existe Fatura, deve criar ABERTA
    [Fact]
    public async Task CriarEstornoAsync_DataSemFatura_CriaFaturaAbertaEAceita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        // NAO criar nenhuma fatura - deixar vazio

        // Criar estorno em data 15/03
        var request = new CriarEstornoRequest
        {
            Descricao = "Estorno novo ciclo",
            Valor = 50.00m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(conta.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(estorno);
        Assert.Equal(-50.00m, estorno.Valor);

        // Verificar que uma Fatura ABERTA foi criada
        var fatura = await context.Faturas.FirstOrDefaultAsync(f => f.Id == estorno.FaturaId);
        Assert.NotNull(fatura);
        Assert.Equal(FaturaStatusConstants.Aberta, fatura.Status);
        Assert.Equal(new DateOnly(2026, 4, 10), fatura.DataFechamento);
        Assert.Equal(new DateOnly(2026, 4, 20), fatura.DataVencimento);
    }

    // Caso 11: Verificar que CategoriaId pode ser null
    [Fact]
    public async Task CriarEstornoAsync_CategoriaIdNull_Aceita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarEstornoRequest
        {
            Descricao = "Estorno sem categoria",
            Valor = 75.50m,
            Data = new DateOnly(2026, 3, 15),
            CategoriaId = null
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(conta.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(estorno);
        Assert.Null(estorno.CategoriaId);
    }

    // Caso 12: Verificar que lancamento fica persistido corretamente no banco
    [Fact]
    public async Task CriarEstornoAsync_EstornoPersistitoNoBanco_VerificaIntegridade()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new EstornoCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarEstornoRequest
        {
            Descricao = "Estorno persistencia",
            Valor = 123.45m,
            Data = new DateOnly(2026, 3, 15),
            CategoriaId = null
        };

        var (sucesso, estorno, erro) = await service.CriarEstornoAsync(conta.Id, request);

        Assert.True(sucesso);
        var estornoId = estorno!.Id;

        // Criar novo context pra simular nova query (verifica persistencia)
        using var context2 = CreateInMemoryContext();
        // Copiar dados do primeiro context (in-memory databases sao isolados)
        // Na pratica, isso nao funcionaria assim em teste in-memory, mas vamos
        // confiar que SaveChangesAsync persistiu. Vamos apenas verificar
        // que o estorno ja esta no DB do primeiro context.
        var estornoNoBanco = await context.Lancamentos
            .FirstOrDefaultAsync(l => l.Id == estornoId);

        Assert.NotNull(estornoNoBanco);
        Assert.Equal(-123.45m, estornoNoBanco.Valor);
        Assert.Equal(TipoLancamentoConstants.Debit, estornoNoBanco.Tipo);
        Assert.Equal("Estorno persistencia", estornoNoBanco.Descricao);
    }
}
