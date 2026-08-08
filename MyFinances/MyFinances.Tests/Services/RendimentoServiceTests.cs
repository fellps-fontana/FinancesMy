using MyFinances.Domain;
using MyFinances.Repositories;
using MyFinances.Services;
using MyFinances.Tests.Helpers;
using Xunit;

namespace MyFinances.Tests.Services;

public class RendimentoServiceTests
{
    #region Regra: RegistrarValorizacaoAutomatica com delta != 0

    [Fact]
    public async Task RegistrarValorizacaoAutomatica_ComDeltaPositivo_CriaRendimentoValorizacao()
    {
        // Arrange
        var fakeRepository = new FakeRendimentoRepository();
        var fakeAtivoRepository = new FakeAtivoRepository();
        var service = new RendimentoService(fakeRepository, fakeAtivoRepository);

        var ativoId = Guid.NewGuid();
        var valorAtualAnterior = 1000m;
        var valorAtualNovo = 1200m;
        var data = new DateOnly(2024, 7, 30);

        // Act
        await service.RegistrarValorizacaoAutomatica(ativoId, valorAtualAnterior, valorAtualNovo, data);

        // Assert
        var rendimentos = await fakeRepository.ListarPorAtivo(ativoId);
        Assert.Single(rendimentos);

        var rendimento = rendimentos.First();
        Assert.Equal(ativoId, rendimento.AtivoId);
        Assert.Equal(TipoRendimento.Valorizacao, rendimento.Tipo);
        Assert.Equal(OrigemRendimento.Automatico, rendimento.Origem);
        Assert.Equal(200m, rendimento.Valor); // 1200 - 1000
        Assert.Equal(data, rendimento.Data);
    }

    [Fact]
    public async Task RegistrarValorizacaoAutomatica_ComDeltaNegativo_CriaRendimentoValorizacaoNegativo()
    {
        // Arrange
        var fakeRepository = new FakeRendimentoRepository();
        var fakeAtivoRepository = new FakeAtivoRepository();
        var service = new RendimentoService(fakeRepository, fakeAtivoRepository);

        var ativoId = Guid.NewGuid();
        var valorAtualAnterior = 5000m;
        var valorAtualNovo = 4800m;
        var data = new DateOnly(2024, 7, 30);

        // Act
        await service.RegistrarValorizacaoAutomatica(ativoId, valorAtualAnterior, valorAtualNovo, data);

        // Assert
        var rendimentos = await fakeRepository.ListarPorAtivo(ativoId);
        Assert.Single(rendimentos);

        var rendimento = rendimentos.First();
        Assert.Equal(TipoRendimento.Valorizacao, rendimento.Tipo);
        Assert.Equal(OrigemRendimento.Automatico, rendimento.Origem);
        Assert.Equal(-200m, rendimento.Valor); // 4800 - 5000
    }

    [Fact]
    public async Task RegistrarValorizacaoAutomatica_ComValoresComCentavos_CriaRendimentoPreciso()
    {
        // Arrange
        var fakeRepository = new FakeRendimentoRepository();
        var fakeAtivoRepository = new FakeAtivoRepository();
        var service = new RendimentoService(fakeRepository, fakeAtivoRepository);

        var ativoId = Guid.NewGuid();
        var valorAtualAnterior = 1234.56m;
        var valorAtualNovo = 1456.78m;
        var data = new DateOnly(2024, 7, 30);

        // Act
        await service.RegistrarValorizacaoAutomatica(ativoId, valorAtualAnterior, valorAtualNovo, data);

        // Assert
        var rendimentos = await fakeRepository.ListarPorAtivo(ativoId);
        Assert.Single(rendimentos);

        var rendimento = rendimentos.First();
        Assert.Equal(222.22m, rendimento.Valor); // 1456.78 - 1234.56
    }

    #endregion

    #region Regra: RegistrarValorizacaoAutomatica com delta == 0

    [Fact]
    public async Task RegistrarValorizacaoAutomatica_ComDeltaZero_NaoCriaRendimento()
    {
        // Arrange
        var fakeRepository = new FakeRendimentoRepository();
        var fakeAtivoRepository = new FakeAtivoRepository();
        var service = new RendimentoService(fakeRepository, fakeAtivoRepository);

        var ativoId = Guid.NewGuid();
        var valorAtualAnterior = 1000m;
        var valorAtualNovo = 1000m;
        var data = new DateOnly(2024, 7, 30);

        // Act
        await service.RegistrarValorizacaoAutomatica(ativoId, valorAtualAnterior, valorAtualNovo, data);

        // Assert
        var rendimentos = await fakeRepository.ListarPorAtivo(ativoId);
        Assert.Empty(rendimentos);
    }

    [Fact]
    public async Task RegistrarValorizacaoAutomatica_ComValoresIguaisComCentavos_NaoCriaRendimento()
    {
        // Arrange
        var fakeRepository = new FakeRendimentoRepository();
        var fakeAtivoRepository = new FakeAtivoRepository();
        var service = new RendimentoService(fakeRepository, fakeAtivoRepository);

        var ativoId = Guid.NewGuid();
        var valorAtualAnterior = 5678.90m;
        var valorAtualNovo = 5678.90m;
        var data = new DateOnly(2024, 7, 30);

        // Act
        await service.RegistrarValorizacaoAutomatica(ativoId, valorAtualAnterior, valorAtualNovo, data);

        // Assert
        var rendimentos = await fakeRepository.ListarPorAtivo(ativoId);
        Assert.Empty(rendimentos);
    }

    #endregion

    #region Regra: Casos extremos

    [Fact]
    public async Task RegistrarValorizacaoAutomatica_ComZeroParaValorPositivo_CriaRendimento()
    {
        // Arrange
        var fakeRepository = new FakeRendimentoRepository();
        var fakeAtivoRepository = new FakeAtivoRepository();
        var service = new RendimentoService(fakeRepository, fakeAtivoRepository);

        var ativoId = Guid.NewGuid();
        var valorAtualAnterior = 0m;
        var valorAtualNovo = 100m;
        var data = new DateOnly(2024, 7, 30);

        // Act
        await service.RegistrarValorizacaoAutomatica(ativoId, valorAtualAnterior, valorAtualNovo, data);

        // Assert
        var rendimentos = await fakeRepository.ListarPorAtivo(ativoId);
        Assert.Single(rendimentos);

        var rendimento = rendimentos.First();
        Assert.Equal(100m, rendimento.Valor);
    }

    [Fact]
    public async Task RegistrarValorizacaoAutomatica_ComValorGrande_CriaRendimento()
    {
        // Arrange
        var fakeRepository = new FakeRendimentoRepository();
        var fakeAtivoRepository = new FakeAtivoRepository();
        var service = new RendimentoService(fakeRepository, fakeAtivoRepository);

        var ativoId = Guid.NewGuid();
        var valorAtualAnterior = 1_000_000m;
        var valorAtualNovo = 1_500_000m;
        var data = new DateOnly(2024, 7, 30);

        // Act
        await service.RegistrarValorizacaoAutomatica(ativoId, valorAtualAnterior, valorAtualNovo, data);

        // Assert
        var rendimentos = await fakeRepository.ListarPorAtivo(ativoId);
        Assert.Single(rendimentos);

        var rendimento = rendimentos.First();
        Assert.Equal(500_000m, rendimento.Valor);
    }

    [Fact]
    public async Task RegistrarValorizacaoAutomatica_ComPequenaDiferenca_CriaRendimento()
    {
        // Arrange
        var fakeRepository = new FakeRendimentoRepository();
        var fakeAtivoRepository = new FakeAtivoRepository();
        var service = new RendimentoService(fakeRepository, fakeAtivoRepository);

        var ativoId = Guid.NewGuid();
        var valorAtualAnterior = 10000m;
        var valorAtualNovo = 10000.01m;
        var data = new DateOnly(2024, 7, 30);

        // Act
        await service.RegistrarValorizacaoAutomatica(ativoId, valorAtualAnterior, valorAtualNovo, data);

        // Assert
        var rendimentos = await fakeRepository.ListarPorAtivo(ativoId);
        Assert.Single(rendimentos);

        var rendimento = rendimentos.First();
        Assert.Equal(0.01m, rendimento.Valor);
    }

    #endregion
}
