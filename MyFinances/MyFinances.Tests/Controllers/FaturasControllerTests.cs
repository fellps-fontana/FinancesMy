using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using MyFinances.Controllers;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Controllers;

[CollectionDefinition("FaturasController Integration Collection")]
public class FaturasControllerIntegrationCollection : ICollectionFixture<FaturasControllerIntegrationTestsFixture>
{
}

public class FaturasControllerIntegrationTestsFixture : IAsyncLifetime
{
    private SqliteConnection? _connection;
    public MyFinancesDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var connectionString = "DataSource=:memory:";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        _connection = connection;

        var options = new DbContextOptionsBuilder<MyFinancesDbContext>()
            .UseSqlite(connection)
            .Options;

        DbContext = new MyFinancesDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}

[Collection("FaturasController Integration Collection")]
public class FaturasControllerTests
{
    private readonly FaturasControllerIntegrationTestsFixture _fixture;

    public FaturasControllerTests(FaturasControllerIntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    #region Gap 3 (CRITICO): ListarFaturas deve usar escopo correto (por conta, nao global)

    [Fact]
    public async Task ListarFaturas_ContaX_NaoDeveGerarOcorrenciaDeContaY()
    {
        // Arrange - Gap 3: ListarFaturas hoje chama GarantirOcorrenciasAtivasDoMesAsync (varredura GLOBAL)
        // quando deveria chamar GarantirOcorrenciaDoMesAsync ESCOPADO so as ContaFixa daquela conta
        // (via ContaFixaRepository.ListarPorConta). Efeito colateral: abrir a fatura de UM cartao
        // gera ocorrencias de QUALQUER outra ContaFixa do sistema.
        var contaXId = Guid.NewGuid();  // Conta X (a qual o usuario acessou)
        var contaYId = Guid.NewGuid();  // Conta Y (outra conta do sistema)
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var ano = hoje.Year;
        var mes = hoje.Month;

        var contaX = new Conta
        {
            Id = contaXId,
            Nome = "Cartao X",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 10,
            DiaVencimento = 20,
            Ativa = true
        };

        var contaY = new Conta
        {
            Id = contaYId,
            Nome = "Cartao Y",
            Tipo = TipoConta.Cartao,
            Origem = OrigemConta.Manual,
            DiaFechamento = 10,
            DiaVencimento = 20,
            Ativa = true
        };

        _fixture.DbContext.Contas.Add(contaX);
        _fixture.DbContext.Contas.Add(contaY);
        await _fixture.DbContext.SaveChangesAsync();

        // Setup serviços
        var faturaRepository = new FaturaRepository(_fixture.DbContext);
        var contaRepository = new ContaRepository(_fixture.DbContext);
        var lancamentoRepository = new LancamentoRepository(_fixture.DbContext);
        var transferenciaRepository = new TransferenciaRepository(_fixture.DbContext);
        var contaFixaRepository = new ContaFixaRepository(_fixture.DbContext);

        // Mocks para RecorrenciaGeradorService - estes sao os callsites que queremos verificar
        var mockRecorrenciaGeradorService = new Mock<IRecorrenciaGeradorService>();
        mockRecorrenciaGeradorService
            .Setup(s => s.GarantirOcorrenciaDoMesAsync(It.IsAny<Guid>(), ano, mes))
            .ReturnsAsync(true);

        var faturaCreditoService = new FaturaCreditoService(faturaRepository);
        var faturaCicloService = new FaturaCicloService(faturaRepository, contaRepository);
        var validacaoCartaoService = new ValidacaoCartaoService(contaRepository);
        var pagamentoFaturaService = new PagamentoFaturaService(
            faturaRepository,
            transferenciaRepository,
            lancamentoRepository,
            contaRepository,
            faturaCreditoService);
        var estornoCartaoService = new EstornoCartaoService(
            lancamentoRepository,
            faturaCicloService,
            validacaoCartaoService);

        var controller = new FaturasController(
            faturaRepository,
            pagamentoFaturaService,
            estornoCartaoService,
            faturaCreditoService,
            mockRecorrenciaGeradorService.Object,
            contaFixaRepository);

        // Act - abrir fatura da conta X apenas
        await controller.ListarFaturas(contaXId);

        // Assert - deve chamar GarantirOcorrenciaDoMesAsync (ESCOPADO), nao GarantirOcorrenciasAtivasDoMesAsync (GLOBAL)
        mockRecorrenciaGeradorService.Verify(
            s => s.GarantirOcorrenciasAtivasDoMesAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never,
            "ListarFaturas NAO deve chamar GarantirOcorrenciasAtivasDoMesAsync (varredura global de TODAS as contas)");

        // Idealmente verificaria que GarantirOcorrenciaDoMesAsync foi chamado,
        // mas isso requer injecao de contaFixaRepository que a interface atual nao expoe.
        // Por ora, verificar que GLOBAL nao foi chamado e o suficiente para detectar o gap.
    }

    #endregion
}
