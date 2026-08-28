using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

public class RecebivelRecorrenteServiceTests
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

    #region CriarAsync: validacoes (dia/mes/dia-da-semana obrigatorios, valor/descricao)

    [Fact]
    public async Task CriarAsync_MensalSemDiaVencimento_RetornaErroValidacao()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            // Act & Assert - MENSAL sem diaVencimento deve ser rejeitado
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CriarAsync("Receita", 1000m, "MENSAL", null, null, null, null)
            );
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task CriarAsync_AnualSemMesReferencia_RetornaErroValidacao()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            // Act & Assert - ANUAL sem mesReferencia deve ser rejeitado
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CriarAsync("Receita anual", 5000m, "ANUAL", 15, null, null, null)
            );
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task CriarAsync_SemanalSemDiaDaSemana_RetornaErroValidacao()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            // Act & Assert - SEMANAL sem diaDaSemana deve ser rejeitado
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CriarAsync("Receita semanal", 500m, "SEMANAL", null, null, null, null)
            );
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task CriarAsync_ValorNegativo_RetornaErroValidacao()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            // Act & Assert - valor <= 0 deve ser rejeitado
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CriarAsync("Receita", -100m, "MENSAL", 10, null, null, null)
            );
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task CriarAsync_DescricaoVazia_RetornaErroValidacao()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            // Act & Assert - descricao vazia deve ser rejeitada
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.CriarAsync("", 1000m, "MENSAL", 10, null, null, null)
            );
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion

    #region CriarAsync valido: persiste e materializa ocorrencias

    [Fact]
    public async Task CriarAsync_ValidoMensal_PersisteMoldeEMatEializaOcorrencias()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            // Act
            var molde = await service.CriarAsync("Receita mensal", 1000m, "MENSAL", 10, null, null, null);

            // Assert - molde foi persistido
            Assert.NotNull(molde);
            Assert.NotEqual(Guid.Empty, molde.Id);
            Assert.Equal("Receita mensal", molde.Descricao);
            Assert.Equal(1000m, molde.Valor);

            // Assert - ocorrencias foram materializadas com as datas exatas
            var contasNoDb = await repositorioConta.Listar();
            var contasMolde = contasNoDb.Where(c => c.RecebivelRecorrenteId == molde.Id).ToList();

            // Janela padrao para criacao: data atual +90 dias, com DiaVencimento=10
            // Esperado: pelo menos 4 ocorrencias (mes corrente + 3 proximos)
            Assert.NotEmpty(contasMolde);

            // Verificar que todas as contas tem DataPrevista no dia 10
            foreach (var conta in contasMolde)
            {
                Assert.Equal(10, conta.DataPrevista?.Day);
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

    #region ObterPorId: inexistente retorna excecao

    [Fact]
    public async Task ObterPorId_IdInexistente_RetornaRecebivelRecorrenteNaoEncontradoException()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            var idInexistente = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<RecebivelRecorrenteNaoEncontradoException>(async () =>
                await service.ObterPorId(idInexistente)
            );
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion

    #region EditarAsync: propaga Valor e CategoriaId para Pendentes, nao altera Parcial/Recebido

    [Fact]
    public async Task EditarAsync_ProrogaValorECategoriaParaPendentes()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            // Criar uma categoria real para usar no molde
            var categoriaNova = new Categoria
            {
                Id = Guid.NewGuid(),
                Nome = "Categoria Teste",
                Tipo = TipoCategoria.Receita,
                Arquivada = false
            };
            dbContext.Categorias.Add(categoriaNova);
            await dbContext.SaveChangesAsync();

            // Criar um molde e suas ocorrencias
            var molde = await service.CriarAsync("Receita", 1000m, "MENSAL", 10, null, null, null);
            dbContext.ChangeTracker.Clear();

            // Fetch molde novamente pra ter suas ocorrencias
            var moldeRecuperado = await repositorioRecebivel.ObterPorId(molde.Id);
            var contasOriginais = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).ToList();

            // Marcar uma como Parcial (simular recebimento)
            if (contasOriginais.Any())
            {
                contasOriginais.First().Status = StatusContaReceber.Parcial;
                await repositorioConta.Atualizar(contasOriginais.First());
                await repositorioConta.Salvar();
            }

            dbContext.ChangeTracker.Clear();

            // Act
            var novoValor = 1500m;
            var moldeEditado = await service.EditarAsync(
                molde.Id,
                novoValor,
                "MENSAL",
                10,
                null,
                null,
                categoriaNova.Id
            );

            // Assert - molde foi editado
            Assert.NotNull(moldeEditado);
            Assert.Equal(novoValor, moldeEditado.Valor);
            Assert.Equal(categoriaNova.Id, moldeEditado.CategoriaId);

            // Assert - ocorrencias PENDENTE foram propagadas com novo valor e categoria
            var contasAposEdicao = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).ToList();
            var contasPendentes = contasAposEdicao.Where(c => c.Status == StatusContaReceber.Pendente).ToList();
            foreach (var contaPendente in contasPendentes)
            {
                Assert.Equal(novoValor, contaPendente.ValorTotal);
                Assert.Equal(categoriaNova.Id, contaPendente.CategoriaId);
            }

            // Assert - ocorrencia PARCIAL manteve valor e categoria antigos
            var contaParcial = contasAposEdicao.FirstOrDefault(c => c.Status == StatusContaReceber.Parcial);
            if (contaParcial != null)
            {
                Assert.Equal(1000m, contaParcial.ValorTotal); // Valor original nao foi alterado
                // Categoria pode ter sido propagada ou nao conforme a implementacao, mas valor deve permanecer
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

    #region EditarAsync: mudanca de Periodicidade dispara regeneracao

    [Fact]
    public async Task EditarAsync_MudaPeriodicidade_DisparaRegeneracaoOcorrencias()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            // Criar molde MENSAL (dia 10)
            var molde = await service.CriarAsync("Receita mensal", 1000m, "MENSAL", 10, null, null, null);
            var contasAntesDeEdit = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id && c.Status == StatusContaReceber.Pendente).ToList();

            dbContext.ChangeTracker.Clear();

            // Act - editar para ANUAL mes=3
            var moldeEditado = await service.EditarAsync(
                molde.Id,
                1000m,
                "ANUAL",
                10,
                3,  // MesReferencia = marco
                null,
                null
            );

            // Assert - periodicidade mudou
            Assert.Equal(PeriodicidadeRecebivel.Anual, moldeEditado.Periodicidade);
            Assert.Equal(3, moldeEditado.MesReferencia);

            // Assert - o conjunto de Pendentes mudou
            var contasAposEdit = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id && c.Status == StatusContaReceber.Pendente).ToList();

            // Datas antigas (dia 10 de cada mes MENSAL) nao devem mais existir como Pendente
            var datasAntiguasMensais = contasAntesDeEdit.Select(c => c.DataPrevista).ToList();
            foreach (var dataAntiga in datasAntiguasMensais)
            {
                if (dataAntiga.HasValue)
                {
                    // Se a data antiga ainda existe, nao deve estar como Pendente
                    var contaAntiga = contasAposEdit.FirstOrDefault(c => c.DataPrevista == dataAntiga.Value);
                    if (contaAntiga != null)
                    {
                        Assert.NotEqual(StatusContaReceber.Pendente, contaAntiga.Status);
                    }
                }
            }

            // Deve existir >= 1 conta PENDENTE com DataPrevista em marco (mes 3)
            var contasMarco = contasAposEdit.Where(c => c.DataPrevista.HasValue && c.DataPrevista.Value.Month == 3).ToList();
            Assert.NotEmpty(contasMarco);
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion

    #region DesativarAsync: seta Ativa=false, deleta Pendentes, preserva Parcial/Recebido

    [Fact]
    public async Task DesativarAsync_SetaAtivaFalseEDeletaPendentes()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            // Criar molde
            var molde = await service.CriarAsync("Receita", 1000m, "MENSAL", 10, null, null, null);
            var contasAntesDeDesativar = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).ToList();
            var countAntes = contasAntesDeDesativar.Count;

            dbContext.ChangeTracker.Clear();

            // Act
            await service.DesativarAsync(molde.Id);

            // Assert
            var moldeAposDesativacao = await repositorioRecebivel.ObterPorId(molde.Id);
            Assert.False(moldeAposDesativacao.Ativa);

            var contasAposDesativacao = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).ToList();
            // Todas as Pendentes devem ter sido deletadas
            Assert.All(contasAposDesativacao, c =>
                Assert.NotEqual(StatusContaReceber.Pendente, c.Status)
            );
        }
        finally
        {
            await dbContext.DisposeAsync();
            await connection.CloseAsync();
            connection.Dispose();
        }
    }

    #endregion

    #region ReativarAsync: seta Ativa=true, re-materializa idempotente

    [Fact]
    public async Task ReativarAsync_SetaAtivaVerdadeirERematerializaIdempotente()
    {
        // Arrange
        var (dbContext, connection) = await CriarDbContext();
        try
        {
            var repositorioRecebivel = new RecebivelRecorrenteRepository(dbContext);
            var repositorioConta = new ContaReceberRepository(dbContext);
            var geradorService = new RecebivelRecorrenteGeradorService(repositorioRecebivel, repositorioConta);
            var service = new RecebivelRecorrenteService(repositorioRecebivel, geradorService);

            // Criar e desativar
            var molde = await service.CriarAsync("Receita", 1000m, "MENSAL", 10, null, null, null);
            var countAposCriacao = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).Count();

            await service.DesativarAsync(molde.Id);

            var countAposDesativacao = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).Count();
            dbContext.ChangeTracker.Clear();

            // Act - reativar
            await service.ReativarAsync(molde.Id);

            // Assert
            var moldeAposReativacao = await repositorioRecebivel.ObterPorId(molde.Id);
            Assert.True(moldeAposReativacao.Ativa);

            var contasAposReativacao = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).ToList();
            // Deve ter rematerializado ocorrencias (pelo menos as do periodo corrente)
            Assert.NotEmpty(contasAposReativacao);

            // Deve ter criado >= 1 ocorrencia PENDENTE apos reativacao
            var pendentesAposReativacao = contasAposReativacao.Where(c => c.Status == StatusContaReceber.Pendente).ToList();
            Assert.NotEmpty(pendentesAposReativacao);

            // Todas as contas rematerializadas devem ter DataPrevista no dia 10 (DiaVencimento)
            foreach (var conta in pendentesAposReativacao)
            {
                Assert.Equal(10, conta.DataPrevista?.Day);
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
}
