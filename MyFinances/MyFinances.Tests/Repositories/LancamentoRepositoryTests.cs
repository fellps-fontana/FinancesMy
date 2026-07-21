using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using Xunit;

namespace MyFinances.Tests.Repositories;

public class LancamentoRepositoryTests
{
    private async Task<(MyFinancesDbContext dbContext, SqliteConnection connection)> CriarDbContext()
    {
        var connectionString = "DataSource=:memory:";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new MyFinancesDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        return (dbContext, connection);
    }

    [Fact]
    public async Task ListarParaFluxoCaixaDoMes_ApenasLancamentosDoMesEAnoSpecificados_RetornaApenasMes()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar conta
            var contaId = Guid.NewGuid();
            var conta = new Conta
            {
                Id = contaId,
                Nome = "Conta Teste",
                Tipo = TipoConta.Banco,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(conta);

            // Criar lancamento no mes/ano solicitado (2026/7)
            var lancamentoDoMes = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 100m,
                Data = new DateOnly(2026, 7, 15),
                Manual = true,
                Oculto = false
            };

            // Criar lancamento de mes anterior (2026/6)
            var lancamentoMesAnterior = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 50m,
                Data = new DateOnly(2026, 6, 15),
                Manual = true,
                Oculto = false
            };

            // Criar lancamento de mes posterior (2026/8)
            var lancamentoMesPosterior = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 75m,
                Data = new DateOnly(2026, 8, 15),
                Manual = true,
                Oculto = false
            };

            dbContext.Lancamentos.Add(lancamentoDoMes);
            dbContext.Lancamentos.Add(lancamentoMesAnterior);
            dbContext.Lancamentos.Add(lancamentoMesPosterior);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new LancamentoRepository(dbContext);
            var resultado = await repositorio.ListarParaFluxoCaixaDoMes(2026, 7);

            // Assert
            Assert.Single(resultado);
            var lancamento = resultado.First();
            Assert.Equal(lancamentoDoMes.Id, lancamento.Id);
            Assert.Equal(new DateOnly(2026, 7, 15), lancamento.Data);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ListarParaFluxoCaixaDoMes_ExcluiLancamentosComFaturaId_NaoRetornaFatura()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar conta
            var contaId = Guid.NewGuid();
            var conta = new Conta
            {
                Id = contaId,
                Nome = "Conta Cartao",
                Tipo = TipoConta.Cartao,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(conta);

            // Criar fatura
            var faturaId = Guid.NewGuid();
            var fatura = new Fatura
            {
                Id = faturaId,
                ContaId = contaId,
                DataFechamento = new DateOnly(2026, 7, 5),
                DataVencimento = new DateOnly(2026, 7, 20),
                Status = StatusFatura.Aberta
            };
            dbContext.Faturas.Add(fatura);

            // Criar lancamento SEM fatura (fluxo de caixa normal)
            var lancamentoSemFatura = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 100m,
                Data = new DateOnly(2026, 7, 15),
                Manual = true,
                Oculto = false
            };

            // Criar lancamento COM fatura (competencia de cartao, nao deve aparecer)
            var lancamentoComFatura = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                FaturaId = faturaId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 50m,
                Data = new DateOnly(2026, 7, 10),
                Manual = true,
                Oculto = false
            };

            dbContext.Lancamentos.Add(lancamentoSemFatura);
            dbContext.Lancamentos.Add(lancamentoComFatura);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new LancamentoRepository(dbContext);
            var resultado = await repositorio.ListarParaFluxoCaixaDoMes(2026, 7);

            // Assert
            Assert.Single(resultado);
            var lancamento = resultado.First();
            Assert.Null(lancamento.FaturaId);
            Assert.Equal(lancamentoSemFatura.Id, lancamento.Id);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ListarParaFluxoCaixaDoMes_ExcluiLancamentosOcultos_NaoRetornaOculto()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar conta
            var contaId = Guid.NewGuid();
            var conta = new Conta
            {
                Id = contaId,
                Nome = "Conta Teste",
                Tipo = TipoConta.Banco,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(conta);

            // Criar lancamento visivel
            var lancamentoVisivel = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 100m,
                Data = new DateOnly(2026, 7, 15),
                Manual = true,
                Oculto = false
            };

            // Criar lancamento oculto
            var lancamentoOculto = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 75m,
                Data = new DateOnly(2026, 7, 10),
                Manual = true,
                Oculto = true
            };

            dbContext.Lancamentos.Add(lancamentoVisivel);
            dbContext.Lancamentos.Add(lancamentoOculto);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new LancamentoRepository(dbContext);
            var resultado = await repositorio.ListarParaFluxoCaixaDoMes(2026, 7);

            // Assert
            Assert.Single(resultado);
            var lancamento = resultado.First();
            Assert.False(lancamento.Oculto);
            Assert.Equal(lancamentoVisivel.Id, lancamento.Id);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ListarParaFluxoCaixaDoMes_ExcluiTransferencias_NaoRetornaTransferencia()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar duas contas
            var contaOrigemId = Guid.NewGuid();
            var contaDestId = Guid.NewGuid();

            var contaOrigem = new Conta
            {
                Id = contaOrigemId,
                Nome = "Conta Origem",
                Tipo = TipoConta.Banco,
                Origem = OrigemConta.Manual,
                Ativa = true
            };

            var contaDest = new Conta
            {
                Id = contaDestId,
                Nome = "Conta Destino",
                Tipo = TipoConta.Investimento,
                Origem = OrigemConta.Manual,
                Ativa = true
            };

            dbContext.Contas.Add(contaOrigem);
            dbContext.Contas.Add(contaDest);

            // Criar transferencia (mesma titularidade)
            var transferenciaId = Guid.NewGuid();
            var transferencia = new Transferencia
            {
                Id = transferenciaId,
                ContaOrigemId = contaOrigemId,
                ContaDestinoId = contaDestId
            };
            dbContext.Transferencias.Add(transferencia);

            // Criar lancamento normal (sem transferencia)
            var lancamentoNormal = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaOrigemId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 100m,
                Data = new DateOnly(2026, 7, 15),
                Manual = true,
                Oculto = false
            };

            // Criar lancamento de transferencia (saida)
            var lancamentoTransferenciaSaida = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaOrigemId,
                TransferenciaId = transferenciaId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 500m,
                Data = new DateOnly(2026, 7, 10),
                Manual = true,
                Oculto = false
            };

            // Criar lancamento de transferencia (entrada)
            var lancamentoTransferenciaEntrada = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaDestId,
                TransferenciaId = transferenciaId,
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 500m,
                Data = new DateOnly(2026, 7, 10),
                Manual = true,
                Oculto = false
            };

            dbContext.Lancamentos.Add(lancamentoNormal);
            dbContext.Lancamentos.Add(lancamentoTransferenciaSaida);
            dbContext.Lancamentos.Add(lancamentoTransferenciaEntrada);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new LancamentoRepository(dbContext);
            var resultado = await repositorio.ListarParaFluxoCaixaDoMes(2026, 7);

            // Assert
            Assert.Single(resultado);
            var lancamento = resultado.First();
            Assert.Null(lancamento.TransferenciaId);
            Assert.Equal(lancamentoNormal.Id, lancamento.Id);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ListarParaFluxoCaixaDoMes_SemLancamentosNoMes_RetornaListaVazia()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar conta
            var contaId = Guid.NewGuid();
            var conta = new Conta
            {
                Id = contaId,
                Nome = "Conta Teste",
                Tipo = TipoConta.Banco,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(conta);

            // Criar lancamento em outro mes
            var lancamento = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 100m,
                Data = new DateOnly(2026, 6, 15),
                Manual = true,
                Oculto = false
            };

            dbContext.Lancamentos.Add(lancamento);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new LancamentoRepository(dbContext);
            var resultado = await repositorio.ListarParaFluxoCaixaDoMes(2026, 7);

            // Assert
            Assert.Empty(resultado);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ListarParaFluxoCaixaDoMes_IncludeContaECategoria_RelacionamosCarregados()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar conta
            var contaId = Guid.NewGuid();
            var conta = new Conta
            {
                Id = contaId,
                Nome = "Conta Teste",
                Tipo = TipoConta.Banco,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(conta);

            // Criar categoria
            var categoriaId = Guid.NewGuid();
            var categoria = new Categoria
            {
                Id = categoriaId,
                Nome = "Alimentacao",
                Tipo = TipoCategoria.Despesa,
                Arquivada = false
            };
            dbContext.Categorias.Add(categoria);

            // Criar lancamento com conta e categoria
            var lancamento = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                CategoriaId = categoriaId,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pago,
                Valor = 100m,
                Data = new DateOnly(2026, 7, 15),
                Manual = true,
                Oculto = false
            };

            dbContext.Lancamentos.Add(lancamento);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new LancamentoRepository(dbContext);
            var resultado = await repositorio.ListarParaFluxoCaixaDoMes(2026, 7);

            // Assert
            Assert.Single(resultado);
            var lancamentoResultado = resultado.First();
            Assert.NotNull(lancamentoResultado.Conta);
            Assert.Equal("Conta Teste", lancamentoResultado.Conta.Nome);
            Assert.NotNull(lancamentoResultado.Categoria);
            Assert.Equal("Alimentacao", lancamentoResultado.Categoria.Nome);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }
}
