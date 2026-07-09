using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class TransferenciaServiceTests
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

    // ========== TESTE 1: Criar transferencia com sucesso — duas pernas ==========
    [Fact]
    public async Task CriarAsync_ContasManualValidas_CriaTransferenciaComDuasPernas()
    {
        using var context = CreateInMemoryContext();
        var contaOrigem = CriarContaManual(context, "Conta Origem");
        var contaDestino = CriarContaManual(context, "Conta Destino");
        var service = new TransferenciaService(context);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigem.Id,
            ContaDestinoId = contaDestino.Id,
            Valor = 100.00m,
            Data = new DateOnly(2026, 7, 8),
            Descricao = "Transferencia teste"
        };

        var (sucesso, transferencia, erro) = await service.CriarAsync(request);

        // Valida retorno da transferencia
        Assert.True(sucesso);
        Assert.Null(erro);
        Assert.NotNull(transferencia);
        Assert.Equal(contaOrigem.Id, transferencia.ContaOrigemId);
        Assert.Equal(contaDestino.Id, transferencia.ContaDestinoId);
        Assert.Equal(100.00m, transferencia.Valor);
        Assert.Equal("Transferencia teste", transferencia.Descricao);

        // Valida perna DEBIT (saida na origem)
        var lancamentoSaida = await context.Lancamentos
            .FirstOrDefaultAsync(l => l.ContaId == contaOrigem.Id && l.TransferenciaId == transferencia.Id);
        Assert.NotNull(lancamentoSaida);
        Assert.Equal(100.00m, lancamentoSaida.Valor);
        Assert.Equal(TipoLancamentoConstants.Debit, lancamentoSaida.Tipo);
        Assert.Equal(LancamentoStatusConstants.Pago, lancamentoSaida.Status);
        Assert.True(lancamentoSaida.Manual);
        Assert.Equal(transferencia.Id, lancamentoSaida.TransferenciaId);

        // Valida perna CREDIT (entrada no destino)
        var lancamentoEntrada = await context.Lancamentos
            .FirstOrDefaultAsync(l => l.ContaId == contaDestino.Id && l.TransferenciaId == transferencia.Id);
        Assert.NotNull(lancamentoEntrada);
        Assert.Equal(100.00m, lancamentoEntrada.Valor);
        Assert.Equal(TipoLancamentoConstants.Credit, lancamentoEntrada.Tipo);
        Assert.Equal(LancamentoStatusConstants.Pago, lancamentoEntrada.Status);
        Assert.True(lancamentoEntrada.Manual);
        Assert.Equal(transferencia.Id, lancamentoEntrada.TransferenciaId);

        // Valida que ambas pernas compartilham o mesmo TransferenciaId
        Assert.Equal(lancamentoSaida.TransferenciaId, lancamentoEntrada.TransferenciaId);
    }

    // ========== TESTE 2: Classificacao das duas pernas retorna Transferencia ==========
    [Fact]
    public async Task CriarAsync_DuasPernasClassificadas_AmbasRetornamTransferencia()
    {
        using var context = CreateInMemoryContext();
        var contaOrigem = CriarContaManual(context, "Origem");
        var contaDestino = CriarContaManual(context, "Destino");
        var service = new TransferenciaService(context);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigem.Id,
            ContaDestinoId = contaDestino.Id,
            Valor = 250.50m,
            Data = new DateOnly(2026, 7, 9),
            Descricao = "Classificacao teste"
        };

        var (sucesso, transferencia, _) = await service.CriarAsync(request);
        Assert.True(sucesso);
        Assert.NotNull(transferencia);

        var lancamentoSaida = await context.Lancamentos
            .FirstOrDefaultAsync(l => l.ContaId == contaOrigem.Id && l.TransferenciaId == transferencia.Id);
        var lancamentoEntrada = await context.Lancamentos
            .FirstOrDefaultAsync(l => l.ContaId == contaDestino.Id && l.TransferenciaId == transferencia.Id);

        Assert.NotNull(lancamentoSaida);
        Assert.NotNull(lancamentoEntrada);

        // Aplica classificacao nas duas pernas
        var classificacaoSaida = ClassificacaoLancamentoService.Classificar(lancamentoSaida);
        var classificacaoEntrada = ClassificacaoLancamentoService.Classificar(lancamentoEntrada);

        // Ambas devem ser Transferencia (nao Entrada/Saida)
        Assert.Equal(ClassificacaoLancamento.Transferencia, classificacaoSaida);
        Assert.Equal(ClassificacaoLancamento.Transferencia, classificacaoEntrada);
    }

    // ========== TESTE 3: Rejeita transferencia com conta origem OPEN_FINANCE ==========
    [Fact]
    public async Task CriarAsync_ContaOrigemOpenFinance_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var contaOrigemOF = CriarContaManual(context, "OF", OrigemConstants.OpenFinance);
        var contaDestino = CriarContaManual(context, "Manual", OrigemConstants.Manual);
        var service = new TransferenciaService(context);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigemOF.Id,
            ContaDestinoId = contaDestino.Id,
            Valor = 100.00m,
            Data = new DateOnly(2026, 7, 8),
            Descricao = "Deveria falhar"
        };

        var (sucesso, transferencia, erro) = await service.CriarAsync(request);

        Assert.False(sucesso);
        Assert.Null(transferencia);
        Assert.NotNull(erro);
        Assert.Contains("origem", erro.ToLower());
    }

    // ========== TESTE 4: Rejeita transferencia com conta destino OPEN_FINANCE ==========
    [Fact]
    public async Task CriarAsync_ContaDestinoOpenFinance_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var contaOrigem = CriarContaManual(context, "Manual", OrigemConstants.Manual);
        var contaDestinoOF = CriarContaManual(context, "OF", OrigemConstants.OpenFinance);
        var service = new TransferenciaService(context);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigem.Id,
            ContaDestinoId = contaDestinoOF.Id,
            Valor = 100.00m,
            Data = new DateOnly(2026, 7, 8),
            Descricao = "Deveria falhar"
        };

        var (sucesso, transferencia, erro) = await service.CriarAsync(request);

        Assert.False(sucesso);
        Assert.Null(transferencia);
        Assert.NotNull(erro);
        Assert.Contains("origem", erro.ToLower());
    }

    // ========== TESTE 5: Rejeita transferencia com origem == destino ==========
    [Fact]
    public async Task CriarAsync_ContaOrigemIgualDestino_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaManual(context, "Mesma Conta");
        var service = new TransferenciaService(context);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = conta.Id,
            ContaDestinoId = conta.Id,  // Mesmo ID
            Valor = 100.00m,
            Data = new DateOnly(2026, 7, 8),
            Descricao = "Deveria falhar"
        };

        var (sucesso, transferencia, erro) = await service.CriarAsync(request);

        Assert.False(sucesso);
        Assert.Null(transferencia);
        Assert.NotNull(erro);
        Assert.Contains("diferentes", erro.ToLower());
    }

    // ========== TESTE 6: Rejeita transferencia com valor <= 0 ==========
    [Fact]
    public async Task CriarAsync_ValorZero_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var contaOrigem = CriarContaManual(context, "Origem");
        var contaDestino = CriarContaManual(context, "Destino");
        var service = new TransferenciaService(context);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigem.Id,
            ContaDestinoId = contaDestino.Id,
            Valor = 0m,  // Borda: zero
            Data = new DateOnly(2026, 7, 8),
            Descricao = "Deveria falhar"
        };

        var (sucesso, transferencia, erro) = await service.CriarAsync(request);

        Assert.False(sucesso);
        Assert.Null(transferencia);
        Assert.NotNull(erro);
        Assert.Contains("maior que zero", erro.ToLower());
    }

    // ========== TESTE 7: Rejeita transferencia com valor negativo ==========
    [Fact]
    public async Task CriarAsync_ValorNegativo_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var contaOrigem = CriarContaManual(context, "Origem");
        var contaDestino = CriarContaManual(context, "Destino");
        var service = new TransferenciaService(context);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigem.Id,
            ContaDestinoId = contaDestino.Id,
            Valor = -50.00m,  // Borda: negativo
            Data = new DateOnly(2026, 7, 8),
            Descricao = "Deveria falhar"
        };

        var (sucesso, transferencia, erro) = await service.CriarAsync(request);

        Assert.False(sucesso);
        Assert.Null(transferencia);
        Assert.NotNull(erro);
        Assert.Contains("maior que zero", erro.ToLower());
    }

    // ========== TESTE 8: Rejeita transferencia com conta origem inexistente ==========
    [Fact]
    public async Task CriarAsync_ContaOrigemInexistente_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var contaDestino = CriarContaManual(context, "Destino");
        var service = new TransferenciaService(context);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = Guid.NewGuid(),  // ID que nao existe
            ContaDestinoId = contaDestino.Id,
            Valor = 100.00m,
            Data = new DateOnly(2026, 7, 8),
            Descricao = "Deveria falhar"
        };

        var (sucesso, transferencia, erro) = await service.CriarAsync(request);

        Assert.False(sucesso);
        Assert.Null(transferencia);
        Assert.NotNull(erro);
        Assert.Contains("origem", erro.ToLower());
    }

    // ========== TESTE 9: Rejeita transferencia com conta destino inexistente ==========
    [Fact]
    public async Task CriarAsync_ContaDestinoInexistente_Rejeita()
    {
        using var context = CreateInMemoryContext();
        var contaOrigem = CriarContaManual(context, "Origem");
        var service = new TransferenciaService(context);

        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigem.Id,
            ContaDestinoId = Guid.NewGuid(),  // ID que nao existe
            Valor = 100.00m,
            Data = new DateOnly(2026, 7, 8),
            Descricao = "Deveria falhar"
        };

        var (sucesso, transferencia, erro) = await service.CriarAsync(request);

        Assert.False(sucesso);
        Assert.Null(transferencia);
        Assert.NotNull(erro);
        Assert.Contains("destino", erro.ToLower());
    }

    // ========== TESTE 10: Criar transferencia com data especificada ==========
    [Fact]
    public async Task CriarAsync_ComDataEspecificada_UsaDataFornecida()
    {
        using var context = CreateInMemoryContext();
        var contaOrigem = CriarContaManual(context, "Origem");
        var contaDestino = CriarContaManual(context, "Destino");
        var service = new TransferenciaService(context);

        var dataEspecificada = new DateOnly(2026, 5, 15);
        var request = new CriarTransferenciaRequest
        {
            ContaOrigemId = contaOrigem.Id,
            ContaDestinoId = contaDestino.Id,
            Valor = 100.00m,
            Data = dataEspecificada,
            Descricao = "Com data especificada"
        };

        var (sucesso, transferencia, _) = await service.CriarAsync(request);

        Assert.True(sucesso);
        Assert.NotNull(transferencia);
        Assert.Equal(dataEspecificada, transferencia.Data);

        var lancamentos = await context.Lancamentos
            .Where(l => l.TransferenciaId == transferencia.Id)
            .ToListAsync();

        Assert.All(lancamentos, l => Assert.Equal(dataEspecificada, l.Data));
    }
}
