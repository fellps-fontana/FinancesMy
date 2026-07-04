using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Dtos;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class CompraCartaoServiceTests
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

    // Caso 1: Criar compra em conta CARTAO valida
    // Esperado: Lancamento criado com Tipo=DEBIT, Status=PAGO, Manual=true, Oculto=false,
    // PierreTxnId=null, TransferenciaId=null, ConciliadoCom=null, ContaFixaId=null,
    // e FaturaId apontando pra fatura do ciclo da DATA da compra (nao "hoje")
    [Fact]
    public async Task CriarCompraAsync_ContaCartaoValida_CriaLancamentoComPropriedadesCorretas()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new CompraCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarCompraRequest
        {
            Descricao = "Compra teste",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15),
            CategoriaId = null
        };

        var (sucesso, compra, erro) = await service.CriarCompraAsync(conta.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(compra);
        Assert.Equal(TipoLancamentoConstants.Debit, compra.Tipo);
        Assert.Equal(LancamentoStatusConstants.Pago, compra.Status);
        Assert.True(compra.Manual);
        Assert.False(compra.Oculto);
        Assert.Null(compra.PierreTxnId);
        Assert.Null(compra.TransferenciaId);
        Assert.Null(compra.ConciliadoCom);
        Assert.Null(compra.ContaFixaId);
        Assert.NotNull(compra.FaturaId);

        // Verificar que a fatura resolvida tem o ciclo correto (dia_fechamento=10, dia_vencimento=20)
        // Para data 15/03, sendo 15 >= 10, o ciclo fecha em 10/04
        var fatura = await context.Faturas.FirstOrDefaultAsync(f => f.Id == compra.FaturaId);
        Assert.NotNull(fatura);
        Assert.Equal(new DateOnly(2026, 4, 10), fatura.DataFechamento);
        Assert.Equal(new DateOnly(2026, 4, 20), fatura.DataVencimento);
    }

    // Caso 2: Criar compra em conta que nao existe
    [Fact]
    public async Task CriarCompraAsync_ContaNaoExiste_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var service = new CompraCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var contaIdInvalida = Guid.NewGuid();
        var request = new CriarCompraRequest
        {
            Descricao = "Compra teste",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compra, erro) = await service.CriarCompraAsync(contaIdInvalida, request);

        Assert.False(sucesso);
        Assert.Null(compra);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrada", erro);
    }

    // Caso 3: Criar compra em conta que nao e CARTAO
    [Fact]
    public async Task CriarCompraAsync_ContaNaoECartao_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context);
        var service = new CompraCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarCompraRequest
        {
            Descricao = "Compra teste",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compra, erro) = await service.CriarCompraAsync(contaBanco.Id, request);

        Assert.False(sucesso);
        Assert.Null(compra);
        Assert.NotNull(erro);
        Assert.Contains("nao e do tipo CARTAO", erro);
    }

    // Caso 4: Criar compra com descricao vazia
    [Fact]
    public async Task CriarCompraAsync_DescricaoVazia_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new CompraCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarCompraRequest
        {
            Descricao = "",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compra, erro) = await service.CriarCompraAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(compra);
        Assert.NotNull(erro);
        Assert.Contains("obrigatoria", erro);
    }

    // Caso 5: Criar compra com descricao so espacos (whitespace)
    [Fact]
    public async Task CriarCompraAsync_DescricaoApenasEspacos_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new CompraCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarCompraRequest
        {
            Descricao = "   ",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compra, erro) = await service.CriarCompraAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(compra);
        Assert.NotNull(erro);
        Assert.Contains("obrigatoria", erro);
    }

    // Caso 6: Criar compra com valor zero
    [Fact]
    public async Task CriarCompraAsync_ValorZero_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new CompraCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarCompraRequest
        {
            Descricao = "Compra teste",
            Valor = 0,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compra, erro) = await service.CriarCompraAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(compra);
        Assert.NotNull(erro);
        Assert.Contains("positivo", erro);
    }

    // Caso 7: Criar compra com valor negativo
    [Fact]
    public async Task CriarCompraAsync_ValorNegativo_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new CompraCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var request = new CriarCompraRequest
        {
            Descricao = "Compra teste",
            Valor = -50.00m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compra, erro) = await service.CriarCompraAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(compra);
        Assert.NotNull(erro);
        Assert.Contains("positivo", erro);
    }

    // Caso 8: Criar compra cujo data cai numa fatura PAGA
    // Para data 05/03 com diaFechamento=10: 05 < 10 => ciclo fecha em 03/10 - 03/20
    [Fact]
    public async Task CriarCompraAsync_DataEmFaturaPaga_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new CompraCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

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
            TransferenciaId = null
        };
        context.Faturas.Add(faturaAntiga);
        context.SaveChanges();

        // Tentar criar compra em data 05/03 que cai na fatura paga
        var request = new CriarCompraRequest
        {
            Descricao = "Compra teste",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 5)
        };

        var (sucesso, compra, erro) = await service.CriarCompraAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(compra);
        Assert.NotNull(erro);
        Assert.Contains("Fatura ja foi paga", erro);
    }

    // Caso 9: Criar compra cujo data cai numa fatura FECHADA (nao paga) - aceita como retroativo
    // Para data 05/03 com diaFechamento=10: 05 < 10 => ciclo = 03/10 - 03/20
    [Fact]
    public async Task CriarCompraAsync_DataEmFaturaFechada_Aceita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new CompraCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        // Criar uma fatura FECHADA para o ciclo 10/03 - 20/03
        var faturaFechada = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Fechada,
            TransferenciaId = null
        };
        context.Faturas.Add(faturaFechada);
        context.SaveChanges();

        // Criar compra em data 05/03 que cai na fatura fechada (retroativo)
        var request = new CriarCompraRequest
        {
            Descricao = "Compra retroativa",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 5)
        };

        var (sucesso, compra, erro) = await service.CriarCompraAsync(conta.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(compra);
        Assert.Equal(faturaFechada.Id, compra.FaturaId);
    }

    // Caso 10: Editar compra mudando so Valor quando fatura ATUALMENTE vinculada esta PAGA
    // ESTE E O BUG QUE O STYLE PEGOU - deve ser REJEITADO (teste de regressao)
    [Fact]
    public async Task EditarCompraAsync_MudandoValor_ComFaturaPaga_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var faturaCicloService = new FaturaCicloService(context);
        var validacaoCartaoService = new ValidacaoCartaoService(context);
        var service = new CompraCartaoService(context, faturaCicloService, validacaoCartaoService);

        // Criar fatura PAGA
        var faturaAntiga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Paga,
            TransferenciaId = null
        };
        context.Faturas.Add(faturaAntiga);
        context.SaveChanges();

        // Criar compra nessa fatura
        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Descricao = "Compra original",
            Valor = 100.00m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = faturaAntiga.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };
        context.Lancamentos.Add(compra);
        context.SaveChanges();

        // Tentar editar apenas o valor (sem mudar data)
        var request = new EditarCompraRequest
        {
            Descricao = "Compra original",
            Valor = 150.00m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compraEditada, erro) = await service.EditarCompraAsync(conta.Id, compra.Id, request);

        Assert.False(sucesso);
        Assert.Null(compraEditada);
        Assert.NotNull(erro);
        Assert.Contains("fatura ja paga", erro);
    }

    // Caso 11: Editar compra mudando so Descricao quando fatura ATUALMENTE vinculada esta PAGA
    [Fact]
    public async Task EditarCompraAsync_MudandoDescricao_ComFaturaPaga_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var faturaCicloService = new FaturaCicloService(context);
        var validacaoCartaoService = new ValidacaoCartaoService(context);
        var service = new CompraCartaoService(context, faturaCicloService, validacaoCartaoService);

        // Criar fatura PAGA
        var faturaAntiga = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Paga,
            TransferenciaId = null
        };
        context.Faturas.Add(faturaAntiga);
        context.SaveChanges();

        // Criar compra nessa fatura
        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Descricao = "Compra original",
            Valor = 100.00m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = faturaAntiga.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };
        context.Lancamentos.Add(compra);
        context.SaveChanges();

        // Tentar editar apenas a descricao (sem mudar data)
        var request = new EditarCompraRequest
        {
            Descricao = "Compra modificada",
            Valor = 100.00m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compraEditada, erro) = await service.EditarCompraAsync(conta.Id, compra.Id, request);

        Assert.False(sucesso);
        Assert.Null(compraEditada);
        Assert.NotNull(erro);
        Assert.Contains("fatura ja paga", erro);
    }

    // Caso 12: Editar compra mudando so Valor quando fatura ATUALMENTE vinculada NAO esta paga (ABERTA)
    [Fact]
    public async Task EditarCompraAsync_MudandoValor_ComFaturaAberta_Aceita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var faturaCicloService = new FaturaCicloService(context);
        var validacaoCartaoService = new ValidacaoCartaoService(context);
        var service = new CompraCartaoService(context, faturaCicloService, validacaoCartaoService);

        // Criar fatura ABERTA
        var faturaAberta = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Aberta,
            TransferenciaId = null
        };
        context.Faturas.Add(faturaAberta);
        context.SaveChanges();

        // Criar compra nessa fatura
        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Descricao = "Compra original",
            Valor = 100.00m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = faturaAberta.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };
        context.Lancamentos.Add(compra);
        context.SaveChanges();

        // Editar apenas o valor (sem mudar data)
        var request = new EditarCompraRequest
        {
            Descricao = "Compra original",
            Valor = 150.00m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compraEditada, erro) = await service.EditarCompraAsync(conta.Id, compra.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(compraEditada);
        Assert.Equal(150.00m, compraEditada.Valor);
        Assert.Equal(faturaAberta.Id, compraEditada.FaturaId);
    }

    // Caso 13: Editar compra mudando so Valor quando fatura ATUALMENTE vinculada NAO esta paga (FECHADA)
    [Fact]
    public async Task EditarCompraAsync_MudandoValor_ComFaturaFechada_Aceita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var faturaCicloService = new FaturaCicloService(context);
        var validacaoCartaoService = new ValidacaoCartaoService(context);
        var service = new CompraCartaoService(context, faturaCicloService, validacaoCartaoService);

        // Criar fatura FECHADA
        var faturaFechada = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Fechada,
            TransferenciaId = null
        };
        context.Faturas.Add(faturaFechada);
        context.SaveChanges();

        // Criar compra nessa fatura
        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Descricao = "Compra original",
            Valor = 100.00m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = faturaFechada.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };
        context.Lancamentos.Add(compra);
        context.SaveChanges();

        // Editar apenas o valor (sem mudar data)
        var request = new EditarCompraRequest
        {
            Descricao = "Compra original",
            Valor = 120.00m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compraEditada, erro) = await service.EditarCompraAsync(conta.Id, compra.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(compraEditada);
        Assert.Equal(120.00m, compraEditada.Valor);
        Assert.Equal(faturaFechada.Id, compraEditada.FaturaId);
    }

    // Caso 14: Editar compra mudando Data pra um ciclo cuja fatura esta PAGA
    // Compra em 05/03 (ciclo 03/10-03/20 ABERTA) => mover pra 15/04 (ciclo 05/10-05/20 PAGA)
    // Nota: Data 15/04 com diaFechamento=10: 15 >= 10 => proximo mes = 05, logo DataFechamento=05/10
    [Fact]
    public async Task EditarCompraAsync_MudandoData_ParaFaturaPaga_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var faturaCicloService = new FaturaCicloService(context);
        var validacaoCartaoService = new ValidacaoCartaoService(context);
        var service = new CompraCartaoService(context, faturaCicloService, validacaoCartaoService);

        // Criar fatura ABERTA para 05/03 (ciclo 03/10 - 03/20)
        var faturaAtual = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Aberta,
            TransferenciaId = null
        };
        context.Faturas.Add(faturaAtual);

        // Criar fatura PAGA para ciclo 05/10 - 05/20 (para data 15/04, ciclo eh maio)
        var faturaAlvo = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 5, 10),
            DataVencimento = new DateOnly(2026, 5, 20),
            Status = FaturaStatusConstants.Paga,
            TransferenciaId = null
        };
        context.Faturas.Add(faturaAlvo);
        context.SaveChanges();

        // Criar compra em data 05/03 (ciclo 03/10-03/20)
        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Descricao = "Compra original",
            Valor = 100.00m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 5),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = faturaAtual.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };
        context.Lancamentos.Add(compra);
        context.SaveChanges();

        // Tentar mover pra data 15/04 que cai em fatura PAGA (05/10-05/20)
        var request = new EditarCompraRequest
        {
            Descricao = "Compra original",
            Valor = 100.00m,
            Data = new DateOnly(2026, 4, 15)
        };

        var (sucesso, compraEditada, erro) = await service.EditarCompraAsync(conta.Id, compra.Id, request);

        Assert.False(sucesso);
        Assert.Null(compraEditada);
        Assert.NotNull(erro);
        Assert.Contains("Fatura ja foi paga", erro);
    }

    // Caso 15: Editar compra mudando Data pra um ciclo valido (fatura ABERTA)
    // Data 05/03 (ciclo 03/10-03/20) => data 15/04 (como 15>=10, ciclo vai pra 05/10-05/20)
    [Fact]
    public async Task EditarCompraAsync_MudandoData_ParaFaturaAberta_Aceita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var faturaCicloService = new FaturaCicloService(context);
        var validacaoCartaoService = new ValidacaoCartaoService(context);
        var service = new CompraCartaoService(context, faturaCicloService, validacaoCartaoService);

        // Criar fatura ABERTA primeira (03/10 - 03/20)
        var faturaMarco = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Aberta,
            TransferenciaId = null
        };
        context.Faturas.Add(faturaMarco);
        context.SaveChanges();

        // Criar compra em data 05/03 (ciclo 03/10-03/20)
        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Descricao = "Compra original",
            Valor = 100.00m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 5),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = faturaMarco.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };
        context.Lancamentos.Add(compra);
        context.SaveChanges();

        // Mover pra data 15/04 (como 15 >= 10, ciclo vai pra proximo mes = 05, fatura = 05/10 - 05/20)
        var request = new EditarCompraRequest
        {
            Descricao = "Compra original",
            Valor = 100.00m,
            Data = new DateOnly(2026, 4, 15)
        };

        var (sucesso, compraEditada, erro) = await service.EditarCompraAsync(conta.Id, compra.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(compraEditada);
        Assert.Equal(new DateOnly(2026, 4, 15), compraEditada.Data);

        // Verificar que FaturaId foi atualizado pra nova fatura de maio (05/10 - 05/20)
        var novaFatura = await context.Faturas.FirstOrDefaultAsync(f => f.Id == compraEditada.FaturaId);
        Assert.NotNull(novaFatura);
        Assert.Equal(new DateOnly(2026, 5, 10), novaFatura.DataFechamento);
        Assert.Equal(new DateOnly(2026, 5, 20), novaFatura.DataVencimento);
        Assert.NotEqual(faturaMarco.Id, compraEditada.FaturaId);
    }

    // Caso 16: Editar compra mudando Data pra um ciclo valido (fatura FECHADA)
    // Data 15/04 (ciclo 05/10-05/20) => data 05/03 (ciclo 03/10-03/20 FECHADA)
    [Fact]
    public async Task EditarCompraAsync_MudandoData_ParaFaturaFechada_Aceita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var faturaCicloService = new FaturaCicloService(context);
        var validacaoCartaoService = new ValidacaoCartaoService(context);
        var service = new CompraCartaoService(context, faturaCicloService, validacaoCartaoService);

        // Criar fatura ABERTA para 15/04 (ciclo 05/10 - 05/20, pois 15 >= 10)
        var faturaAberta = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 5, 10),
            DataVencimento = new DateOnly(2026, 5, 20),
            Status = FaturaStatusConstants.Aberta,
            TransferenciaId = null
        };
        context.Faturas.Add(faturaAberta);

        // Criar fatura FECHADA para 05/03 (ciclo 03/10 - 03/20, pois 05 < 10)
        var faturaFechada = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Fechada,
            TransferenciaId = null
        };
        context.Faturas.Add(faturaFechada);
        context.SaveChanges();

        // Criar compra em data 15/04 (ciclo 05/10-05/20)
        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Descricao = "Compra original",
            Valor = 100.00m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 4, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = faturaAberta.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };
        context.Lancamentos.Add(compra);
        context.SaveChanges();

        // Mover pra data 05/03 que cai em fatura fechada
        var request = new EditarCompraRequest
        {
            Descricao = "Compra original",
            Valor = 100.00m,
            Data = new DateOnly(2026, 3, 5)
        };

        var (sucesso, compraEditada, erro) = await service.EditarCompraAsync(conta.Id, compra.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(compraEditada);
        Assert.Equal(new DateOnly(2026, 3, 5), compraEditada.Data);
        Assert.Equal(faturaFechada.Id, compraEditada.FaturaId);
    }

    // Caso 17: Editar compra nao encontrada
    [Fact]
    public async Task EditarCompraAsync_CompraNaoEncontrada_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var service = new CompraCartaoService(context, new FaturaCicloService(context), new ValidacaoCartaoService(context));

        var compraIdInvalida = Guid.NewGuid();
        var request = new EditarCompraRequest
        {
            Descricao = "Compra teste",
            Valor = 100.50m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compra, erro) = await service.EditarCompraAsync(conta.Id, compraIdInvalida, request);

        Assert.False(sucesso);
        Assert.Null(compra);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrada", erro);
    }

    // Caso 18: Editar compra com validacao falha (descricao vazia)
    [Fact]
    public async Task EditarCompraAsync_DescricaoVazia_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var faturaCicloService = new FaturaCicloService(context);
        var validacaoCartaoService = new ValidacaoCartaoService(context);
        var service = new CompraCartaoService(context, faturaCicloService, validacaoCartaoService);

        // Criar fatura e compra
        var fatura = await faturaCicloService.ResolverFaturaAbertaVigenteAsync(conta.Id, new DateOnly(2026, 3, 15));

        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Descricao = "Compra original",
            Valor = 100.00m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = fatura.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };
        context.Lancamentos.Add(compra);
        context.SaveChanges();

        // Tentar editar com descricao vazia
        var request = new EditarCompraRequest
        {
            Descricao = "",
            Valor = 150.00m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compraEditada, erro) = await service.EditarCompraAsync(conta.Id, compra.Id, request);

        Assert.False(sucesso);
        Assert.Null(compraEditada);
        Assert.NotNull(erro);
        Assert.Contains("obrigatoria", erro);
    }

    // Caso 19: Editar compra com valor negativo
    [Fact]
    public async Task EditarCompraAsync_ValorNegativo_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var faturaCicloService = new FaturaCicloService(context);
        var validacaoCartaoService = new ValidacaoCartaoService(context);
        var service = new CompraCartaoService(context, faturaCicloService, validacaoCartaoService);

        // Criar fatura e compra
        var fatura = await faturaCicloService.ResolverFaturaAbertaVigenteAsync(conta.Id, new DateOnly(2026, 3, 15));

        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Descricao = "Compra original",
            Valor = 100.00m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = fatura.Id,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };
        context.Lancamentos.Add(compra);
        context.SaveChanges();

        // Tentar editar com valor negativo
        var request = new EditarCompraRequest
        {
            Descricao = "Compra original",
            Valor = -50.00m,
            Data = new DateOnly(2026, 3, 15)
        };

        var (sucesso, compraEditada, erro) = await service.EditarCompraAsync(conta.Id, compra.Id, request);

        Assert.False(sucesso);
        Assert.Null(compraEditada);
        Assert.NotNull(erro);
        Assert.Contains("positivo", erro);
    }
}
