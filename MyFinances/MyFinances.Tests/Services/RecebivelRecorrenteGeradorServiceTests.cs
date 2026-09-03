using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class RecebivelRecorrenteGeradorServiceTests
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

    #region MaterializarOcorrenciasAsync: cria ContaReceber da janela, retorna contagem

    [Fact]
    public async Task MaterializarOcorrenciasAsync_Mensal_CriaOcorrenciasDoIntervalo()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var moldeId = Guid.NewGuid();
            var molde = new RecebivelRecorrente
            {
                Id = moldeId,
                Descricao = "Receita mensal",
                Valor = 1000m,
                Periodicidade = PeriodicidadeRecebivel.Mensal,
                DiaVencimento = 10,
                CategoriaId = null,
                Ativa = true
            };
            dbContext.RecebiveisRecorrentes.Add(molde);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            var dataReferencia = new DateOnly(2026, 8, 15);
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);

            // Act
            var criadasCount = await geradorService.MaterializarOcorrenciasAsync(moldeId, dataReferencia);

            // Assert - deve ter criado as ocorrencias de agosto, setembro, outubro de 2026
            var contasNoDb = await repositorioConta.Listar();
            var contasMolde = contasNoDb.Where(c => c.RecebivelRecorrenteId == moldeId).ToList();

            // Datas esperadas: com DiaVencimento=10 e dataReferencia=2026-08-15, janela = [2026-08-01, 2026-11-13]
            var datasEsperadas = new[]
            {
                new DateOnly(2026, 8, 10),
                new DateOnly(2026, 9, 10),
                new DateOnly(2026, 10, 10),
                new DateOnly(2026, 11, 10)
            };

            Assert.Equal(datasEsperadas.Length, contasMolde.Count);
            foreach (var dataEsperada in datasEsperadas)
            {
                var contaComData = contasMolde.FirstOrDefault(c => c.DataPrevista == dataEsperada);
                Assert.NotNull(contaComData);
                Assert.Equal(dataEsperada, contaComData.DataPrevista);
            }
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion

    #region IDEMPOTENCIA: rodar 2x nao duplica

    [Fact]
    public async Task MaterializarOcorrenciasAsync_Idempotencia_SegundaRodaRetorna0()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var moldeId = Guid.NewGuid();
            var molde = new RecebivelRecorrente
            {
                Id = moldeId,
                Descricao = "Receita mensal",
                Valor = 1000m,
                Periodicidade = PeriodicidadeRecebivel.Mensal,
                DiaVencimento = 10,
                CategoriaId = null,
                Ativa = true
            };
            dbContext.RecebiveisRecorrentes.Add(molde);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            var dataReferencia = new DateOnly(2026, 8, 15);
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);

            // Act - primeira vez
            var primeiraRodaCriadas = await geradorService.MaterializarOcorrenciasAsync(moldeId, dataReferencia);
            var countAposRoda1 = (await repositorioConta.Listar()).Count();

            // Act - segunda vez
            var segundaRodaCriadas = await geradorService.MaterializarOcorrenciasAsync(moldeId, dataReferencia);
            var countAposRoda2 = (await repositorioConta.Listar()).Count();

            // Assert
            Assert.True(primeiraRodaCriadas > 0);
            Assert.Equal(0, segundaRodaCriadas); // Nao deve criar nada segunda vez
            Assert.Equal(countAposRoda1, countAposRoda2); // Count igual antes e depois
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion

    #region Janela: nao ultrapassa 90 dias, mas cria mes corrente anterior a dataReferencia

    [Fact]
    public async Task MaterializarOcorrenciasAsync_Janela_NaoCriaAlemDe90Dias()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var moldeId = Guid.NewGuid();
            var molde = new RecebivelRecorrente
            {
                Id = moldeId,
                Descricao = "Receita mensal",
                Valor = 1000m,
                Periodicidade = PeriodicidadeRecebivel.Mensal,
                DiaVencimento = 10,
                CategoriaId = null,
                Ativa = true
            };
            dbContext.RecebiveisRecorrentes.Add(molde);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            var dataReferencia = new DateOnly(2026, 8, 15);
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);

            // Act
            await geradorService.MaterializarOcorrenciasAsync(moldeId, dataReferencia);
            var contasNoDb = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == moldeId).ToList();

            // Assert - nenhuma data deve ser > dataReferencia + 90 dias
            var limite = dataReferencia.AddDays(90);
            foreach (var conta in contasNoDb)
            {
                if (conta.DataPrevista.HasValue)
                {
                    Assert.True(conta.DataPrevista.Value <= limite,
                        $"Data {conta.DataPrevista} ultrapassa o limite de {limite}");
                }
            }
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion

    #region RemoverOcorrenciasPendentesAsync: deleta apenas PENDENTE

    [Fact]
    public async Task RemoverOcorrenciasPendentesAsync_DeletaSoStatus_PendentePermanecemOutros()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var moldeId = Guid.NewGuid();
            var molde = new RecebivelRecorrente
            {
                Id = moldeId,
                Descricao = "Receita mensal",
                Valor = 1000m,
                Periodicidade = PeriodicidadeRecebivel.Mensal,
                DiaVencimento = 10,
                CategoriaId = null,
                Ativa = true
            };
            dbContext.RecebiveisRecorrentes.Add(molde);

            // Criar ContaReceber com varios status
            var contaPendente = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Pendente",
                ValorTotal = 1000m,
                DataRegistro = new DateOnly(2026, 8, 1),
                DataPrevista = new DateOnly(2026, 8, 10),
                Status = StatusContaReceber.Pendente,
                RecebivelRecorrenteId = moldeId
            };

            var contaParcial = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Parcial",
                ValorTotal = 1000m,
                DataRegistro = new DateOnly(2026, 8, 1),
                DataPrevista = new DateOnly(2026, 8, 20),
                Status = StatusContaReceber.Parcial,
                RecebivelRecorrenteId = moldeId
            };

            var contaRecebida = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Recebida",
                ValorTotal = 1000m,
                DataRegistro = new DateOnly(2026, 8, 1),
                DataPrevista = new DateOnly(2026, 8, 30),
                Status = StatusContaReceber.Recebido,
                RecebivelRecorrenteId = moldeId
            };

            dbContext.ContasReceber.AddRange(contaPendente, contaParcial, contaRecebida);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);

            // Act
            var removidas = await geradorService.RemoverOcorrenciasPendentesAsync(moldeId);

            // Assert
            Assert.Equal(1, removidas); // Apenas 1 Pendente
            var contasAposRemocao = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == moldeId).ToList();
            Assert.Equal(2, contasAposRemocao.Count); // Parcial + Recebida permanecem
            Assert.Contains(contaParcial.Id, contasAposRemocao.Select(c => c.Id));
            Assert.Contains(contaRecebida.Id, contasAposRemocao.Select(c => c.Id));
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion

    #region RegenerarOcorrenciasAsync: muda periodicidade, deleta fora do novo conjunto

    [Fact]
    public async Task RegenerarOcorrenciasAsync_MudaPeriodicidade_DeletaForaDaPendentesNovos()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var moldeId = Guid.NewGuid();
            var molde = new RecebivelRecorrente
            {
                Id = moldeId,
                Descricao = "Receita",
                Valor = 1000m,
                Periodicidade = PeriodicidadeRecebivel.Mensal,
                DiaVencimento = 10,
                CategoriaId = null,
                Ativa = true
            };
            dbContext.RecebiveisRecorrentes.Add(molde);

            // Criar ContasReceber sob periodicidade MENSAL
            var contaMensal1 = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Agosto",
                ValorTotal = 1000m,
                DataRegistro = new DateOnly(2026, 8, 1),
                DataPrevista = new DateOnly(2026, 8, 10),
                Status = StatusContaReceber.Pendente,
                RecebivelRecorrenteId = moldeId
            };

            var contaMensal2 = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Setembro",
                ValorTotal = 1000m,
                DataRegistro = new DateOnly(2026, 8, 1),
                DataPrevista = new DateOnly(2026, 9, 10),
                Status = StatusContaReceber.Pendente,
                RecebivelRecorrenteId = moldeId
            };

            dbContext.ContasReceber.AddRange(contaMensal1, contaMensal2);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            // Mudamos o molde para ANUAL
            molde.Periodicidade = PeriodicidadeRecebivel.Anual;
            molde.MesReferencia = 8;
            dbContext.RecebiveisRecorrentes.Update(molde);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);

            // Act
            var dataReferencia = new DateOnly(2026, 8, 15);
            await geradorService.RegenerarOcorrenciasAsync(moldeId, dataReferencia);

            // Assert
            var contasAposRegenacao = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == moldeId).ToList();
            // Deve ter deletado a de setembro (nao faz parte do novo conjunto ANUAL mes=8)
            // Deve ter mantido/recriado a de agosto (faz parte do novo conjunto ANUAL)

            // A conta PENDENTE de setembro (2026-09-10) nao deve mais existir
            var contaSetembro = contasAposRegenacao.FirstOrDefault(c => c.DataPrevista.HasValue && c.DataPrevista.Value == new DateOnly(2026, 9, 10) && c.Status == StatusContaReceber.Pendente);
            Assert.Null(contaSetembro);

            // A conta de agosto (2026-08-10) deve continuar existindo com status Pendente (ou ter sido recriada)
            var contaAgosto = contasAposRegenacao.FirstOrDefault(c => c.DataPrevista.HasValue && c.DataPrevista.Value.Year == 2026 && c.DataPrevista.Value.Month == 8 && c.Status == StatusContaReceber.Pendente);
            Assert.NotNull(contaAgosto);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion

    #region Janela ANUAL distante: proxima ocorrencia alem de 90 dias ainda e materializada

    [Fact]
    public async Task MaterializarOcorrenciasAsync_AnualDistanteAlem90Dias_MatSerializaProximaOcorrencia()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var moldeId = Guid.NewGuid();
            // Molde ANUAL: proxima ocorrencia = 2027-03-10
            // dataReferencia = 2026-08-15
            // Diferenca = ~207 dias (alem dos 90 dias de lookahead normal)
            var molde = new RecebivelRecorrente
            {
                Id = moldeId,
                Descricao = "Receita anual distante",
                Valor = 2000m,
                Periodicidade = PeriodicidadeRecebivel.Anual,
                DiaVencimento = 10,
                MesReferencia = 3,  // Marco
                CategoriaId = null,
                Ativa = true
            };
            dbContext.RecebiveisRecorrentes.Add(molde);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            var dataReferencia = new DateOnly(2026, 8, 15);
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);

            // Act
            var criadasCount = await geradorService.MaterializarOcorrenciasAsync(moldeId, dataReferencia);

            // Assert - deve ter criado EXATAMENTE 1 ocorrencia (a proxima: 2027-03-10)
            Assert.Equal(1, criadasCount);

            var contasNoDb = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == moldeId).ToList();
            Assert.Single(contasNoDb);

            var contaCriada = contasNoDb.First();
            Assert.Equal(new DateOnly(2027, 3, 10), contaCriada.DataPrevista);
            Assert.Equal(StatusContaReceber.Pendente, contaCriada.Status);
            Assert.Equal(2000m, contaCriada.ValorTotal);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion

    #region MaterializarTodosAtivosAsync: materializa so os com Ativa=true

    [Fact]
    public async Task MaterializarTodosAtivosAsync_IgnoraMoldesInativos()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var moldeAtivoId = Guid.NewGuid();
            var moldeInativoId = Guid.NewGuid();

            var moldeAtivo = new RecebivelRecorrente
            {
                Id = moldeAtivoId,
                Descricao = "Ativo",
                Valor = 1000m,
                Periodicidade = PeriodicidadeRecebivel.Mensal,
                DiaVencimento = 10,
                CategoriaId = null,
                Ativa = true
            };

            var moldeInativo = new RecebivelRecorrente
            {
                Id = moldeInativoId,
                Descricao = "Inativo",
                Valor = 1000m,
                Periodicidade = PeriodicidadeRecebivel.Mensal,
                DiaVencimento = 10,
                CategoriaId = null,
                Ativa = false
            };

            dbContext.RecebiveisRecorrentes.AddRange(moldeAtivo, moldeInativo);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);

            // Act
            var dataReferencia = new DateOnly(2026, 8, 15);
            await geradorService.MaterializarTodosAtivosAsync(dataReferencia);

            // Assert - nao deve ter criado nada para moldeInativo
            var contasNoDb = (await repositorioConta.Listar()).ToList();
            var contasInativo = contasNoDb.Where(c => c.RecebivelRecorrenteId == moldeInativoId).ToList();
            Assert.Empty(contasInativo);

            // Deve ter criado >= 1 ocorrencia para moldeAtivo
            var contasAtivo = contasNoDb.Where(c => c.RecebivelRecorrenteId == moldeAtivoId).ToList();
            Assert.NotEmpty(contasAtivo);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion
}
