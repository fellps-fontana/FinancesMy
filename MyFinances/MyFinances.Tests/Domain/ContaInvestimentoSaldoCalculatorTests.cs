using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class ContaInvestimentoSaldoCalculatorTests
{
    #region Helpers

    private static Ativo CriarAtivo(Guid? contaId, decimal valorAtual, bool ativa = true)
    {
        return new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = "Ativo teste",
            Tipo = TipoAtivo.RendaFixa,
            ContaId = contaId,
            ValorAtual = valorAtual,
            Ativa = ativa
        };
    }

    private static Conta CriarContaInvestimento(Guid id, decimal? saldoManual)
    {
        return new Conta
        {
            Id = id,
            Nome = "Conta investimento teste",
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Investimento,
            SaldoManual = saldoManual,
            Ativa = true
        };
    }

    #endregion

    #region PossuiAtivoAtivoVinculado

    [Fact]
    public void PossuiAtivoAtivoVinculado_ListaVazia_RetornaFalse()
    {
        // Act
        var resultado = ContaInvestimentoSaldoCalculator.PossuiAtivoAtivoVinculado([]);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void PossuiAtivoAtivoVinculado_SoAtivosInativos_RetornaFalse()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var ativos = new List<Ativo> { CriarAtivo(contaId, 500m, ativa: false) };

        // Act
        var resultado = ContaInvestimentoSaldoCalculator.PossuiAtivoAtivoVinculado(ativos);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void PossuiAtivoAtivoVinculado_UmAtivoAtivo_RetornaTrue()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var ativos = new List<Ativo> { CriarAtivo(contaId, 100m) };

        // Act
        var resultado = ContaInvestimentoSaldoCalculator.PossuiAtivoAtivoVinculado(ativos);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public void PossuiAtivoAtivoVinculado_MistoAtivoEInativo_RetornaTrue()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var ativos = new List<Ativo>
        {
            CriarAtivo(contaId, 500m, ativa: false),
            CriarAtivo(contaId, 100m)
        };

        // Act
        var resultado = ContaInvestimentoSaldoCalculator.PossuiAtivoAtivoVinculado(ativos);

        // Assert
        Assert.True(resultado);
    }

    #endregion

    #region CalcularSaldo - com ativo ativo vinculado

    [Fact]
    public void CalcularSaldo_DoisAtivosAtivos_SomaValorAtualIgnorandoSaldoManual()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = CriarContaInvestimento(contaId, saldoManual: 999m);
        var ativos = new List<Ativo>
        {
            CriarAtivo(contaId, 100m),
            CriarAtivo(contaId, 50m)
        };

        // Act
        var saldo = ContaInvestimentoSaldoCalculator.CalcularSaldo(conta, ativos);

        // Assert
        Assert.Equal(150m, saldo);
    }

    [Fact]
    public void CalcularSaldo_AtivoInativoJuntoComAtivoAtivo_IgnoraInativo()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = CriarContaInvestimento(contaId, saldoManual: 999m);
        var ativos = new List<Ativo>
        {
            CriarAtivo(contaId, 100m),
            CriarAtivo(contaId, 500m, ativa: false)
        };

        // Act
        var saldo = ContaInvestimentoSaldoCalculator.CalcularSaldo(conta, ativos);

        // Assert
        Assert.Equal(100m, saldo);
    }

    [Fact]
    public void CalcularSaldo_AtivoDeOutraConta_NaoEntraNoSaldo()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = CriarContaInvestimento(contaId, saldoManual: null);
        var ativos = new List<Ativo>
        {
            CriarAtivo(contaId, 100m),
            CriarAtivo(Guid.NewGuid(), 300m)
        };

        // Act
        var saldo = ContaInvestimentoSaldoCalculator.CalcularSaldo(conta, ativos);

        // Assert
        Assert.Equal(100m, saldo);
    }

    [Fact]
    public void CalcularSaldo_AtivoSemConta_NaoEntraNoSaldo()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = CriarContaInvestimento(contaId, saldoManual: null);
        var ativos = new List<Ativo>
        {
            CriarAtivo(contaId, 100m),
            CriarAtivo(null, 200m)
        };

        // Act
        var saldo = ContaInvestimentoSaldoCalculator.CalcularSaldo(conta, ativos);

        // Assert
        Assert.Equal(100m, saldo);
    }

    #endregion

    #region CalcularSaldo - sem ativo ativo (volta ao saldo_manual)

    [Fact]
    public void CalcularSaldo_SemAtivos_RetornaSaldoManual()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = CriarContaInvestimento(contaId, saldoManual: 500m);

        // Act
        var saldo = ContaInvestimentoSaldoCalculator.CalcularSaldo(conta, []);

        // Assert
        Assert.Equal(500m, saldo);
    }

    [Fact]
    public void CalcularSaldo_SemAtivosESaldoManualNulo_RetornaZero()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = CriarContaInvestimento(contaId, saldoManual: null);

        // Act
        var saldo = ContaInvestimentoSaldoCalculator.CalcularSaldo(conta, []);

        // Assert
        Assert.Equal(0m, saldo);
    }

    [Fact]
    public void CalcularSaldo_SemAtivosESaldoManualNegativo_PreservaNegativo()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = CriarContaInvestimento(contaId, saldoManual: -250m);

        // Act
        var saldo = ContaInvestimentoSaldoCalculator.CalcularSaldo(conta, []);

        // Assert
        Assert.Equal(-250m, saldo);
    }

    [Fact]
    public void CalcularSaldo_SoAtivoInativo_VoltaAoSaldoManual()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var conta = CriarContaInvestimento(contaId, saldoManual: 40m);
        var ativos = new List<Ativo> { CriarAtivo(contaId, 500m, ativa: false) };

        // Act
        var saldo = ContaInvestimentoSaldoCalculator.CalcularSaldo(conta, ativos);

        // Assert
        Assert.Equal(40m, saldo);
    }

    #endregion

    #region CalcularTotalInvestido

    [Fact]
    public void CalcularTotalInvestido_CenarioMisto_SomaSaldosDeContasEAtivosSemConta()
    {
        // Arrange
        var contaA = Guid.NewGuid();
        var contaB = Guid.NewGuid();
        var contas = new List<Conta>
        {
            CriarContaInvestimento(contaA, saldoManual: null),
            CriarContaInvestimento(contaB, saldoManual: 30m)
        };
        var ativos = new List<Ativo>
        {
            CriarAtivo(contaA, 100m),
            CriarAtivo(contaA, 50m),
            CriarAtivo(null, 20m)
        };

        // Act
        var total = ContaInvestimentoSaldoCalculator.CalcularTotalInvestido(contas, ativos);

        // Assert
        Assert.Equal(200m, total);
    }

    [Fact]
    public void CalcularTotalInvestido_AtivoVinculadoComSaldoManualAntigo_NaoDuplicaValor()
    {
        // Arrange
        var contaA = Guid.NewGuid();
        var contas = new List<Conta> { CriarContaInvestimento(contaA, saldoManual: 100m) };
        var ativos = new List<Ativo> { CriarAtivo(contaA, 100m) };

        // Act
        var total = ContaInvestimentoSaldoCalculator.CalcularTotalInvestido(contas, ativos);

        // Assert
        Assert.Equal(100m, total);
    }

    [Fact]
    public void CalcularTotalInvestido_ContaComSoAtivoInativo_UsaSaldoManual()
    {
        // Arrange
        var contaA = Guid.NewGuid();
        var contas = new List<Conta> { CriarContaInvestimento(contaA, saldoManual: 40m) };
        var ativos = new List<Ativo> { CriarAtivo(contaA, 500m, ativa: false) };

        // Act
        var total = ContaInvestimentoSaldoCalculator.CalcularTotalInvestido(contas, ativos);

        // Assert
        Assert.Equal(40m, total);
    }

    [Fact]
    public void CalcularTotalInvestido_AtivoSemContaInativo_NaoEntraNoTotal()
    {
        // Arrange
        var contaB = Guid.NewGuid();
        var contas = new List<Conta> { CriarContaInvestimento(contaB, saldoManual: 30m) };
        var ativos = new List<Ativo> { CriarAtivo(null, 500m, ativa: false) };

        // Act
        var total = ContaInvestimentoSaldoCalculator.CalcularTotalInvestido(contas, ativos);

        // Assert
        Assert.Equal(30m, total);
    }

    [Fact]
    public void CalcularTotalInvestido_ContaSemAtivoESaldoNulo_ContribuiZero()
    {
        // Arrange
        var contaB = Guid.NewGuid();
        var contas = new List<Conta> { CriarContaInvestimento(contaB, saldoManual: null) };

        // Act
        var total = ContaInvestimentoSaldoCalculator.CalcularTotalInvestido(contas, []);

        // Assert
        Assert.Equal(0m, total);
    }

    [Fact]
    public void CalcularTotalInvestido_SemContasESemAtivos_RetornaZero()
    {
        // Act
        var total = ContaInvestimentoSaldoCalculator.CalcularTotalInvestido([], []);

        // Assert
        Assert.Equal(0m, total);
    }

    #endregion
}
