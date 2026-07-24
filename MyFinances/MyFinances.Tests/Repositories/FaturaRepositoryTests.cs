using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using Xunit;

namespace MyFinances.Tests.Repositories;

public class FaturaRepositoryTests
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
    public async Task ListarFaturasCartaoPorVencimentoNoMes_RetornaFaturaDeCartaoNoMesCerto()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar Conta Cartao
            var contaCartaoId = Guid.NewGuid();
            var contaCartao = new Conta
            {
                Id = contaCartaoId,
                Nome = "Cartao Credito",
                Tipo = TipoConta.Cartao,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(contaCartao);

            // Criar Fatura de Cartao com Vencimento em julho de 2026
            var faturaCartaoJulho = new Fatura
            {
                Id = Guid.NewGuid(),
                ContaId = contaCartaoId,
                DataFechamento = new DateOnly(2026, 6, 30),
                DataVencimento = new DateOnly(2026, 7, 15),
                Status = StatusFatura.Fechada
            };
            dbContext.Faturas.Add(faturaCartaoJulho);

            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new FaturaRepository(dbContext);
            var resultado = await repositorio.ListarFaturasCartaoPorVencimentoNoMes(2026, 7);

            // Assert
            Assert.NotEmpty(resultado);
            var fatura = resultado.Single();
            Assert.Equal(faturaCartaoJulho.Id, fatura.Id);
            Assert.Equal(contaCartaoId, fatura.ContaId);
            Assert.Equal(TipoConta.Cartao, fatura.Conta!.Tipo);
            Assert.Equal(2026, fatura.DataVencimento.Year);
            Assert.Equal(7, fatura.DataVencimento.Month);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ListarFaturasCartaoPorVencimentoNoMes_IgnoraFaturaDeMesDiferente()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar Conta Cartao
            var contaCartaoId = Guid.NewGuid();
            var contaCartao = new Conta
            {
                Id = contaCartaoId,
                Nome = "Cartao Credito",
                Tipo = TipoConta.Cartao,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(contaCartao);

            // Criar Fatura de Cartao com Vencimento em JUNHO de 2026
            var faturaCartaoJunho = new Fatura
            {
                Id = Guid.NewGuid(),
                ContaId = contaCartaoId,
                DataFechamento = new DateOnly(2026, 5, 30),
                DataVencimento = new DateOnly(2026, 6, 15),
                Status = StatusFatura.Fechada
            };
            dbContext.Faturas.Add(faturaCartaoJunho);

            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new FaturaRepository(dbContext);
            var resultado = await repositorio.ListarFaturasCartaoPorVencimentoNoMes(2026, 7);

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
    public async Task ListarFaturasCartaoPorVencimentoNoMes_IgnoraContaQueNaoECartao()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar Conta Banco
            var contaBancoId = Guid.NewGuid();
            var contaBanco = new Conta
            {
                Id = contaBancoId,
                Nome = "Conta Banco",
                Tipo = TipoConta.Banco,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(contaBanco);

            // Criar Fatura para Conta Banco com Vencimento em julho de 2026
            var faturaBancoJulho = new Fatura
            {
                Id = Guid.NewGuid(),
                ContaId = contaBancoId,
                DataFechamento = new DateOnly(2026, 6, 30),
                DataVencimento = new DateOnly(2026, 7, 15),
                Status = StatusFatura.Fechada
            };
            dbContext.Faturas.Add(faturaBancoJulho);

            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new FaturaRepository(dbContext);
            var resultado = await repositorio.ListarFaturasCartaoPorVencimentoNoMes(2026, 7);

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
    public async Task ListarFaturasCartaoPorVencimentoNoMes_LancamentosETransferenciasCarregados()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar Conta Cartao
            var contaCartaoId = Guid.NewGuid();
            var contaCartao = new Conta
            {
                Id = contaCartaoId,
                Nome = "Cartao Credito",
                Tipo = TipoConta.Cartao,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(contaCartao);

            // Criar Conta Destino para Transferencia
            var contaDestinoId = Guid.NewGuid();
            var contaDestino = new Conta
            {
                Id = contaDestinoId,
                Nome = "Conta Destino",
                Tipo = TipoConta.Banco,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(contaDestino);

            // Criar Fatura de Cartao
            var faturaCartao = new Fatura
            {
                Id = Guid.NewGuid(),
                ContaId = contaCartaoId,
                DataFechamento = new DateOnly(2026, 6, 30),
                DataVencimento = new DateOnly(2026, 7, 15),
                Status = StatusFatura.Fechada
            };
            dbContext.Faturas.Add(faturaCartao);

            // Criar Lancamento vinculado a Fatura
            var lancamento = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaCartaoId,
                FaturaId = faturaCartao.Id,
                Tipo = TipoLancamento.Debit,
                Status = StatusLancamento.Pendente,
                Valor = 100m,
                Data = new DateOnly(2026, 7, 5),
                Manual = true,
                Oculto = false
            };
            dbContext.Lancamentos.Add(lancamento);

            // Criar Transferencia vinculada a Fatura
            var transferencia = new Transferencia
            {
                Id = Guid.NewGuid(),
                FaturaId = faturaCartao.Id,
                ContaOrigemId = contaCartaoId,
                ContaDestinoId = contaDestinoId
            };
            dbContext.Transferencias.Add(transferencia);

            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new FaturaRepository(dbContext);
            var resultado = await repositorio.ListarFaturasCartaoPorVencimentoNoMes(2026, 7);

            // Assert
            Assert.NotEmpty(resultado);
            var fatura = resultado.Single();

            // Verificar que Lancamentos e Transferencias foram carregados
            Assert.NotNull(fatura.Lancamentos);
            Assert.NotEmpty(fatura.Lancamentos);
            Assert.Single(fatura.Lancamentos);
            Assert.Equal(100m, fatura.Lancamentos.First().Valor);

            Assert.NotNull(fatura.Transferencias);
            Assert.NotEmpty(fatura.Transferencias);
            Assert.Single(fatura.Transferencias);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ListarFaturasCartaoPorVencimentoNoMes_MultiplasFaturasCartaoNoMes_RetornaTodasDoMes()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar Conta Cartao
            var contaCartaoId = Guid.NewGuid();
            var contaCartao = new Conta
            {
                Id = contaCartaoId,
                Nome = "Cartao Credito",
                Tipo = TipoConta.Cartao,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(contaCartao);

            // Criar segunda Conta Cartao para evitar constraint UNIQUE de (conta_id, status)
            var contaCartao2Id = Guid.NewGuid();
            var contaCartao2 = new Conta
            {
                Id = contaCartao2Id,
                Nome = "Cartao Credito 2",
                Tipo = TipoConta.Cartao,
                Origem = OrigemConta.Manual,
                Ativa = true
            };
            dbContext.Contas.Add(contaCartao2);

            // Criar primeira Fatura de Cartao em julho de 2026
            var faturaCartaoJulho1 = new Fatura
            {
                Id = Guid.NewGuid(),
                ContaId = contaCartaoId,
                DataFechamento = new DateOnly(2026, 6, 1),
                DataVencimento = new DateOnly(2026, 7, 5),
                Status = StatusFatura.Aberta
            };
            dbContext.Faturas.Add(faturaCartaoJulho1);

            // Criar segunda Fatura de Cartao em julho de 2026 (de outra conta)
            var faturaCartaoJulho2 = new Fatura
            {
                Id = Guid.NewGuid(),
                ContaId = contaCartao2Id,
                DataFechamento = new DateOnly(2026, 6, 15),
                DataVencimento = new DateOnly(2026, 7, 20),
                Status = StatusFatura.Fechada
            };
            dbContext.Faturas.Add(faturaCartaoJulho2);

            // Criar Fatura de Cartao em agosto de 2026 (deve ser ignorada)
            var faturaCartaoAgosto = new Fatura
            {
                Id = Guid.NewGuid(),
                ContaId = contaCartaoId,
                DataFechamento = new DateOnly(2026, 7, 15),
                DataVencimento = new DateOnly(2026, 8, 10),
                Status = StatusFatura.Fechada
            };
            dbContext.Faturas.Add(faturaCartaoAgosto);

            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new FaturaRepository(dbContext);
            var resultado = await repositorio.ListarFaturasCartaoPorVencimentoNoMes(2026, 7);

            // Assert
            Assert.Equal(2, resultado.Count());
            var ids = resultado.Select(f => f.Id).OrderBy(id => id).ToList();
            Assert.Contains(faturaCartaoJulho1.Id, ids);
            Assert.Contains(faturaCartaoJulho2.Id, ids);
            Assert.DoesNotContain(faturaCartaoAgosto.Id, ids);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }
}
