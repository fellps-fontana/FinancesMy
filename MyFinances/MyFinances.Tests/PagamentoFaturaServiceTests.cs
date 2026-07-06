using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Dtos;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class PagamentoFaturaServiceTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
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

    private Fatura CriarFatura(
        AppDbContext context,
        Guid contaId,
        string status = FaturaStatusConstants.Fechada,
        DateOnly? dataFechamento = null,
        DateOnly? dataVencimento = null)
    {
        dataFechamento ??= new DateOnly(2026, 3, 10);
        dataVencimento ??= new DateOnly(2026, 3, 20);

        var conta = context.Contas.First(c => c.Id == contaId);

        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = dataFechamento.Value,
            DataVencimento = dataVencimento.Value,
            Status = status,
            Conta = conta
        };

        context.Faturas.Add(fatura);
        context.SaveChanges();
        return fatura;
    }

    private Lancamento CriarLancamento(
        AppDbContext context,
        Guid contaId,
        Guid faturaId,
        decimal valor,
        string tipo = TipoLancamentoConstants.Debit)
    {
        var conta = context.Contas.First(c => c.Id == contaId);

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            FaturaId = faturaId,
            Descricao = "Compra teste",
            Valor = valor,
            Tipo = tipo,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            ContaFixaId = null,
            ConciliadoCom = null,
            TransferenciaId = null,
            Conta = conta
        };

        context.Lancamentos.Add(lancamento);
        context.SaveChanges();
        return lancamento;
    }

    // Caso 1: Pagar fatura FECHADA valida com conta origem tipo BANCO -> sucesso
    // Confirmacao: Fatura.Status vira PAGA, Fatura.TransferenciaId preenchido,
    // dois Lancamentos criados (DEBIT na origem, CREDIT no cartao),
    // ambos com mesmo TransferenciaId, CategoriaId=null, FaturaId=null, Manual=true, Status=PAGO
    [Fact]
    public async Task PagarFaturaAsync_FaturaFechadaValidaComContaBanco_Sucesso()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context, "Banco Origem");
        var contaCartao = CriarContaCartao(context, "Cartao Credito");
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Fechada);

        // Criar um lancamento na fatura para ter valor > 0
        var lancamento = CriarLancamento(context, contaCartao.Id, fatura.Id, 100.00m);

        var service = new PagamentoFaturaService(context);
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 20),
            Valor = 100.00m
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(faturaRetorno);
        Assert.Equal(FaturaStatusConstants.Paga, faturaRetorno.Status);

        // Verificar que a transferencia foi criada
        var transferencia = await context.Transferencias.FirstOrDefaultAsync(t => t.FaturaId == faturaRetorno.Id);
        Assert.NotNull(transferencia);
        Assert.Equal(100.00m, transferencia.Valor);
        Assert.Equal(contaBanco.Id, transferencia.ContaOrigemId);
        Assert.Equal(contaCartao.Id, transferencia.ContaDestinoId);

        // Verificar que dois lancamentos foram criados com mesma TransferenciaId
        var lancamentos = await context.Lancamentos
            .Where(l => l.TransferenciaId == transferencia.Id)
            .ToListAsync();

        Assert.Equal(2, lancamentos.Count);

        // Um DEBIT na conta de origem
        var lancamentoDebito = lancamentos.FirstOrDefault(l => l.Tipo == TipoLancamentoConstants.Debit);
        Assert.NotNull(lancamentoDebito);
        Assert.Equal(contaBanco.Id, lancamentoDebito.ContaId);
        Assert.Equal(100.00m, lancamentoDebito.Valor);
        Assert.Null(lancamentoDebito.CategoriaId);
        Assert.Null(lancamentoDebito.FaturaId);
        Assert.True(lancamentoDebito.Manual);
        Assert.Equal(LancamentoStatusConstants.Pago, lancamentoDebito.Status);

        // Um CREDIT na conta do cartao
        var lancamentoCredito = lancamentos.FirstOrDefault(l => l.Tipo == TipoLancamentoConstants.Credit);
        Assert.NotNull(lancamentoCredito);
        Assert.Equal(contaCartao.Id, lancamentoCredito.ContaId);
        Assert.Equal(100.00m, lancamentoCredito.Valor);
        Assert.Null(lancamentoCredito.CategoriaId);
        Assert.Null(lancamentoCredito.FaturaId);
        Assert.True(lancamentoCredito.Manual);
        Assert.Equal(LancamentoStatusConstants.Pago, lancamentoCredito.Status);
    }

    // Caso 2: Valor do pagamento = soma correta dos Lancamentos vinculados à Fatura
    // Cenario: 2-3 compras + 1 estorno na mesma fatura
    [Fact]
    public async Task PagarFaturaAsync_ValorPagamentoSomaCorretaDosLancamentos_Sucesso()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context, "Banco Origem");
        var contaCartao = CriarContaCartao(context, "Cartao Credito");
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Fechada);

        // Criar 3 compras: 100 + 50 + 75
        CriarLancamento(context, contaCartao.Id, fatura.Id, 100.00m);
        CriarLancamento(context, contaCartao.Id, fatura.Id, 50.00m);
        CriarLancamento(context, contaCartao.Id, fatura.Id, 75.00m);

        // Criar 1 estorno: -30 (reduz o total)
        CriarLancamento(context, contaCartao.Id, fatura.Id, -30.00m);

        // Total esperado: 100 + 50 + 75 - 30 = 195
        var service = new PagamentoFaturaService(context);
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 25),
            Valor = 195.00m
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(faturaRetorno);

        // Verificar que a transferencia tem o valor correto (soma dos lancamentos)
        var transferencia = await context.Transferencias.FirstOrDefaultAsync(t => t.FaturaId == faturaRetorno.Id);
        Assert.NotNull(transferencia);
        Assert.Equal(195.00m, transferencia.Valor);

        // Verificar que os lancamentos de pagamento tem o valor correto
        var lancamentos = await context.Lancamentos
            .Where(l => l.TransferenciaId == transferencia.Id)
            .ToListAsync();

        Assert.All(lancamentos, l => Assert.Equal(195.00m, l.Valor));
    }

    // Caso 3: Pagar fatura ABERTA -> agora permitido (pagamento antecipado)
    [Fact]
    public async Task PagarFaturaAsync_FaturaAberta_PagamentoAntecipadoPermitido()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context);
        var contaCartao = CriarContaCartao(context);
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Aberta);
        CriarLancamento(context, contaCartao.Id, fatura.Id, 100.00m);

        var service = new PagamentoFaturaService(context);
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 20),
            Valor = 100.00m
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(faturaRetorno);
        Assert.Equal(FaturaStatusConstants.Aberta, faturaRetorno.Status);
    }

    // Caso 4: Pagar fatura ja PAGA -> rejeitado com mensagem clara
    [Fact]
    public async Task PagarFaturaAsync_FaturaPaga_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context);
        var contaCartao = CriarContaCartao(context);
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Paga);
        CriarLancamento(context, contaCartao.Id, fatura.Id, 100.00m);

        var service = new PagamentoFaturaService(context);
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 20),
            Valor = 100.00m
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.False(sucesso);
        Assert.Null(faturaRetorno);
        Assert.NotNull(erro);
        Assert.Contains("ja foi paga", erro);
    }

    // Caso 5: Pagar fatura que nao existe -> rejeitado
    [Fact]
    public async Task PagarFaturaAsync_FaturaNaoExiste_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context);

        var service = new PagamentoFaturaService(context);
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 20),
            Valor = 100.00m
        };

        var faturaIdInexistente = Guid.NewGuid();
        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(faturaIdInexistente, request);

        Assert.False(sucesso);
        Assert.Null(faturaRetorno);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrada", erro);
    }

    // Caso 6: Conta origem que nao existe -> rejeitado
    [Fact]
    public async Task PagarFaturaAsync_ContaOrigemNaoExiste_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaCartao = CriarContaCartao(context);
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Fechada);
        CriarLancamento(context, contaCartao.Id, fatura.Id, 100.00m);

        var service = new PagamentoFaturaService(context);
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = Guid.NewGuid(),
            Data = new DateOnly(2026, 3, 20)
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.False(sucesso);
        Assert.Null(faturaRetorno);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrada", erro);
    }

    // Caso 7: Conta origem que NAO e tipo BANCO (ex: CARTAO ou INVESTIMENTO) -> rejeitado
    [Fact]
    public async Task PagarFaturaAsync_ContaOrigemNaoBanco_CartaoRejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaCartaoOrigem = CriarContaCartao(context, "Cartao Origem");
        var contaCartaoDestino = CriarContaCartao(context, "Cartao Destino");
        var fatura = CriarFatura(context, contaCartaoDestino.Id, FaturaStatusConstants.Fechada);
        CriarLancamento(context, contaCartaoDestino.Id, fatura.Id, 100.00m);

        var service = new PagamentoFaturaService(context);
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaCartaoOrigem.Id,
            Data = new DateOnly(2026, 3, 20)
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.False(sucesso);
        Assert.Null(faturaRetorno);
        Assert.NotNull(erro);
        Assert.Contains("BANCO", erro);
    }

    // Caso 7b: Conta origem tipo INVESTIMENTO -> rejeitado
    [Fact]
    public async Task PagarFaturaAsync_ContaOrigemNaoBanco_InvestimentoRejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaInvestimento = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Investimento Teste",
            Origem = OrigemConstants.Manual,
            Tipo = TipoContaConstants.Investimento,
            Ativa = true
        };
        context.Contas.Add(contaInvestimento);
        context.SaveChanges();

        var contaCartao = CriarContaCartao(context);
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Fechada);
        CriarLancamento(context, contaCartao.Id, fatura.Id, 100.00m);

        var service = new PagamentoFaturaService(context);
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaInvestimento.Id,
            Data = new DateOnly(2026, 3, 20)
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.False(sucesso);
        Assert.Null(faturaRetorno);
        Assert.NotNull(erro);
        Assert.Contains("BANCO", erro);
    }

    // Caso 8: ContaOrigemId igual a fatura.ContaId (pagar com a propria conta do cartao)
    // -> rejeitado
    [Fact]
    public async Task PagarFaturaAsync_ContaOrigemIgualContaCartao_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaCartao = CriarContaCartao(context);
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Fechada);
        CriarLancamento(context, contaCartao.Id, fatura.Id, 100.00m);

        var service = new PagamentoFaturaService(context);
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaCartao.Id,
            Data = new DateOnly(2026, 3, 20)
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.False(sucesso);
        Assert.Null(faturaRetorno);
        Assert.NotNull(erro);
        Assert.Contains("propria conta", erro);
    }

    // Caso 9: Fatura sem nenhum lancamento (valorPagamento = 0)
    // -> rejeitado (guarda contra valor <= 0)
    [Fact]
    public async Task PagarFaturaAsync_FaturaSemLancamentos_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context);
        var contaCartao = CriarContaCartao(context);
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Fechada);
        // Nao criar nenhum lancamento na fatura

        var service = new PagamentoFaturaService(context);
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 20),
            Valor = 100.00m
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.False(sucesso);
        Assert.Null(faturaRetorno);
        Assert.NotNull(erro);
        Assert.Contains("lancamentos", erro);
    }

    // Caso 10: Fatura FECHADA com saldo 300 - Pagamento PARCIAL (100), depois segundo pagamento (200)
    // Esperado: primeiro pag nao muda status (FECHADA), segundo pag muda para PAGA
    // e tem 2 Transferencias vinculadas a fatura
    [Fact]
    public async Task PagarFaturaAsync_FaturaFechadaPagamentoParcialDepois100e200_DuasTransferenciasStatusMudaSoPrimeira()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context, "Banco Origem");
        var contaCartao = CriarContaCartao(context, "Cartao Credito");
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Fechada);

        // Criar lancamento de 300
        var lancamento = CriarLancamento(context, contaCartao.Id, fatura.Id, 300.00m);

        var service = new PagamentoFaturaService(context);

        // Primeiro pagamento: 100
        var request1 = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 20),
            Valor = 100.00m
        };

        var (sucesso1, faturaRetorno1, erro1) = await service.PagarFaturaAsync(fatura.Id, request1);

        Assert.True(sucesso1);
        Assert.Null(erro1);
        Assert.NotNull(faturaRetorno1);
        // Status deve continuar FECHADA apos pagamento parcial
        Assert.Equal(FaturaStatusConstants.Fechada, faturaRetorno1.Status);

        // Recarregar fatura para verificar saldo pendente
        var faturaAposFirstPag = await context.Faturas
            .Include(f => f.Lancamentos)
            .Include(f => f.Transferencias)
            .FirstOrDefaultAsync(f => f.Id == fatura.Id);

        var saldoAposPrimeiro = FaturaSaldoCalculator.Calcular(faturaAposFirstPag);
        Assert.Equal(300.00m, saldoAposPrimeiro.ValorTotal);
        Assert.Equal(100.00m, saldoAposPrimeiro.ValorPago);
        Assert.Equal(200.00m, saldoAposPrimeiro.ValorPendente);

        // Segundo pagamento: 200
        var request2 = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 21),
            Valor = 200.00m
        };

        var (sucesso2, faturaRetorno2, erro2) = await service.PagarFaturaAsync(fatura.Id, request2);

        Assert.True(sucesso2);
        Assert.Null(erro2);
        Assert.NotNull(faturaRetorno2);
        // Status deve mudar para PAGA apos quitacao total
        Assert.Equal(FaturaStatusConstants.Paga, faturaRetorno2.Status);

        // Recarregar fatura para verificar estado final
        var faturaAposFinalPag = await context.Faturas
            .Include(f => f.Lancamentos)
            .Include(f => f.Transferencias)
            .FirstOrDefaultAsync(f => f.Id == fatura.Id);

        var saldoFinal = FaturaSaldoCalculator.Calcular(faturaAposFinalPag);
        Assert.Equal(300.00m, saldoFinal.ValorTotal);
        Assert.Equal(300.00m, saldoFinal.ValorPago);
        Assert.Equal(0.00m, saldoFinal.ValorPendente);

        // Verificar que tem 2 Transferencias vinculadas
        Assert.Equal(2, faturaAposFinalPag.Transferencias.Count);

        // Verificar que cada transferencia tem seu proprio valor
        var transferencias = faturaAposFinalPag.Transferencias.OrderBy(t => t.Data).ToList();
        Assert.Equal(100.00m, transferencias[0].Valor);
        Assert.Equal(200.00m, transferencias[1].Valor);
    }

    // Caso 11: Fatura ABERTA com saldo 300 - Pagamento PARCIAL (100), depois (200)
    // Esperado: ambos os pagamentos nao mudam status (continua ABERTA)
    [Fact]
    public async Task PagarFaturaAsync_FaturaAbertaPagamentoParcialDepois100e200_StatusContinuaAberta()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context, "Banco Origem");
        var contaCartao = CriarContaCartao(context, "Cartao Credito");
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Aberta);

        // Criar lancamento de 300
        var lancamento = CriarLancamento(context, contaCartao.Id, fatura.Id, 300.00m);

        var service = new PagamentoFaturaService(context);

        // Primeiro pagamento: 100
        var request1 = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 20),
            Valor = 100.00m
        };

        var (sucesso1, faturaRetorno1, erro1) = await service.PagarFaturaAsync(fatura.Id, request1);

        Assert.True(sucesso1);
        Assert.Null(erro1);
        Assert.Equal(FaturaStatusConstants.Aberta, faturaRetorno1.Status);

        // Segundo pagamento: 200 (quitando totalmente)
        var request2 = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 21),
            Valor = 200.00m
        };

        var (sucesso2, faturaRetorno2, erro2) = await service.PagarFaturaAsync(fatura.Id, request2);

        Assert.True(sucesso2);
        Assert.Null(erro2);
        // Status continua ABERTA mesmo apos quitacao total (conforme regra revisada)
        Assert.Equal(FaturaStatusConstants.Aberta, faturaRetorno2.Status);

        // Recarregar para confirmar saldo zero
        var faturaFinal = await context.Faturas
            .Include(f => f.Lancamentos)
            .Include(f => f.Transferencias)
            .FirstOrDefaultAsync(f => f.Id == fatura.Id);

        var saldoFinal = FaturaSaldoCalculator.Calcular(faturaFinal);
        Assert.Equal(300.00m, saldoFinal.ValorTotal);
        Assert.Equal(300.00m, saldoFinal.ValorPago);
        Assert.Equal(0.00m, saldoFinal.ValorPendente);
    }

    // Caso 12: Overpayment - tentar pagar valor maior que saldo pendente
    // Esperado: rejeitado com mensagem clara, nenhuma Transferencia criada
    [Fact]
    public async Task PagarFaturaAsync_OverpaymentMaiorQueSaldo_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context, "Banco Origem");
        var contaCartao = CriarContaCartao(context, "Cartao Credito");
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Fechada);

        // Criar lancamento de 100
        var lancamento = CriarLancamento(context, contaCartao.Id, fatura.Id, 100.00m);

        var service = new PagamentoFaturaService(context);

        // Tentar pagar 150 (excede saldo de 100)
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 20),
            Valor = 150.00m
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.False(sucesso);
        Assert.Null(faturaRetorno);
        Assert.NotNull(erro);
        Assert.Contains("excede", erro);

        // Verificar que nenhuma Transferencia foi criada
        var transferenciasCount = await context.Transferencias
            .Where(t => t.FaturaId == fatura.Id)
            .CountAsync();

        Assert.Equal(0, transferenciasCount);
    }

    // Caso 13: Tentar pagar fatura ja totalmente quitada (saldoPendente == 0)
    // Cenario: fatura com compra de 100, ja paga 100, tentando pagar de novo
    // Esperado: rejeitado, nenhuma transferencia nova
    [Fact]
    public async Task PagarFaturaAsync_FaturaJaTotalmentePaga_Rejeitado()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context, "Banco Origem");
        var contaCartao = CriarContaCartao(context, "Cartao Credito");
        var fatura = CriarFatura(context, contaCartao.Id, FaturaStatusConstants.Fechada);

        // Criar lancamento de 100
        CriarLancamento(context, contaCartao.Id, fatura.Id, 100.00m);

        // Criar uma transferencia ja vinculada (primeiro pagamento de 100)
        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            Data = new DateOnly(2026, 3, 20),
            Valor = 100.00m,
            ContaOrigemId = contaBanco.Id,
            ContaDestinoId = contaCartao.Id,
            FaturaId = fatura.Id,
            Descricao = "Pagamento anterior",
            ContaOrigem = contaBanco,
            ContaDestino = contaCartao
        };

        context.Transferencias.Add(transferencia);
        context.SaveChanges();

        var service = new PagamentoFaturaService(context);

        // Tentar pagar novamente (saldo ja eh zero)
        var request = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 21),
            Valor = 50.00m
        };

        var (sucesso, faturaRetorno, erro) = await service.PagarFaturaAsync(fatura.Id, request);

        Assert.False(sucesso);
        Assert.Null(faturaRetorno);
        Assert.NotNull(erro);
        Assert.Contains("quitada", erro);

        // Verificar que so existe uma transferencia (a original)
        var transferenciasCount = await context.Transferencias
            .Where(t => t.FaturaId == fatura.Id)
            .CountAsync();

        Assert.Equal(1, transferenciasCount);
    }

    // Caso 14: Fluxo combinado - fatura ABERTA, paga parcialmente (nao quita),
    // depois ciclo fecha -> vira FECHADA (nao PAGA)
    [Fact]
    public async Task PagarFaturaAsync_FaturaAbertaPagaParcialmenteMaisCicloFecha_FechaComoFechada()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context, "Banco Origem");
        var contaCartao = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);

        // Criar fatura do ciclo marco (10/03 - 20/03) como ABERTA
        var faturaMarco = CriarFatura(
            context,
            contaCartao.Id,
            FaturaStatusConstants.Aberta,
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 20)
        );

        // Criar lancamento de 300
        CriarLancamento(context, contaCartao.Id, faturaMarco.Id, 300.00m);

        var service = new PagamentoFaturaService(context);

        // Pagamento parcial de 100 (deixa 200 pendentes)
        var requestPagParcial = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 15),
            Valor = 100.00m
        };

        var (sucessoPagParcial, _, _) = await service.PagarFaturaAsync(faturaMarco.Id, requestPagParcial);
        Assert.True(sucessoPagParcial);

        // Verificar que continua ABERTA apos pagamento parcial
        var faturaAposPagParcial = await context.Faturas
            .FirstOrDefaultAsync(f => f.Id == faturaMarco.Id);
        Assert.Equal(FaturaStatusConstants.Aberta, faturaAposPagParcial.Status);

        // Agora simular ciclo fechamento ao chamar ResolverFaturaAbertaVigente para novo ciclo
        var serviceCiclo = new FaturaCicloService(context);
        var dataReferenciaAbril = new DateOnly(2026, 4, 15);
        await serviceCiclo.ResolverFaturaAbertaVigenteAsync(contaCartao.Id, dataReferenciaAbril);

        // Recarregar fatura de marco e verificar que vira FECHADA (nao PAGA)
        // porque ainda tem saldo pendente de 200
        var faturaMarcoAposCicloFecha = await context.Faturas
            .Include(f => f.Lancamentos)
            .Include(f => f.Transferencias)
            .FirstOrDefaultAsync(f => f.Id == faturaMarco.Id);

        Assert.NotNull(faturaMarcoAposCicloFecha);
        Assert.Equal(FaturaStatusConstants.Fechada, faturaMarcoAposCicloFecha.Status);

        var saldoFinal = FaturaSaldoCalculator.Calcular(faturaMarcoAposCicloFecha);
        Assert.Equal(200.00m, saldoFinal.ValorPendente);
    }

    // Caso 15: Fluxo combinado - fatura ABERTA, paga INTEGRALMENTE (saldo = 0, status = ABERTA),
    // depois ciclo fecha -> vira PAGA direto (nao FECHADA)
    [Fact]
    public async Task PagarFaturaAsync_FaturaAbertaPagaIntegralmenteMaisCicloFecha_FechaComoPagaDireto()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context, "Banco Origem");
        var contaCartao = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);

        // Criar fatura do ciclo marco (10/03 - 20/03) como ABERTA
        var faturaMarco = CriarFatura(
            context,
            contaCartao.Id,
            FaturaStatusConstants.Aberta,
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 20)
        );

        // Criar lancamento de 300
        CriarLancamento(context, contaCartao.Id, faturaMarco.Id, 300.00m);

        var service = new PagamentoFaturaService(context);

        // Pagamento integral de 300
        var requestPagIntegral = new PagarFaturaRequest
        {
            ContaOrigemId = contaBanco.Id,
            Data = new DateOnly(2026, 3, 15),
            Valor = 300.00m
        };

        var (sucessoPagIntegral, _, _) = await service.PagarFaturaAsync(faturaMarco.Id, requestPagIntegral);
        Assert.True(sucessoPagIntegral);

        // Verificar que continua ABERTA apos pagamento integral (conforme regra revisada)
        var faturaAposPagIntegral = await context.Faturas
            .FirstOrDefaultAsync(f => f.Id == faturaMarco.Id);
        Assert.Equal(FaturaStatusConstants.Aberta, faturaAposPagIntegral.Status);

        // Verificar que saldo eh zero
        var faturaComRelacoes = await context.Faturas
            .Include(f => f.Lancamentos)
            .Include(f => f.Transferencias)
            .FirstOrDefaultAsync(f => f.Id == faturaMarco.Id);

        var saldoAposPag = FaturaSaldoCalculator.Calcular(faturaComRelacoes);
        Assert.Equal(0.00m, saldoAposPag.ValorPendente);

        // Agora simular ciclo fechamento
        var serviceCiclo = new FaturaCicloService(context);
        var dataReferenciaAbril = new DateOnly(2026, 4, 15);
        await serviceCiclo.ResolverFaturaAbertaVigenteAsync(contaCartao.Id, dataReferenciaAbril);

        // Recarregar fatura de marco e verificar que vira PAGA direto
        var faturaMarcoAposCicloFecha = await context.Faturas
            .Include(f => f.Lancamentos)
            .Include(f => f.Transferencias)
            .FirstOrDefaultAsync(f => f.Id == faturaMarco.Id);

        Assert.NotNull(faturaMarcoAposCicloFecha);
        Assert.Equal(FaturaStatusConstants.Paga, faturaMarcoAposCicloFecha.Status);
    }
}
