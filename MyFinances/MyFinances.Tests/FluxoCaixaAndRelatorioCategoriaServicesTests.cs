using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class FluxoCaixaAndRelatorioCategoriaServicesTests
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

    private Categoria CriarCategoria(
        AppDbContext context,
        string nome,
        string tipo = "DESPESA")
    {
        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Tipo = tipo,
            Arquivada = false
        };
        context.Categorias.Add(categoria);
        context.SaveChanges();
        return categoria;
    }

    private Fatura CriarFatura(
        AppDbContext context,
        Guid contaId,
        DateOnly dataFechamento,
        DateOnly dataVencimento,
        string status = FaturaStatusConstants.Aberta,
        Guid? transferenciaId = null)
    {
        var conta = context.Contas.Find(contaId);
        var fatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta!,
            DataFechamento = dataFechamento,
            DataVencimento = dataVencimento,
            Status = status,
        };
        context.Faturas.Add(fatura);
        context.SaveChanges();
        return fatura;
    }

    private Transferencia CriarTransferencia(
        AppDbContext context,
        Guid contaOrigemId,
        Guid contaDestinoId,
        decimal valor,
        DateOnly data,
        string descricao = "Transferencia")
    {
        var contaOrigem = context.Contas.Find(contaOrigemId);
        var contaDestino = context.Contas.Find(contaDestinoId);
        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            Data = data,
            Valor = valor,
            ContaOrigemId = contaOrigemId,
            ContaOrigem = contaOrigem!,
            ContaDestinoId = contaDestinoId,
            ContaDestino = contaDestino!,
            Descricao = descricao
        };
        context.Transferencias.Add(transferencia);
        context.SaveChanges();
        return transferencia;
    }

    // ==============================
    // CENARIO CENTRAL — A REGRA CRITICA
    // ==============================

    [Fact]
    public async Task CenarioCentral_CartaoComTresComprasEmCategoriasDiferentesEPagamento_NuncaHaDuplaContagem()
    {
        // Arranjar: conta cartao, 3 categorias, 3 compras, 1 fatura, 1 pagamento
        using var context = CreateInMemoryContext();
        var contaCartao = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var contaBanco = CriarContaBanco(context);

        var catAlimentacao = CriarCategoria(context, "Alimentacao");
        var catTransporte = CriarCategoria(context, "Transporte");
        var catLazer = CriarCategoria(context, "Lazer");

        // Fatura aberta para marco
        var fatura = CriarFatura(context, contaCartao.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20));

        // Adicionar 3 compras em categorias diferentes
        var compra1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Conta = contaCartao,
            CategoriaId = catAlimentacao.Id,
            Categoria = catAlimentacao,
            Descricao = "Supermercado",
            Valor = 100m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = fatura.Id,
            Fatura = fatura
        };
        context.Lancamentos.Add(compra1);

        var compra2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Conta = contaCartao,
            CategoriaId = catTransporte.Id,
            Categoria = catTransporte,
            Descricao = "Uber",
            Valor = 50m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = fatura.Id,
            Fatura = fatura
        };
        context.Lancamentos.Add(compra2);

        var compra3 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Conta = contaCartao,
            CategoriaId = catLazer.Id,
            Categoria = catLazer,
            Descricao = "Cinema",
            Valor = 75m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 18),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = fatura.Id,
            Fatura = fatura
        };
        context.Lancamentos.Add(compra3);
        context.SaveChanges();

        // Criar pagamento de fatura (transferencia CC -> CARTAO)
        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            Data = new DateOnly(2026, 3, 20),
            Valor = 225m,
            ContaOrigemId = contaBanco.Id,
            ContaOrigem = contaBanco,
            ContaDestinoId = contaCartao.Id,
            ContaDestino = contaCartao,
            FaturaId = fatura.Id,
            Descricao = "Pagamento de fatura"
        };
        context.Transferencias.Add(transferencia);

        // Adicionar as duas pernas da transferencia: DEBIT (saida CC), CREDIT (entrada CARTAO)
        var pernaDEBIT = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaBanco.Id,
            Conta = contaBanco,
            Descricao = "Pagamento fatura cartao",
            Valor = 225m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 20),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            TransferenciaId = transferencia.Id,
            Transferencia = transferencia
        };
        context.Lancamentos.Add(pernaDEBIT);

        var pernaCREDIT = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Conta = contaCartao,
            Descricao = "Pagamento fatura cartao",
            Valor = 225m,
            Tipo = TipoLancamentoConstants.Credit,
            Data = new DateOnly(2026, 3, 20),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            TransferenciaId = transferencia.Id,
            Transferencia = transferencia
        };
        context.Lancamentos.Add(pernaCREDIT);
        context.SaveChanges();

        var fluxoCaixaService = new FluxoCaixaService(context);
        var relatorioCategoriaService = new RelatorioCategoriaService(context);

        // Act: obter lancamentos do fluxo de caixa
        var caixa = await fluxoCaixaService.ObterLancamentosCaixaAsync();

        // Assert: fluxo de caixa deve ter EXATAMENTE 1 linha (o pagamento perna DEBIT do banco)
        Assert.Single(caixa);
        var linhaFluxo = caixa.First();
        Assert.Equal(pernaDEBIT.Id, linhaFluxo.Id);
        Assert.Equal(contaBanco.Id, linhaFluxo.ContaId);
        Assert.Equal(225m, linhaFluxo.Valor);
        Assert.Equal(TipoLancamentoConstants.Debit, linhaFluxo.Tipo);
        // As compras nao devem aparecer
        Assert.DoesNotContain(caixa, l => l.Id == compra1.Id);
        Assert.DoesNotContain(caixa, l => l.Id == compra2.Id);
        Assert.DoesNotContain(caixa, l => l.Id == compra3.Id);

        // Act: obter relatorio categorico
        var relatorio = await relatorioCategoriaService.ObterGastoPorCategoriaAsync(2026, 3);

        // Assert: relatorio deve ter 3 grupos (uma categoria por compra)
        Assert.Equal(3, relatorio.Itens.Count());

        var alimentacao = relatorio.Itens.FirstOrDefault(i => i.CategoriaId == catAlimentacao.Id);
        var transporte = relatorio.Itens.FirstOrDefault(i => i.CategoriaId == catTransporte.Id);
        var lazer = relatorio.Itens.FirstOrDefault(i => i.CategoriaId == catLazer.Id);

        Assert.NotNull(alimentacao);
        Assert.NotNull(transporte);
        Assert.NotNull(lazer);

        Assert.Equal(100m, alimentacao.Total);
        Assert.Equal(50m, transporte.Total);
        Assert.Equal(75m, lazer.Total);

        // O pagamento NAO deve aparecer em nenhuma categoria
        var somaTotalCompetencia = relatorio.Itens.Sum(i => i.Total);
        Assert.Equal(225m, somaTotalCompetencia);

        // PROVA DE NUNCA DUPLA CONTAGEM:
        // Soma CAIXA (pagamento) = 225m
        // Soma COMPETENCIA (compras) = 225m
        // Mas NAO aparecem nas mesmas linhas — CAIXA tem perna DEBIT, COMPETENCIA tem as compras individuais
        var somaCaixa = caixa.Sum(l => Math.Abs(l.Valor));
        Assert.Equal(225m, somaCaixa);

        // Verificar que sao valores iguais mas nao ha overlap
        // (o pagamento nao aparece em relatorio, as compras nao aparecem em caixa)
        Assert.Equal(somaCaixa, somaTotalCompetencia);
        // Mas as linhas sao distintas
        Assert.DoesNotContain(caixa, l => l.Id == compra1.Id || l.Id == compra2.Id || l.Id == compra3.Id);
    }

    // ==============================
    // FLUXO DE CAIXA — LANCAMENTO OCULTO
    // ==============================

    [Fact]
    public async Task FluxoCaixa_LancamentoOcultoTrue_NaoAparece()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context);

        // Criar um lancamento normal e outro oculto
        var lancamentoNormal = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaBanco.Id,
            Conta = contaBanco,
            Descricao = "Compra normal",
            Valor = 50m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false
        };
        context.Lancamentos.Add(lancamentoNormal);

        var lancamentoOculto = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaBanco.Id,
            Conta = contaBanco,
            Descricao = "Compra oculta",
            Valor = 30m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 16),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = true  // Oculto!
        };
        context.Lancamentos.Add(lancamentoOculto);
        context.SaveChanges();

        var service = new FluxoCaixaService(context);

        // Act
        var caixa = await service.ObterLancamentosCaixaAsync();

        // Assert: apenas o lancamento normal deve aparecer
        Assert.Single(caixa);
        Assert.Equal(lancamentoNormal.Id, caixa.First().Id);
        Assert.DoesNotContain(caixa, l => l.Id == lancamentoOculto.Id);
    }

    // ==============================
    // FLUXO DE CAIXA — LANCAMENTO NORMAL (NAO CARTAO, NAO TRANSFERENCIA)
    // ==============================

    [Fact]
    public async Task FluxoCaixa_LancamentoNormalBanco_ApareceComSeuTipoOriginal()
    {
        using var context = CreateInMemoryContext();
        var contaBanco = CriarContaBanco(context);

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaBanco.Id,
            Conta = contaBanco,
            Descricao = "Deposito",
            Valor = 1000m,
            Tipo = TipoLancamentoConstants.Credit,  // CREDIT
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false
        };
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();

        var service = new FluxoCaixaService(context);

        // Act
        var caixa = await service.ObterLancamentosCaixaAsync();

        // Assert
        Assert.Single(caixa);
        var linha = caixa.First();
        Assert.Equal(lancamento.Id, linha.Id);
        Assert.Equal(TipoLancamentoConstants.Credit, linha.Tipo);  // Manteu o tipo original
        Assert.Equal(1000m, linha.Valor);
    }

    // ==============================
    // FLUXO DE CAIXA — TRANSFERENCIA (SO PERNA DEBIT)
    // ==============================

    [Fact]
    public async Task FluxoCaixa_Transferencia_SoAPernaDEBITAparece_NuncaDuasNemSoCredit()
    {
        using var context = CreateInMemoryContext();
        var contaOrigem = CriarContaBanco(context, "Conta A");
        var contaDestino = CriarContaBanco(context, "Conta B");

        var transferencia = CriarTransferencia(context, contaOrigem.Id, contaDestino.Id, 500m, new DateOnly(2026, 3, 15));

        // Adicionar duas pernas
        var pernaDEBIT = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaOrigem.Id,
            Conta = contaOrigem,
            Descricao = "Transferencia",
            Valor = 500m,
            Tipo = TipoLancamentoConstants.Debit,  // DEBIT
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            TransferenciaId = transferencia.Id,
            Transferencia = transferencia
        };
        context.Lancamentos.Add(pernaDEBIT);

        var pernaCREDIT = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaDestino.Id,
            Conta = contaDestino,
            Descricao = "Transferencia",
            Valor = 500m,
            Tipo = TipoLancamentoConstants.Credit,  // CREDIT
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            TransferenciaId = transferencia.Id,
            Transferencia = transferencia
        };
        context.Lancamentos.Add(pernaCREDIT);
        context.SaveChanges();

        var service = new FluxoCaixaService(context);

        // Act
        var caixa = await service.ObterLancamentosCaixaAsync();

        // Assert: SO a perna DEBIT deve aparecer
        Assert.Single(caixa);
        var linha = caixa.First();
        Assert.Equal(pernaDEBIT.Id, linha.Id);
        Assert.Equal(TipoLancamentoConstants.Debit, linha.Tipo);
        // Perna CREDIT NAO deve aparecer sozinha
        Assert.DoesNotContain(caixa, l => l.Id == pernaCREDIT.Id);
    }

    // ==============================
    // RELATORIO CATEGORIA — ESTORNO
    // ==============================

    [Fact]
    public async Task RelatorioCategoria_EstornoNaMesmaCategoria_ReduzOTotalDoGrupo()
    {
        using var context = CreateInMemoryContext();
        var contaCartao = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var categoria = CriarCategoria(context, "Alimentacao");

        var fatura = CriarFatura(context, contaCartao.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20));

        // Compra de 100
        var compra = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Conta = contaCartao,
            CategoriaId = categoria.Id,
            Categoria = categoria,
            Descricao = "Compra",
            Valor = 100m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = fatura.Id,
            Fatura = fatura
        };
        context.Lancamentos.Add(compra);

        // Estorno de -30 (negativo = estorno)
        var estorno = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Conta = contaCartao,
            CategoriaId = categoria.Id,
            Categoria = categoria,
            Descricao = "Estorno",
            Valor = -30m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 18),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = fatura.Id,
            Fatura = fatura
        };
        context.Lancamentos.Add(estorno);
        context.SaveChanges();

        var service = new RelatorioCategoriaService(context);

        // Act
        var relatorio = await service.ObterGastoPorCategoriaAsync(2026, 3);

        // Assert: deve ter 1 grupo com total = 100 - 30 = 70
        Assert.Single(relatorio.Itens);
        var grupo = relatorio.Itens.First();
        Assert.Equal(categoria.Id, grupo.CategoriaId);
        Assert.Equal(70m, grupo.Total);  // 100 - 30
    }

    // ==============================
    // RELATORIO CATEGORIA — COMPRA SEM CATEGORIA (NULL)
    // ==============================

    [Fact]
    public async Task RelatorioCategoria_CompraSemCategoria_ApareceComoGrupoProprio_SemCrash()
    {
        using var context = CreateInMemoryContext();
        var contaCartao = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var categoria = CriarCategoria(context, "Alimentacao");

        var fatura = CriarFatura(context, contaCartao.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20));

        // Compra com categoria
        var compraComCategoria = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Conta = contaCartao,
            CategoriaId = categoria.Id,
            Categoria = categoria,
            Descricao = "Compra com categoria",
            Valor = 100m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = fatura.Id,
            Fatura = fatura
        };
        context.Lancamentos.Add(compraComCategoria);

        // Compra SEM categoria (CategoriaId = null)
        var compraSemCategoria = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Conta = contaCartao,
            CategoriaId = null,  // SEM CATEGORIA
            Categoria = null,
            Descricao = "Compra sem categoria",
            Valor = 50m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 16),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = fatura.Id,
            Fatura = fatura
        };
        context.Lancamentos.Add(compraSemCategoria);
        context.SaveChanges();

        var service = new RelatorioCategoriaService(context);

        // Act: NAO deve lancar excecao
        var relatorio = await service.ObterGastoPorCategoriaAsync(2026, 3);

        // Assert: deve ter 2 grupos (1 com categoria, 1 sem)
        Assert.Equal(2, relatorio.Itens.Count());

        var grupoComCategoria = relatorio.Itens.FirstOrDefault(i => i.CategoriaId == categoria.Id);
        var grupoSemCategoria = relatorio.Itens.FirstOrDefault(i => i.CategoriaId == null);

        Assert.NotNull(grupoComCategoria);
        Assert.NotNull(grupoSemCategoria);

        Assert.Equal(100m, grupoComCategoria.Total);
        Assert.Equal(50m, grupoSemCategoria.Total);
        Assert.Null(grupoSemCategoria.NomeCategoria);  // Sem nome
    }

    // ==============================
    // RELATORIO CATEGORIA — COMPRA FORA DO MES
    // ==============================

    [Fact]
    public async Task RelatorioCategoria_CompraForaDo_MesFiltrado_NaoAparece()
    {
        using var context = CreateInMemoryContext();
        var contaCartao = CriarContaCartao(context, diaFechamento: 10, diaVencimento: 20);
        var categoria = CriarCategoria(context, "Alimentacao");

        // Fatura para marco
        var faturaMarco = CriarFatura(context, contaCartao.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20));

        // Fatura para abril
        var faturaAbril = CriarFatura(context, contaCartao.Id, new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 20));

        // Compra em marco
        var compraMarco = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Conta = contaCartao,
            CategoriaId = categoria.Id,
            Categoria = categoria,
            Descricao = "Compra marco",
            Valor = 100m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = faturaMarco.Id,
            Fatura = faturaMarco
        };
        context.Lancamentos.Add(compraMarco);

        // Compra em abril
        var compraAbril = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao.Id,
            Conta = contaCartao,
            CategoriaId = categoria.Id,
            Categoria = categoria,
            Descricao = "Compra abril",
            Valor = 75m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 4, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = faturaAbril.Id,
            Fatura = faturaAbril
        };
        context.Lancamentos.Add(compraAbril);
        context.SaveChanges();

        var service = new RelatorioCategoriaService(context);

        // Act: filtrar SO para marco
        var relatorioMarco = await service.ObterGastoPorCategoriaAsync(2026, 3);

        // Assert: deve conter SO a compra de marco
        Assert.Single(relatorioMarco.Itens);
        var grupo = relatorioMarco.Itens.First();
        Assert.Equal(categoria.Id, grupo.CategoriaId);
        Assert.Equal(100m, grupo.Total);  // SO a compra de marco

        // Act: filtrar para abril
        var relatorioAbril = await service.ObterGastoPorCategoriaAsync(2026, 4);

        // Assert: deve conter SO a compra de abril
        Assert.Single(relatorioAbril.Itens);
        var grupoAbril = relatorioAbril.Itens.First();
        Assert.Equal(categoria.Id, grupoAbril.CategoriaId);
        Assert.Equal(75m, grupoAbril.Total);  // SO a compra de abril
    }

    // ==============================
    // RELATORIO CATEGORIA — FILTRO POR CONTA
    // ==============================

    [Fact]
    public async Task RelatorioCategoria_FiltroContaEspecifica_RetornaApenasComprasDaquaConta()
    {
        using var context = CreateInMemoryContext();
        var contaCartao1 = CriarContaCartao(context, "Cartao 1", diaFechamento: 10, diaVencimento: 20);
        var contaCartao2 = CriarContaCartao(context, "Cartao 2", diaFechamento: 10, diaVencimento: 20);
        var categoria = CriarCategoria(context, "Alimentacao");

        var fatura1 = CriarFatura(context, contaCartao1.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20));
        var fatura2 = CriarFatura(context, contaCartao2.Id, new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20));

        // Compra na cartao 1
        var compra1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao1.Id,
            Conta = contaCartao1,
            CategoriaId = categoria.Id,
            Categoria = categoria,
            Descricao = "Compra cartao 1",
            Valor = 100m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = fatura1.Id,
            Fatura = fatura1
        };
        context.Lancamentos.Add(compra1);

        // Compra na cartao 2
        var compra2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaCartao2.Id,
            Conta = contaCartao2,
            CategoriaId = categoria.Id,
            Categoria = categoria,
            Descricao = "Compra cartao 2",
            Valor = 50m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false,
            FaturaId = fatura2.Id,
            Fatura = fatura2
        };
        context.Lancamentos.Add(compra2);
        context.SaveChanges();

        var service = new RelatorioCategoriaService(context);

        // Act: filtrar para cartao 1
        var relatorio1 = await service.ObterGastoPorCategoriaAsync(2026, 3, contaCartao1.Id);

        // Assert: deve conter SO a compra de cartao 1
        Assert.Single(relatorio1.Itens);
        var grupo = relatorio1.Itens.First();
        Assert.Equal(100m, grupo.Total);

        // Act: filtrar para cartao 2
        var relatorio2 = await service.ObterGastoPorCategoriaAsync(2026, 3, contaCartao2.Id);

        // Assert: deve conter SO a compra de cartao 2
        Assert.Single(relatorio2.Itens);
        var grupo2 = relatorio2.Itens.First();
        Assert.Equal(50m, grupo2.Total);
    }

    // ==============================
    // FLUXO DE CAIXA — FILTRO POR CONTA
    // ==============================

    [Fact]
    public async Task FluxoCaixa_FiltroContaEspecifica_RetornaApenasLancamentosDaquaConta()
    {
        using var context = CreateInMemoryContext();
        var contaBanco1 = CriarContaBanco(context, "Banco 1");
        var contaBanco2 = CriarContaBanco(context, "Banco 2");

        var lancamento1 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaBanco1.Id,
            Conta = contaBanco1,
            Descricao = "Lancamento banco 1",
            Valor = 100m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false
        };
        context.Lancamentos.Add(lancamento1);

        var lancamento2 = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaBanco2.Id,
            Conta = contaBanco2,
            Descricao = "Lancamento banco 2",
            Valor = 50m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = new DateOnly(2026, 3, 15),
            Status = LancamentoStatusConstants.Pago,
            Manual = true,
            Oculto = false
        };
        context.Lancamentos.Add(lancamento2);
        context.SaveChanges();

        var service = new FluxoCaixaService(context);

        // Act: filtrar para banco 1
        var caixa1 = await service.ObterLancamentosCaixaAsync(contaBanco1.Id);

        // Assert: deve conter SO o lancamento de banco 1
        Assert.Single(caixa1);
        Assert.Equal(lancamento1.Id, caixa1.First().Id);

        // Act: filtrar para banco 2
        var caixa2 = await service.ObterLancamentosCaixaAsync(contaBanco2.Id);

        // Assert: deve conter SO o lancamento de banco 2
        Assert.Single(caixa2);
        Assert.Equal(lancamento2.Id, caixa2.First().Id);
    }
}
