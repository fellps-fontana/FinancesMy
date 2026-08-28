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

            // Criar 2 categorias reais (categoria antiga e categoria nova)
            var categoriaAntiga = new Categoria
            {
                Id = Guid.NewGuid(),
                Nome = "Categoria Antiga",
                Tipo = TipoCategoria.Receita,
                Arquivada = false
            };
            var categoriaNova = new Categoria
            {
                Id = Guid.NewGuid(),
                Nome = "Categoria Nova",
                Tipo = TipoCategoria.Receita,
                Arquivada = false
            };
            dbContext.Categorias.AddRange(categoriaAntiga, categoriaNova);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            // Criar molde MENSAL com categoria antiga e valor 1000m
            var molde = await service.CriarAsync("Receita mensal", 1000m, "MENSAL", 10, null, null, categoriaAntiga.Id);
            dbContext.ChangeTracker.Clear();

            // Pegar as ocorrencias geradas
            var contasGerais = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).ToList();
            Assert.NotEmpty(contasGerais);

            // Forcar EXATAMENTE 2 ocorrencias com status diferente:
            // - 1ª ocorrencia: PARCIAL (valor e categoria antigos, imutaveis)
            // - 2ª ocorrencia: RECEBIDO (valor e categoria antigos, imutaveis)
            // - resto: PENDENTE (propagara valores novos)
            var contasTodasExceto2 = contasGerais.Skip(2).ToList();
            contasTodasExceto2.ForEach(c => dbContext.Entry(c).State = Microsoft.EntityFrameworkCore.EntityState.Detached);

            var contaParcial = contasGerais[0];
            var contaRecebida = contasGerais[1];

            contaParcial.Status = StatusContaReceber.Parcial;
            contaParcial.ValorTotal = 1000m;
            contaParcial.CategoriaId = categoriaAntiga.Id;

            contaRecebida.Status = StatusContaReceber.Recebido;
            contaRecebida.ValorTotal = 1000m;
            contaRecebida.CategoriaId = categoriaAntiga.Id;

            await repositorioConta.Atualizar(contaParcial);
            await repositorioConta.Atualizar(contaRecebida);
            await repositorioConta.Salvar();
            dbContext.ChangeTracker.Clear();

            // Act - editar molde para novo valor e nova categoria
            var novoValor = 1500m;
            await service.EditarAsync(
                molde.Id,
                novoValor,
                "MENSAL",
                10,
                null,
                null,
                categoriaNova.Id
            );
            dbContext.ChangeTracker.Clear();

            // Assert - buscar contasReceber do banco apos edicao
            var contasAposEdicao = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).ToList();

            // Assert: TODAS as ocorrencias PENDENTE tem novo valor E nova categoria
            var contasPendentes = contasAposEdicao.Where(c => c.Status == StatusContaReceber.Pendente).ToList();
            Assert.NotEmpty(contasPendentes);
            foreach (var contaPendente in contasPendentes)
            {
                Assert.Equal(novoValor, contaPendente.ValorTotal);
                Assert.Equal(categoriaNova.Id, contaPendente.CategoriaId);
            }

            // Assert: ocorrencia PARCIAL continua com valor e categoria ORIGINAIS (nao foi alterada)
            var contaParcialAposEdicao = contasAposEdicao.FirstOrDefault(c => c.Id == contaParcial.Id);
            Assert.NotNull(contaParcialAposEdicao);
            Assert.Equal(StatusContaReceber.Parcial, contaParcialAposEdicao.Status);
            Assert.Equal(1000m, contaParcialAposEdicao.ValorTotal);
            Assert.Equal(categoriaAntiga.Id, contaParcialAposEdicao.CategoriaId);

            // Assert: ocorrencia RECEBIDO continua com valor e categoria ORIGINAIS (nao foi alterada)
            var contaRecebidaAposEdicao = contasAposEdicao.FirstOrDefault(c => c.Id == contaRecebida.Id);
            Assert.NotNull(contaRecebidaAposEdicao);
            Assert.Equal(StatusContaReceber.Recebido, contaRecebidaAposEdicao.Status);
            Assert.Equal(1000m, contaRecebidaAposEdicao.ValorTotal);
            Assert.Equal(categoriaAntiga.Id, contaRecebidaAposEdicao.CategoriaId);
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

            // ASSERCAO INCONDICIONAL: apos trocar MENSAL(dia 10) -> ANUAL(mes 3),
            // NENHUMA conta PENDENTE pode ter DataPrevista.Month != 3.
            // Se RemoverPendentesForaDoConjunto for removida do RegenerarOcorrenciasAsync,
            // este teste FALHA. Garante que as datas antigas foram limpas.
            Assert.NotEmpty(contasAposEdit);
            Assert.All(contasAposEdit, c => Assert.Equal(3, c.DataPrevista!.Value.Month));
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

            // Criar molde e materializar ocorrencias PENDENTE
            var molde = await service.CriarAsync("Receita mensal", 1000m, "MENSAL", 10, null, null, null);
            dbContext.ChangeTracker.Clear();

            // Pegar contasReceber geradas
            var contasOriginais = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).ToList();
            Assert.NotEmpty(contasOriginais);

            // Forcar manualmente 1 conta como PARCIAL e 1 como RECEBIDO
            var contaParcial = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Parcial manual",
                ValorTotal = 500m,
                DataRegistro = new DateOnly(2026, 8, 1),
                DataPrevista = new DateOnly(2026, 8, 20),
                Status = StatusContaReceber.Parcial,
                RecebivelRecorrenteId = molde.Id
            };

            var contaRecebida = new ContaReceber
            {
                Id = Guid.NewGuid(),
                Tipo = TipoContaReceber.Recebivel,
                Descricao = "Recebida manual",
                ValorTotal = 1000m,
                DataRegistro = new DateOnly(2026, 7, 1),
                DataPrevista = new DateOnly(2026, 7, 30),
                Status = StatusContaReceber.Recebido,
                RecebivelRecorrenteId = molde.Id
            };

            dbContext.ContasReceber.AddRange(contaParcial, contaRecebida);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            // Act
            await service.DesativarAsync(molde.Id);
            dbContext.ChangeTracker.Clear();

            // Assert - molde foi desativado
            var moldeAposDesativacao = await repositorioRecebivel.ObterPorId(molde.Id);
            Assert.NotNull(moldeAposDesativacao);
            Assert.False(moldeAposDesativacao.Ativa);

            // Assert - NENHUMA ocorrencia PENDENTE deve existir (foram deletadas)
            var contasAposDesativacao = (await repositorioConta.Listar()).Where(c => c.RecebivelRecorrenteId == molde.Id).ToList();
            var pendenteCount = contasAposDesativacao.Count(c => c.Status == StatusContaReceber.Pendente);
            Assert.Equal(0, pendenteCount);

            // Assert - ocorrencias PARCIAL e RECEBIDO continuam existindo e intactas
            var contaParcialAposDesativacao = contasAposDesativacao.FirstOrDefault(c => c.Id == contaParcial.Id);
            Assert.NotNull(contaParcialAposDesativacao);
            Assert.Equal(StatusContaReceber.Parcial, contaParcialAposDesativacao.Status);

            var contaRecebidaAposDesativacao = contasAposDesativacao.FirstOrDefault(c => c.Id == contaRecebida.Id);
            Assert.NotNull(contaRecebidaAposDesativacao);
            Assert.Equal(StatusContaReceber.Recebido, contaRecebidaAposDesativacao.Status);
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

            // Criar molde MENSAL e guardar contagem de ocorrencias criadas
            var molde = await service.CriarAsync("Receita mensal", 1000m, "MENSAL", 10, null, null, null);
            var countAposCriacao = (await repositorioConta.Listar())
                .Where(c => c.RecebivelRecorrenteId == molde.Id && c.Status == StatusContaReceber.Pendente)
                .Count();
            Assert.True(countAposCriacao > 0, "Deve ter criado pelo menos 1 ocorrencia PENDENTE");

            dbContext.ChangeTracker.Clear();

            // Desativar (remove todas as PENDENTE)
            await service.DesativarAsync(molde.Id);

            var countAposDesativacao = (await repositorioConta.Listar())
                .Where(c => c.RecebivelRecorrenteId == molde.Id && c.Status == StatusContaReceber.Pendente)
                .Count();
            Assert.Equal(0, countAposDesativacao);

            dbContext.ChangeTracker.Clear();

            // Act - reativar
            await service.ReativarAsync(molde.Id);
            dbContext.ChangeTracker.Clear();

            // Assert - molde esta ativo
            var moldeAposReativacao = await repositorioRecebivel.ObterPorId(molde.Id);
            Assert.NotNull(moldeAposReativacao);
            Assert.True(moldeAposReativacao.Ativa);

            // Assert - contagem de PENDENTE apos reativacao == contagem apos criacao (restaurou conjunto identico)
            var contasAposReativacao = (await repositorioConta.Listar())
                .Where(c => c.RecebivelRecorrenteId == molde.Id && c.Status == StatusContaReceber.Pendente)
                .ToList();
            Assert.Equal(countAposCriacao, contasAposReativacao.Count);

            // Assert - idempotencia: chamar MaterializarOcorrenciasAsync novamente retorna 0 (nao cria duplicatas)
            var dataReferencia = DateOnly.FromDateTime(DateTime.Today);
            var materializadasNovaRodada = await geradorService.MaterializarOcorrenciasAsync(molde.Id, dataReferencia);
            Assert.Equal(0, materializadasNovaRodada);

            // Assert - contagem nao mudou apos 2a rodada de materializacao
            var contasAposMaterializacaoNova = (await repositorioConta.Listar())
                .Where(c => c.RecebivelRecorrenteId == molde.Id && c.Status == StatusContaReceber.Pendente)
                .Count();
            Assert.Equal(countAposCriacao, contasAposMaterializacaoNova);
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
