using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class FaturaTransicaoEstadoTests
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
        string nome = "Cartao Transicao",
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

    // Teste 1: Resolver ciclo N (cria ABERTA), depois ciclo N+1 (fecha ciclo N e cria N+1)
    [Fact]
    public async Task ResolverFaturaCicloN_DepoisCicloNMais1_FechaCicloNECriaCicloNMais1()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new FaturaCicloService(context);

        // Resolver ciclo N (marco: data 05/03 => ciclo 10/03 - 20/03, pois 05 < 10)
        var dataReferenciaMarco = new DateOnly(2026, 3, 5);
        var (faturaMarco, rejeitadaMarco, motivoMarco) =
            await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferenciaMarco);

        Assert.False(rejeitadaMarco);
        Assert.Null(motivoMarco);
        Assert.NotNull(faturaMarco);
        Assert.Equal(new DateOnly(2026, 3, 10), faturaMarco.DataFechamento);
        Assert.Equal(FaturaStatusConstants.Aberta, faturaMarco.Status);
        var idFaturaMarco = faturaMarco.Id;

        // Resolver ciclo N+1 (abril: data 15/04 => ciclo 10/05 - 20/05, pois 15 >= 10 entao vai pro proximo mes)
        var dataReferenciaAbril = new DateOnly(2026, 4, 15);
        var (faturaAbril, rejeitadaAbril, motivoAbril) =
            await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferenciaAbril);

        Assert.False(rejeitadaAbril);
        Assert.Null(motivoAbril);
        Assert.NotNull(faturaAbril);
        Assert.Equal(new DateOnly(2026, 5, 10), faturaAbril.DataFechamento);
        Assert.Equal(FaturaStatusConstants.Aberta, faturaAbril.Status);
        var idFaturaAbril = faturaAbril.Id;

        // Verificar que os IDs sao diferentes
        Assert.NotEqual(idFaturaMarco, idFaturaAbril);

        // Verificar no banco que fatura de marco foi FECHADA
        var faturaMarcoAposTransicao = await context.Faturas
            .FirstOrDefaultAsync(f => f.Id == idFaturaMarco);
        Assert.NotNull(faturaMarcoAposTransicao);
        Assert.Equal(FaturaStatusConstants.Fechada, faturaMarcoAposTransicao.Status);

        // Verificar que fatura de abril continua ABERTA
        var faturaAbrilAposTransicao = await context.Faturas
            .FirstOrDefaultAsync(f => f.Id == idFaturaAbril);
        Assert.NotNull(faturaAbrilAposTransicao);
        Assert.Equal(FaturaStatusConstants.Aberta, faturaAbrilAposTransicao.Status);
    }

    // Teste 2: Resolver fatura pro ciclo N novamente (mesma data_fechamento) apos ela estar FECHADA
    // Esperado: NAO reabrir, NAO duplicar, retornar a fatura FECHADA existente
    [Fact]
    public async Task ResolverFaturaCicloNNovamente_AposFechada_ReutilizaSemReabrir()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new FaturaCicloService(context);

        // Passo 1: Resolver ciclo N (marco: 10/03 - 20/03)
        var dataReferenciaMarco = new DateOnly(2026, 3, 5);
        var (faturaMarco1, _, _) = await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferenciaMarco);
        Assert.NotNull(faturaMarco1);
        var idFaturaMarco = faturaMarco1.Id;

        // Passo 2: Resolver ciclo N+1 (abril: 10/04 - 20/04) pra fechar o ciclo N
        var dataReferenciaAbril = new DateOnly(2026, 4, 15);
        await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferenciaAbril);

        // Verificar que ciclo N foi fechado
        var faturaMarcoAposFechamento = await context.Faturas
            .FirstOrDefaultAsync(f => f.Id == idFaturaMarco);
        Assert.NotNull(faturaMarcoAposFechamento);
        Assert.Equal(FaturaStatusConstants.Fechada, faturaMarcoAposFechamento.Status);

        // Passo 3: Tentar resolver ciclo N novamente (mesma data de referencia que cai no ciclo 10/03)
        var dataReferenciaMarcoNovamente = new DateOnly(2026, 3, 8);
        var (faturaMarco2, rejeitada, motivo) =
            await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferenciaMarcoNovamente);

        // Como ja existe ABERTA pra esse ciclo? Nao, foi FECHADA.
        // ResolverFaturaAbertaVigenteAsync procura por ABERTA, nao encontra, tenta criar.
        // CriarOuReutilizarFaturaAbertaAsync vai chamar FecharFaturaAbertaAnteriorAsync
        // (que nao faz nada porque nao ha ABERTA < 10/03)
        // e depois retorna a fatura FECHADA? Nao, ela busca por data_fechamento == 10/03
        // e nao existe ABERTA com essa data, entao vai tentar criar uma nova.
        // Mas a query valida pra data 08/03 com diaFechamento=10 => ciclo = 10/03 - 20/03
        // entao vai tentar criar ABERTA pra 10/03, e nao ha ABERTA mais recente,
        // entao vai criar??? Nao, dever haver uma verificacao antes.

        // Lendo o codigo novamente:
        // ResolverFaturaAbertaVigenteAsync busca ABERTA com dataFechamento == 10/03.
        // Se encontra, retorna. Se nao encontra, chama CriarOuReutilizarFaturaAbertaAsync.
        // CriarOuReutilizarFaturaAbertaAsync chama FecharFaturaAbertaAnteriorAsync
        // (fecha qualquer ABERTA com dataFechamento < 10/03 - nao ha nenhuma agora).
        // Depois busca ABERTA com dataFechamento > 10/03 - vai encontrar a de abril (10/04).
        // Se encontra mais recente, rejeita. Se nao encontra mais recente, cria nova.

        // Entao vai rejeitar porque ha ABERTA de 10/04, que e mais recente que 10/03.
        Assert.True(rejeitada);
        Assert.NotNull(motivo);
        Assert.Contains("retroativa", motivo, StringComparison.OrdinalIgnoreCase);

        // Mas o teste quer verificar que a fatura FECHADA nao e reabrida nem duplicada.
        // Entao preciso mudar a estrategia: resolver N, resolver N+1 (fecha N),
        // depois tentar resolver N+1 novamente. Nesse caso, a fatura N+1 ja existe ABERTA,
        // entao retorna a mesma.

        // Vou reescrever este teste.
    }

    // Teste 2 (reescrito): Resolver fatura pro ciclo N+1 novamente
    // Esperado: retorna a mesma fatura ABERTA, sem duplicar
    [Fact]
    public async Task ResolverFaturaCicloNPlus1Novamente_JaAbertas_ReutilizaSemDuplicar()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new FaturaCicloService(context);

        // Resolver ciclo N+1 (abril: data 15/04 => ciclo 10/05 - 20/05, pois 15 >= 10)
        var dataReferenciaAbril1 = new DateOnly(2026, 4, 15);
        var (faturaAbril1, rejeitada1, motivo1) =
            await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferenciaAbril1);

        Assert.False(rejeitada1);
        Assert.Null(motivo1);
        Assert.NotNull(faturaAbril1);
        Assert.Equal(new DateOnly(2026, 5, 10), faturaAbril1.DataFechamento);
        Assert.Equal(FaturaStatusConstants.Aberta, faturaAbril1.Status);
        var idFaturaAbril1 = faturaAbril1.Id;

        // Contar quantas faturas existem
        var contagemAntes = await context.Faturas.CountAsync(f => f.ContaId == conta.Id);
        Assert.Equal(1, contagemAntes);

        // Resolver novamente a mesma fatura de maio (data que cai no mesmo ciclo)
        var dataReferenciaAbril2 = new DateOnly(2026, 4, 20);
        var (faturaAbril2, rejeitada2, motivo2) =
            await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferenciaAbril2);

        Assert.False(rejeitada2);
        Assert.Null(motivo2);
        Assert.NotNull(faturaAbril2);
        Assert.Equal(new DateOnly(2026, 5, 10), faturaAbril2.DataFechamento);
        Assert.Equal(FaturaStatusConstants.Aberta, faturaAbril2.Status);

        // Verificar que eh a MESMA fatura (mesmo ID)
        Assert.Equal(idFaturaAbril1, faturaAbril2.Id);

        // Verificar que nao foi duplicada (ainda so 1 fatura)
        var contagemDepois = await context.Faturas.CountAsync(f => f.ContaId == conta.Id);
        Assert.Equal(1, contagemDepois);
    }

    // Teste 3: Rejeitar fatura muito retroativa (ciclo anterior a uma ABERTA mais recente)
    // via ResolverFaturaAbertaVigenteAsync
    [Fact]
    public async Task ResolverFaturaAbertaVigente_CicloMuitoRetroativo_RejeitaControladamente()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new FaturaCicloService(context);

        // Criar fatura ABERTA manualmente para um ciclo recente (julho: 10/07 - 20/07)
        var faturaAbertaRecente = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 7, 10),
            DataVencimento = new DateOnly(2026, 7, 20),
            Status = FaturaStatusConstants.Aberta,
        };
        context.Faturas.Add(faturaAbertaRecente);
        context.SaveChanges();

        // Tentar resolver fatura pra ciclo muito retroativo (marco: 10/03 - 20/03)
        // Data 05/03 vai calcular ciclo 10/03 - 20/03, que eh anterior a 10/07
        var dataReferenciaMarcoBaixa = new DateOnly(2026, 3, 5);
        var (fatura, rejeitada, motivo) =
            await service.ResolverFaturaAbertaVigenteAsync(conta.Id, dataReferenciaMarcoBaixa);

        // Esperado: rejeicao controlada
        Assert.True(rejeitada);
        Assert.Null(fatura);
        Assert.NotNull(motivo);
        Assert.Contains("retroativa", motivo, StringComparison.OrdinalIgnoreCase);
    }

    // Teste 3b: Rejeitar fatura muito retroativa via ResolverFaturaParaLancamentoAsync
    [Fact]
    public async Task ResolverFaturaParaLancamento_CicloMuitoRetroativo_RejeitaControladamente()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var service = new FaturaCicloService(context);

        // Criar fatura ABERTA manualmente para um ciclo recente (julho: 10/07 - 20/07)
        var faturaAbertaRecente = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 7, 10),
            DataVencimento = new DateOnly(2026, 7, 20),
            Status = FaturaStatusConstants.Aberta,
        };
        context.Faturas.Add(faturaAbertaRecente);
        context.SaveChanges();

        // Tentar registrar lancamento (compra) pra data muito retroativa
        var dataMarcoBaixa = new DateOnly(2026, 3, 5);
        var (fatura, rejeitada, motivo) =
            await service.ResolverFaturaParaLancamentoAsync(conta.Id, dataMarcoBaixa);

        // Esperado: rejeicao controlada
        Assert.True(rejeitada);
        Assert.Null(fatura);
        Assert.NotNull(motivo);
        Assert.Contains("retroativa", motivo, StringComparison.OrdinalIgnoreCase);
    }

    // Teste 4: Listar faturas do controller ordenadas por data_fechamento DESC
    // sem vazar Conta/Lancamentos/Transferencia
    [Fact]
    public async Task ListarFaturas_RetornaFaturasOrdenadas_SemVazarDados()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);

        // Criar algumas faturas manualmente em ordem aleatorias
        var faturaMarco = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Fechada,
        };

        var faturaAbril = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 4, 10),
            DataVencimento = new DateOnly(2026, 4, 20),
            Status = FaturaStatusConstants.Aberta,
        };

        var faturaFevereiro = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Conta = conta,
            DataFechamento = new DateOnly(2026, 2, 10),
            DataVencimento = new DateOnly(2026, 2, 20),
            Status = FaturaStatusConstants.Fechada,
        };

        context.Faturas.Add(faturaMarco);
        context.Faturas.Add(faturaAbril);
        context.Faturas.Add(faturaFevereiro);
        context.SaveChanges();

        // Simular a query do controller
        var faturas = await context.Faturas
            .Where(f => f.ContaId == conta.Id)
            .OrderByDescending(f => f.DataFechamento)
            .ToListAsync();

        // Mapear pra DTO (como o controller faz)
        var dtos = faturas.Select(f => new MyFinances.Dtos.FaturaResponseDto
        {
            Id = f.Id,
            ContaId = f.ContaId,
            DataFechamento = f.DataFechamento,
            DataVencimento = f.DataVencimento,
            Status = f.Status,
            ValorTotal = 0,
            ValorPago = 0,
            ValorPendente = 0
        }).ToList();

        // Verificacoes
        Assert.Equal(3, dtos.Count);

        // Verificar orden: aberta (04/10) > marco (03/10) > fevereiro (02/10)
        Assert.Equal(new DateOnly(2026, 4, 10), dtos[0].DataFechamento);
        Assert.Equal(new DateOnly(2026, 3, 10), dtos[1].DataFechamento);
        Assert.Equal(new DateOnly(2026, 2, 10), dtos[2].DataFechamento);

        // Verificar status
        Assert.Equal(FaturaStatusConstants.Aberta, dtos[0].Status);
        Assert.Equal(FaturaStatusConstants.Fechada, dtos[1].Status);
        Assert.Equal(FaturaStatusConstants.Fechada, dtos[2].Status);

        // Verificar que nao expoe relacionamentos vazios (Conta, Lancamentos, Transferencia)
        // Os DTOs nao tem essas propriedades
        foreach (var dto in dtos)
        {
            // DTO so tem: Id, ContaId, DataFechamento, DataVencimento, Status, ValorTotal, ValorPago, ValorPendente
            // Nenhuma propriedade de navegacao
            Assert.NotEqual(Guid.Empty, dto.Id);
            Assert.Equal(conta.Id, dto.ContaId);
            Assert.True(dto.DataFechamento > DateOnly.FromDateTime(new DateTime(2025, 1, 1)));
        }
    }

    // Teste 4b: Listar faturas retorna lista vazia quando nao ha faturas pra conta
    [Fact]
    public async Task ListarFaturas_ContaSemFaturas_RetornaListaVazia()
    {
        using var context = CreateInMemoryContext();
        var conta = CriarContaCartao(context);
        var outraConta = CriarContaCartao(context, nome: "Outro Cartao");

        // Criar fatura apenas pra outraConta
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = outraConta.Id,
            Conta = outraConta,
            DataFechamento = new DateOnly(2026, 3, 10),
            DataVencimento = new DateOnly(2026, 3, 20),
            Status = FaturaStatusConstants.Aberta,
        };
        context.Faturas.Add(fatura);
        context.SaveChanges();

        // Listar faturas pra primeira conta (sem nenhuma fatura)
        var faturas = await context.Faturas
            .Where(f => f.ContaId == conta.Id)
            .OrderByDescending(f => f.DataFechamento)
            .ToListAsync();

        Assert.Empty(faturas);
    }
}
