using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class LancamentoManualServiceTests
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
        string nome = "Conta Manual Teste",
        string origem = OrigemConstants.Manual,
        string? tipo = TipoContaConstants.Banco)
    {
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Origem = origem,
            Tipo = tipo,
            Ativa = true
        };

        context.Contas.Add(conta);
        context.SaveChanges();
        return conta;
    }

    // ========== TESTE 1: Criar lancamento em conta MANUAL — caso feliz ==========
    [Fact]
    public async Task CriarLancamentoAsync_ContaManualValida_CriaComSucesso()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var service = new LancamentoManualService(context);

        var request = new CriarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 150.00m,
            Descricao = "Despesa teste",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, lancamento, erro) = await service.CriarLancamentoAsync(conta.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(lancamento);
        Assert.Equal(conta.Id, lancamento.ContaId);
        Assert.Equal(TipoLancamentoConstants.Debit, lancamento.Tipo);
        Assert.Equal(LancamentoStatusConstants.Pendente, lancamento.Status);
        Assert.Equal(150.00m, lancamento.Valor);
        Assert.Equal("Despesa teste", lancamento.Descricao);
        Assert.True(lancamento.Manual);
        Assert.False(lancamento.Oculto);
        Assert.Null(lancamento.PierreTxnId);
        Assert.Null(lancamento.TransferenciaId);
        Assert.Null(lancamento.FaturaId);
        Assert.Null(lancamento.ConciliadoCom);
    }

    // ========== TESTE 2: Rejeitar criacao em conta OPEN_FINANCE ==========
    [Fact]
    public async Task CriarLancamentoAsync_ContaOpenFinance_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context, origem: OrigemConstants.OpenFinance);
        var service = new LancamentoManualService(context);

        var request = new CriarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Credit,
            Status = LancamentoStatusConstants.Pago,
            Valor = 50.00m,
            Descricao = "Receita teste",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, lancamento, erro) = await service.CriarLancamentoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(lancamento);
        Assert.NotNull(erro);
        Assert.Contains("MANUAL", erro);
    }

    // ========== TESTE 3: Rejeitar Status=SUGERIDO na criacao ==========
    [Fact]
    public async Task CriarLancamentoAsync_StatusSugerido_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var service = new LancamentoManualService(context);

        var request = new CriarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Sugerido,
            Valor = 100.00m,
            Descricao = "Lancamento sugerido",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, lancamento, erro) = await service.CriarLancamentoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(lancamento);
        Assert.NotNull(erro);
        Assert.Contains("Status", erro);
    }

    // ========== TESTE 4: Rejeitar Tipo vazio na criacao ==========
    [Fact]
    public async Task CriarLancamentoAsync_TipoVazio_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var service = new LancamentoManualService(context);

        var request = new CriarLancamentoRequest
        {
            Tipo = string.Empty,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Lancamento sem tipo",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, lancamento, erro) = await service.CriarLancamentoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(lancamento);
        Assert.NotNull(erro);
        Assert.Contains("Tipo", erro);
    }

    // ========== TESTE 5: Rejeitar Tipo invalido na criacao ==========
    [Fact]
    public async Task CriarLancamentoAsync_TipoInvalido_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var service = new LancamentoManualService(context);

        var request = new CriarLancamentoRequest
        {
            Tipo = "INVALIDO",
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Lancamento com tipo invalido",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, lancamento, erro) = await service.CriarLancamentoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(lancamento);
        Assert.NotNull(erro);
        Assert.Contains("DEBIT", erro);
        Assert.Contains("CREDIT", erro);
    }

    // ========== TESTE 6: Rejeitar Valor negativo ou zero ==========
    [Fact]
    public async Task CriarLancamentoAsync_ValorNegativoOuZero_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var service = new LancamentoManualService(context);

        var request = new CriarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = -50.00m,
            Descricao = "Lancamento negativo",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, lancamento, erro) = await service.CriarLancamentoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(lancamento);
        Assert.NotNull(erro);
        Assert.Contains("maior que zero", erro);
    }

    // ========== TESTE 7: Rejeitar Descricao vazia ==========
    [Fact]
    public async Task CriarLancamentoAsync_DescricaoVazia_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var service = new LancamentoManualService(context);

        var request = new CriarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = string.Empty,
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, lancamento, erro) = await service.CriarLancamentoAsync(conta.Id, request);

        Assert.False(sucesso);
        Assert.Null(lancamento);
        Assert.NotNull(erro);
        Assert.Contains("Descricao", erro);
    }

    // ========== TESTE 8: Valor armazenado sempre como magnitude positiva ==========
    [Fact]
    public async Task CriarLancamentoAsync_ValorArmazenadoPositivo()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var service = new LancamentoManualService(context);

        var request = new CriarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 123.45m,
            Descricao = "Valor positivo",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, lancamento, _) = await service.CriarLancamentoAsync(conta.Id, request);

        Assert.True(sucesso);
        Assert.NotNull(lancamento);
        // Valor deve ser armazenado com magnitude positiva, nunca com sinal
        Assert.Equal(123.45m, lancamento.Valor);
        Assert.True(lancamento.Valor > 0);
    }

    // ========== TESTE 9: Editar lancamento em conta MANUAL — caso feliz ==========
    [Fact]
    public async Task EditarLancamentoAsync_ContaManualValida_EditaComSucesso()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Original",
            Data = new DateOnly(2026, 7, 8),
            Manual = true,
            Oculto = false
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var request = new EditarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Credit,
            Status = LancamentoStatusConstants.Pago,
            Valor = 200.00m,
            Descricao = "Editado",
            Data = new DateOnly(2026, 7, 9),
            CategoriaId = null
        };

        var (sucesso, editado, erro) = await service.EditarLancamentoAsync(conta.Id, lancamento.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(editado);
        Assert.Equal(conta.Id, editado.ContaId);
        Assert.Equal(TipoLancamentoConstants.Credit, editado.Tipo);
        Assert.Equal(LancamentoStatusConstants.Pago, editado.Status);
        Assert.Equal(200.00m, editado.Valor);
        Assert.Equal("Editado", editado.Descricao);
        Assert.Equal(new DateOnly(2026, 7, 9), editado.Data);
    }

    // ========== TESTE 10: Rejeitar edicao em conta OPEN_FINANCE ==========
    [Fact]
    public async Task EditarLancamentoAsync_ContaOpenFinance_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context, origem: OrigemConstants.OpenFinance);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Original",
            Data = new DateOnly(2026, 7, 8),
            Manual = false
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var request = new EditarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 150.00m,
            Descricao = "Tentativa de edicao",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, editado, erro) = await service.EditarLancamentoAsync(conta.Id, lancamento.Id, request);

        Assert.False(sucesso);
        Assert.Null(editado);
        Assert.NotNull(erro);
        Assert.Contains("MANUAL", erro);
    }

    // ========== TESTE 11: Rejeitar Status=SUGERIDO na edicao ==========
    [Fact]
    public async Task EditarLancamentoAsync_StatusSugerido_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Original",
            Data = new DateOnly(2026, 7, 8),
            Manual = true
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var request = new EditarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Sugerido,
            Valor = 100.00m,
            Descricao = "Original",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, editado, erro) = await service.EditarLancamentoAsync(conta.Id, lancamento.Id, request);

        Assert.False(sucesso);
        Assert.Null(editado);
        Assert.NotNull(erro);
        Assert.Contains("Status", erro);
    }

    // ========== TESTE 12: Listar lancamentos em conta MANUAL — caso feliz ==========
    [Fact]
    public async Task ListarLancamentosAsync_ContaManualValida_ListaComSucesso()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);

        var lancamento1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Lancamento 1",
            Data = new DateOnly(2026, 7, 8),
            Manual = true
        };

        var lancamento2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Credit,
            Status = LancamentoStatusConstants.Pago,
            Valor = 50.00m,
            Descricao = "Lancamento 2",
            Data = new DateOnly(2026, 7, 7),
            Manual = true
        };

        context.Lancamentos.Add(lancamento1);
        context.Lancamentos.Add(lancamento2);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var resultado = await service.ListarLancamentosAsync(conta.Id);

        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Count);
        // Deve estar ordenado por data descendente (mais recente primeiro)
        Assert.Equal("Lancamento 1", resultado[0].Descricao);
        Assert.Equal("Lancamento 2", resultado[1].Descricao);
    }

    // ========== TESTE 13: Listar lancamentos filtrados por status ==========
    [Fact]
    public async Task ListarLancamentosAsync_FiltradoPorStatus_ListaApenas()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);

        var lancamento1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Pendente",
            Data = new DateOnly(2026, 7, 8),
            Manual = true
        };

        var lancamento2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Credit,
            Status = LancamentoStatusConstants.Pago,
            Valor = 50.00m,
            Descricao = "Pago",
            Data = new DateOnly(2026, 7, 7),
            Manual = true
        };

        context.Lancamentos.Add(lancamento1);
        context.Lancamentos.Add(lancamento2);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var resultado = await service.ListarLancamentosAsync(conta.Id, LancamentoStatusConstants.Pago);

        Assert.NotNull(resultado);
        Assert.Single(resultado);
        Assert.Equal("Pago", resultado[0].Descricao);
    }

    // ========== TESTE 14: Exclusao de lancamento em conta MANUAL — caso feliz ==========
    [Fact]
    public async Task ExcluirLancamentoAsync_SemVinculos_ExcluiComSucesso()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "A ser excluido",
            Data = new DateOnly(2026, 7, 8),
            Manual = true,
            TransferenciaId = null,
            FaturaId = null,
            ConciliadoCom = null
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var (sucesso, erro) = await service.ExcluirLancamentoAsync(conta.Id, lancamento.Id);

        Assert.True(sucesso);
        Assert.Null(erro);

        // Verificar que foi removido do banco
        var verificacao = await context.Lancamentos.FirstOrDefaultAsync(l => l.Id == lancamento.Id);
        Assert.Null(verificacao);
    }

    // ========== TESTE 15: Rejeitar exclusao em conta OPEN_FINANCE ==========
    [Fact]
    public async Task ExcluirLancamentoAsync_ContaOpenFinance_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context, origem: OrigemConstants.OpenFinance);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Nao pode excluir",
            Data = new DateOnly(2026, 7, 8),
            Manual = false
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var (sucesso, erro) = await service.ExcluirLancamentoAsync(conta.Id, lancamento.Id);

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("MANUAL", erro);

        // Verificar que ainda esta no banco
        var verificacao = await context.Lancamentos.FirstOrDefaultAsync(l => l.Id == lancamento.Id);
        Assert.NotNull(verificacao);
    }

    // ========== TESTE 16: Bloqueio de exclusao com TransferenciaId preenchido ==========
    [Fact]
    public async Task ExcluirLancamentoAsync_ComTransferenciaId_BloqueiaExclusao()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Vinculado a transferencia",
            Data = new DateOnly(2026, 7, 8),
            Manual = true,
            TransferenciaId = Guid.NewGuid(),
            FaturaId = null,
            ConciliadoCom = null
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var (sucesso, erro) = await service.ExcluirLancamentoAsync(conta.Id, lancamento.Id);

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("transferencia", erro);

        // Verificar que ainda esta no banco
        var verificacao = await context.Lancamentos.FirstOrDefaultAsync(l => l.Id == lancamento.Id);
        Assert.NotNull(verificacao);
    }

    // ========== TESTE 17: Bloqueio de exclusao com FaturaId preenchido ==========
    [Fact]
    public async Task ExcluirLancamentoAsync_ComFaturaId_BloqueiaExclusao()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pago,
            Valor = 500.00m,
            Descricao = "Compra no cartao",
            Data = new DateOnly(2026, 7, 8),
            Manual = true,
            TransferenciaId = null,
            FaturaId = Guid.NewGuid(),
            ConciliadoCom = null
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var (sucesso, erro) = await service.ExcluirLancamentoAsync(conta.Id, lancamento.Id);

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("fatura", erro);

        // Verificar que ainda esta no banco
        var verificacao = await context.Lancamentos.FirstOrDefaultAsync(l => l.Id == lancamento.Id);
        Assert.NotNull(verificacao);
    }

    // ========== TESTE 18: Bloqueio de exclusao com ConciliadoCom preenchido ==========
    [Fact]
    public async Task ExcluirLancamentoAsync_ComConciliadoCom_BloqueiaExclusao()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pago,
            Valor = 250.00m,
            Descricao = "Conciliado",
            Data = new DateOnly(2026, 7, 8),
            Manual = true,
            TransferenciaId = null,
            FaturaId = null,
            ConciliadoCom = Guid.NewGuid()
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var (sucesso, erro) = await service.ExcluirLancamentoAsync(conta.Id, lancamento.Id);

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("conciliado", erro);

        // Verificar que ainda esta no banco
        var verificacao = await context.Lancamentos.FirstOrDefaultAsync(l => l.Id == lancamento.Id);
        Assert.NotNull(verificacao);
    }

    // ========== TESTE 19: Edicao com mudanca de tipo DEBIT -> CREDIT ==========
    [Fact]
    public async Task EditarLancamentoAsync_MudaTipoDebitParaCredit_Sucesso()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Gasto",
            Data = new DateOnly(2026, 7, 8),
            Manual = true
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var request = new EditarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Credit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Gasto",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, editado, erro) = await service.EditarLancamentoAsync(conta.Id, lancamento.Id, request);

        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(editado);
        Assert.Equal(TipoLancamentoConstants.Credit, editado.Tipo);
    }

    // ========== TESTE 20: Conta nao encontrada na criacao ==========
    [Fact]
    public async Task CriarLancamentoAsync_ContaNaoEncontrada_Erro()
    {
        using var context = CreateInMemoryContext();
        var service = new LancamentoManualService(context);
        var contaInexistenteId = Guid.NewGuid();

        var request = new CriarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Lancamento teste",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, lancamento, erro) = await service.CriarLancamentoAsync(contaInexistenteId, request);

        Assert.False(sucesso);
        Assert.Null(lancamento);
        Assert.NotNull(erro);
        Assert.Contains("Conta nao encontrada", erro);
    }

    // ========== TESTE 21: Lancamento nao encontrado na edicao ==========
    [Fact]
    public async Task EditarLancamentoAsync_LancamentoNaoEncontrado_Erro()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var service = new LancamentoManualService(context);
        var lancamentoInexistenteId = Guid.NewGuid();

        var request = new EditarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Lancamento teste",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        var (sucesso, editado, erro) = await service.EditarLancamentoAsync(conta.Id, lancamentoInexistenteId, request);

        Assert.False(sucesso);
        Assert.Null(editado);
        Assert.NotNull(erro);
        Assert.Contains("Lancamento nao encontrado", erro);
    }

    // ========== TESTE 22: Lancamento nao encontrado na exclusao ==========
    [Fact]
    public async Task ExcluirLancamentoAsync_LancamentoNaoEncontrado_Erro()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var service = new LancamentoManualService(context);
        var lancamentoInexistenteId = Guid.NewGuid();

        var (sucesso, erro) = await service.ExcluirLancamentoAsync(conta.Id, lancamentoInexistenteId);

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("Lancamento nao encontrado", erro);
    }

    // ========== TESTE 23: Listar lancamentos com nenhum resultado ==========
    [Fact]
    public async Task ListarLancamentosAsync_ContaSemLancamentos_RetornaListVazia()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context);
        var service = new LancamentoManualService(context);

        var resultado = await service.ListarLancamentosAsync(conta.Id);

        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }

    // ========== TESTE 24: Verificacao que lancamento manual nao pode mudar de conta na edicao ==========
    // Nota: atualmente o codigo NAO valida isso na edicao. Este teste documenta
    // que a regra "Edicao nao permite mover o lancamento pra outra conta" nao e
    // verificada no servico. Se o usuario tentar editar passando outro contaId,
    // o lancamento nao muda (porque o UpdateLancamento usa o contaId existente).
    // Mas nao ha bloqueio explicito. Este teste verifica o comportamento atual.
    [Fact]
    public async Task EditarLancamentoAsync_ContasIguais_ProtegeContaExistente()
    {
        using var context = CreateInMemoryContext();
        var conta1 = CriarContaManual(context, nome: "Conta 1");
        var conta2 = CriarContaManual(context, nome: "Conta 2");
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta1.Id,
            Conta = conta1,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Original em conta 1",
            Data = new DateOnly(2026, 7, 8),
            Manual = true
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var request = new EditarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pago,
            Valor = 200.00m,
            Descricao = "Editado",
            Data = new DateOnly(2026, 7, 9),
            CategoriaId = null
        };

        // Ao editar passando conta1, funciona normal
        var (sucesso, editado, erro) = await service.EditarLancamentoAsync(conta1.Id, lancamento.Id, request);
        Assert.True(sucesso);
        Assert.NotNull(editado);
        Assert.Equal(conta1.Id, editado.ContaId);

        // Verificar que lancamento continua em conta1 no banco
        var verificacao = await context.Lancamentos.FirstOrDefaultAsync(l => l.Id == lancamento.Id);
        Assert.NotNull(verificacao);
        Assert.Equal(conta1.Id, verificacao.ContaId);
    }

    // ========== TESTE 25: Edicao so encontra lancamento em sua conta correta ==========
    [Fact]
    public async Task EditarLancamentoAsync_LancamentoEmOutraConta_NaoEncontra()
    {
        using var context = CreateInMemoryContext();
        var conta1 = CriarContaManual(context, nome: "Conta 1");
        var conta2 = CriarContaManual(context, nome: "Conta 2");
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta1.Id,
            Conta = conta1,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Em conta 1",
            Data = new DateOnly(2026, 7, 8),
            Manual = true
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);
        var request = new EditarLancamentoRequest
        {
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Nao vai encontrar",
            Data = new DateOnly(2026, 7, 8),
            CategoriaId = null
        };

        // Tentar editar lancamento da conta1 passando conta2 - nao vai encontrar
        var (sucesso, editado, erro) = await service.EditarLancamentoAsync(conta2.Id, lancamento.Id, request);

        Assert.False(sucesso);
        Assert.Null(editado);
        Assert.NotNull(erro);
        Assert.Contains("Lancamento nao encontrado", erro);
    }

    // ========== TESTE 26: Exclusao so encontra lancamento em sua conta correta ==========
    [Fact]
    public async Task ExcluirLancamentoAsync_LancamentoEmOutraConta_NaoEncontra()
    {
        using var context = CreateInMemoryContext();
        var conta1 = CriarContaManual(context, nome: "Conta 1");
        var conta2 = CriarContaManual(context, nome: "Conta 2");
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta1.Id,
            Conta = conta1,
            Tipo = TipoLancamentoConstants.Debit,
            Status = LancamentoStatusConstants.Pendente,
            Valor = 100.00m,
            Descricao = "Em conta 1",
            Data = new DateOnly(2026, 7, 8),
            Manual = true
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new LancamentoManualService(context);

        // Tentar excluir lancamento da conta1 passando conta2 - nao vai encontrar
        var (sucesso, erro) = await service.ExcluirLancamentoAsync(conta2.Id, lancamento.Id);

        Assert.False(sucesso);
        Assert.NotNull(erro);
        Assert.Contains("Lancamento nao encontrado", erro);

        // Verificar que lancamento ainda esta em conta1
        var verificacao = await context.Lancamentos.FirstOrDefaultAsync(l => l.Id == lancamento.Id);
        Assert.NotNull(verificacao);
        Assert.Equal(conta1.Id, verificacao.ContaId);
    }
}
