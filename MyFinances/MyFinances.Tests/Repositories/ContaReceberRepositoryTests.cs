using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using Xunit;

namespace MyFinances.Tests.Repositories;

public class ContaReceberRepositoryTests
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
    public async Task ListarParaProjecaoDoMes_ContaParcialDeMesAnterior_RetornaContaParaProjecao()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar Conta para Lancamento
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

            // Criar ContaReceber Status=PARCIAL de mes anterior
            var contaReceberParcial = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Recebivel mes anterior",
                Pessoa = null,
                ValorTotal = 1000m,
                DataRegistro = new DateOnly(2026, 6, 1),
                DataPrevista = new DateOnly(2026, 6, 30),
                Status = StatusContaReceber.Parcial
            };
            dbContext.ContasReceber.Add(contaReceberParcial);
            await dbContext.SaveChangesAsync();

            // Criar Lancamento (recebimento parcial de 400)
            var lancamento = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                ContaReceberId = contaReceberParcial.Id,
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 400m,
                Data = new DateOnly(2026, 6, 15),
                Manual = false,
                Oculto = false
            };
            dbContext.Lancamentos.Add(lancamento);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new ContaReceberRepository(dbContext);
            var resultado = await repositorio.ListarParaProjecaoDoMes(2026, 7);

            // Assert
            Assert.NotEmpty(resultado);
            var contaParaProjecao = resultado.Single(cr => cr.Id == contaReceberParcial.Id);
            Assert.Equal(StatusContaReceber.Parcial, contaParaProjecao.Status);
            Assert.Equal(1000m, contaParaProjecao.ValorTotal);

            Assert.NotEmpty(contaParaProjecao.Recebimentos);
            Assert.Single(contaParaProjecao.Recebimentos);
            Assert.Equal(400m, contaParaProjecao.Recebimentos.First().Valor);

            var saldo = ContaReceberSaldoCalculator.Calcular(contaParaProjecao);
            Assert.Equal(600m, saldo.SaldoPendente);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ListarParaProjecaoDoMes_ContaPendenteDentroDoMes_RetornaContaParaProjecao()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar Conta
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

            // Criar ContaReceber Status=PENDENTE com DataPrevista DENTRO do mes
            var contaReceberPendente = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Recebivel dentro do mes",
                Pessoa = null,
                ValorTotal = 500m,
                DataRegistro = new DateOnly(2026, 7, 1),
                DataPrevista = new DateOnly(2026, 7, 15),
                Status = StatusContaReceber.Pendente
            };
            dbContext.ContasReceber.Add(contaReceberPendente);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new ContaReceberRepository(dbContext);
            var resultado = await repositorio.ListarParaProjecaoDoMes(2026, 7);

            // Assert
            Assert.NotEmpty(resultado);
            var contaParaProjecao = resultado.Single(cr => cr.Id == contaReceberPendente.Id);
            Assert.Equal(StatusContaReceber.Pendente, contaParaProjecao.Status);
            Assert.Equal(500m, contaParaProjecao.ValorTotal);

            Assert.Empty(contaParaProjecao.Recebimentos);

            var saldo = ContaReceberSaldoCalculator.Calcular(contaParaProjecao);
            Assert.Equal(500m, saldo.SaldoPendente);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ListarParaProjecaoDoMes_ContaPendenteForaDoMes_NaoRetornaContaParaProjecao()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar ContaReceber Status=PENDENTE com DataPrevista FORA do mes
            var contaReceberPendenteFora = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Recebivel fora do mes",
                Pessoa = null,
                ValorTotal = 300m,
                DataRegistro = new DateOnly(2026, 7, 1),
                DataPrevista = new DateOnly(2026, 8, 15),
                Status = StatusContaReceber.Pendente
            };
            dbContext.ContasReceber.Add(contaReceberPendenteFora);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new ContaReceberRepository(dbContext);
            var resultado = await repositorio.ListarParaProjecaoDoMes(2026, 7);

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
    public async Task ListarParaProjecaoDoMes_ContaRecebido_NaoRetornaContaParaProjecao()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar Conta
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

            // Criar ContaReceber Status=RECEBIDO
            var contaReceberRecebida = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Recebivel recebido",
                Pessoa = null,
                ValorTotal = 1000m,
                DataRegistro = new DateOnly(2026, 7, 1),
                DataPrevista = new DateOnly(2026, 7, 15),
                Status = StatusContaReceber.Recebido
            };
            dbContext.ContasReceber.Add(contaReceberRecebida);
            await dbContext.SaveChangesAsync();

            // Criar Lancamento para deixar saldo = 0 (status RECEBIDO)
            var lancamento = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                ContaReceberId = contaReceberRecebida.Id,
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 1000m,
                Data = new DateOnly(2026, 7, 15),
                Manual = false,
                Oculto = false
            };
            dbContext.Lancamentos.Add(lancamento);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new ContaReceberRepository(dbContext);
            var resultado = await repositorio.ListarParaProjecaoDoMes(2026, 7);

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
    public async Task ListarParaProjecaoDoMes_IncludeRecebimentosCarregaLancamentos_SaldoCalculadoCorreto()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar Conta
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

            // Criar ContaReceber com recebimentos
            var contaReceberComRecebimentos = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Recebivel com recebimentos",
                Pessoa = null,
                ValorTotal = 2000m,
                DataRegistro = new DateOnly(2026, 7, 1),
                DataPrevista = new DateOnly(2026, 7, 31),
                Status = StatusContaReceber.Parcial
            };
            dbContext.ContasReceber.Add(contaReceberComRecebimentos);
            await dbContext.SaveChangesAsync();

            // Criar multiplos recebimentos
            var lancamento1 = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                ContaReceberId = contaReceberComRecebimentos.Id,
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 600m,
                Data = new DateOnly(2026, 7, 10),
                Manual = false,
                Oculto = false
            };

            var lancamento2 = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                ContaReceberId = contaReceberComRecebimentos.Id,
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 800m,
                Data = new DateOnly(2026, 7, 20),
                Manual = false,
                Oculto = false
            };

            dbContext.Lancamentos.Add(lancamento1);
            dbContext.Lancamentos.Add(lancamento2);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new ContaReceberRepository(dbContext);
            var resultado = await repositorio.ListarParaProjecaoDoMes(2026, 7);

            // Assert
            Assert.NotEmpty(resultado);
            var contaParaProjecao = resultado.Single(cr => cr.Id == contaReceberComRecebimentos.Id);
            Assert.NotEmpty(contaParaProjecao.Recebimentos);
            Assert.Equal(2, contaParaProjecao.Recebimentos.Count);

            var valores = contaParaProjecao.Recebimentos.Select(l => l.Valor).OrderBy(v => v).ToList();
            Assert.Equal(new[] { 600m, 800m }, valores);

            var saldo = ContaReceberSaldoCalculator.Calcular(contaParaProjecao);
            Assert.Equal(2000m, saldo.ValorTotal);
            Assert.Equal(1400m, saldo.ValorRecebido);
            Assert.Equal(600m, saldo.SaldoPendente);
            Assert.Equal(StatusContaReceber.Parcial, saldo.Status);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ListarParaProjecaoDoMes_MixtoDeParcialEPendenteDentroDoMes_RetornaAmbas()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            // Criar Conta
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

            // Criar ContaReceber Status=PARCIAL
            var contaParcial = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Parcial",
                Pessoa = null,
                ValorTotal = 1000m,
                DataRegistro = new DateOnly(2026, 6, 1),
                DataPrevista = new DateOnly(2026, 6, 30),
                Status = StatusContaReceber.Parcial
            };
            dbContext.ContasReceber.Add(contaParcial);

            // Criar ContaReceber Status=PENDENTE dentro do mes
            var contaPendente = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Pendente",
                Pessoa = null,
                ValorTotal = 500m,
                DataRegistro = new DateOnly(2026, 7, 1),
                DataPrevista = new DateOnly(2026, 7, 15),
                Status = StatusContaReceber.Pendente
            };
            dbContext.ContasReceber.Add(contaPendente);

            await dbContext.SaveChangesAsync();

            // Criar recebimento para PARCIAL
            var lancamento = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                ContaReceberId = contaParcial.Id,
                Tipo = TipoLancamento.Credit,
                Status = StatusLancamento.Pago,
                Valor = 300m,
                Data = new DateOnly(2026, 6, 15),
                Manual = false,
                Oculto = false
            };
            dbContext.Lancamentos.Add(lancamento);
            await dbContext.SaveChangesAsync();

            dbContext.ChangeTracker.Clear();

            // Act
            var repositorio = new ContaReceberRepository(dbContext);
            var resultado = await repositorio.ListarParaProjecaoDoMes(2026, 7);

            // Assert
            Assert.NotEmpty(resultado);
            Assert.Equal(2, resultado.Count());

            var resParcial = resultado.Single(cr => cr.Id == contaParcial.Id);
            var resPendente = resultado.Single(cr => cr.Id == contaPendente.Id);

            var saldoParcial = ContaReceberSaldoCalculator.Calcular(resParcial);
            Assert.Equal(700m, saldoParcial.SaldoPendente);

            var saldoPendenteResult = ContaReceberSaldoCalculator.Calcular(resPendente);
            Assert.Equal(500m, saldoPendenteResult.SaldoPendente);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }
}
