using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class LancamentoOcultacaoServiceTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private Conta CriarContaManual(
        AppDbContext context,
        string nome = "Conta Teste",
        string origem = OrigemConstants.Manual)
    {
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Origem = origem,
            Tipo = TipoContaConstants.Banco,
            Ativa = true
        };

        context.Contas.Add(conta);
        context.SaveChanges();
        return conta;
    }

    private Lancamento CriarLancamento(
        AppDbContext context,
        Guid contaId,
        Conta? conta = null,
        bool manual = false,
        string tipo = TipoLancamentoConstants.Debit,
        decimal valor = 100.00m,
        string descricao = "Lancamento teste")
    {
        // Se nao passou Conta, buscar do contexto
        if (conta == null)
        {
            conta = context.Contas.FirstOrDefault(c => c.Id == contaId);
        }

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta!,
            Tipo = tipo,
            Status = LancamentoStatusConstants.Pago,
            Valor = valor,
            Descricao = descricao,
            Data = new DateOnly(2026, 7, 1),
            Manual = manual,
            Oculto = false
        };

        context.Lancamentos.Add(lancamento);
        context.SaveChanges();
        return lancamento;
    }

    // ========== TESTE 1: Ocultar lancamento Open Finance (Manual=false) — Oculto vira true, linha persiste ==========
    [Fact]
    public async Task OcultarAsync_LancamentoOpenFinance_MarcaComoOcultoESalva()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context, origem: OrigemConstants.OpenFinance);
        var lancamento = CriarLancamento(context, conta.Id, manual: false);

        var service = new LancamentoOcultacaoService(context);

        var (sucesso, erro) = await service.OcultarAsync(lancamento.Id);

        Assert.True(sucesso);
        Assert.Null(erro);

        // Verificar que a linha persiste no banco
        var lancamentoBusca = await context.Lancamentos
            .FirstOrDefaultAsync(l => l.Id == lancamento.Id);

        Assert.NotNull(lancamentoBusca);
        Assert.True(lancamentoBusca.Oculto);
    }

    // ========== TESTE 2: Rejeitar ocultacao de lancamento Manual (Manual=true) ==========
    [Fact]
    public async Task OcultarAsync_LancamentoManual_RetornaErroENaoAltera()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var lancamento = CriarLancamento(context, conta.Id, manual: true);

        var service = new LancamentoOcultacaoService(context);

        var (sucesso, erro) = await service.OcultarAsync(lancamento.Id);

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("manual", erro, StringComparison.OrdinalIgnoreCase);

        // Verificar que Oculto permanece falso
        var lancamentoBusca = await context.Lancamentos
            .FirstOrDefaultAsync(l => l.Id == lancamento.Id);

        Assert.NotNull(lancamentoBusca);
        Assert.False(lancamentoBusca.Oculto);
    }

    // ========== TESTE 3: Nenhum outro campo alterado alem de Oculto ==========
    [Fact]
    public async Task OcultarAsync_ApenasOcultoEhAlterado_DemaisCamposPersistem()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context, origem: OrigemConstants.OpenFinance);
        var lancamento = CriarLancamento(
            context,
            conta.Id,
            manual: false,
            tipo: TipoLancamentoConstants.Credit,
            valor: 250.50m,
            descricao: "Importacao Open Finance"
        );

        // Capturar snapshot antes
        var tipoBefore = lancamento.Tipo;
        var statusBefore = lancamento.Status;
        var valorBefore = lancamento.Valor;
        var descricaoBefore = lancamento.Descricao;
        var dataBefore = lancamento.Data;
        var contaIdBefore = lancamento.ContaId;
        var manualBefore = lancamento.Manual;
        var ocultoBefore = lancamento.Oculto;

        var service = new LancamentoOcultacaoService(context);
        var (sucesso, _) = await service.OcultarAsync(lancamento.Id);

        Assert.True(sucesso);

        var lancamentoAfter = await context.Lancamentos
            .FirstOrDefaultAsync(l => l.Id == lancamento.Id);

        // Verificar que apenas Oculto mudou
        Assert.Equal(tipoBefore, lancamentoAfter.Tipo);
        Assert.Equal(statusBefore, lancamentoAfter.Status);
        Assert.Equal(valorBefore, lancamentoAfter.Valor);
        Assert.Equal(descricaoBefore, lancamentoAfter.Descricao);
        Assert.Equal(dataBefore, lancamentoAfter.Data);
        Assert.Equal(contaIdBefore, lancamentoAfter.ContaId);
        Assert.Equal(manualBefore, lancamentoAfter.Manual);
        Assert.NotEqual(ocultoBefore, lancamentoAfter.Oculto);
        Assert.True(lancamentoAfter.Oculto);
    }

    // ========== TESTE 4: Lancamento inexistente — retorna erro apropriado ==========
    [Fact]
    public async Task OcultarAsync_LancamentoInexistente_RetornaErro()
    {
        using var context = CreateInMemoryContext();
        var service = new LancamentoOcultacaoService(context);

        var idInexistente = Guid.NewGuid();
        var (sucesso, erro) = await service.OcultarAsync(idInexistente);

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("nao encontrado", erro, StringComparison.OrdinalIgnoreCase);
    }
}
