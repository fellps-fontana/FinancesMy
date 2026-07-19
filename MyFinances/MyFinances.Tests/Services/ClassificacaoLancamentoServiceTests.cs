using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Services;

public class ClassificacaoLancamentoServiceTests
{
    // Caso 1: Tipo=Debit sem TransferenciaId e FaturaId -> Saida
    [Fact]
    public void Classificar_DebitSemTransferenciaOuFatura_RetornaSaida()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = 100m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = null,
            FaturaId = null
        };

        // Act
        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        // Assert
        Assert.Equal(ClassificacaoLancamento.Saida, resultado);
    }

    // Caso 2: Tipo=Credit sem TransferenciaId e FaturaId -> Entrada
    [Fact]
    public void Classificar_CreditSemTransferenciaOuFatura_RetornaEntrada()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = 150m,
            Tipo = TipoLancamento.Credit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = null,
            FaturaId = null
        };

        // Act
        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        // Assert
        Assert.Equal(ClassificacaoLancamento.Entrada, resultado);
    }

    // Caso 3: TransferenciaId preenchido com Tipo=Debit -> Transferencia
    // Prova que TransferenciaId tem precedencia sobre Tipo
    [Fact]
    public void Classificar_DebitComTransferenciaId_RetornaTransferencia()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = 200m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = Guid.NewGuid(),
            FaturaId = null
        };

        // Act
        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        // Assert
        Assert.Equal(ClassificacaoLancamento.Transferencia, resultado);
    }

    // Caso 4: TransferenciaId preenchido com Tipo=Credit -> Transferencia
    // Prova que TransferenciaId ignora o sinal e sempre retorna Transferencia
    [Fact]
    public void Classificar_CreditComTransferenciaId_RetornaTransferencia()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = 300m,
            Tipo = TipoLancamento.Credit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = Guid.NewGuid(),
            FaturaId = null
        };

        // Act
        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        // Assert
        Assert.Equal(ClassificacaoLancamento.Transferencia, resultado);
    }

    // Caso 5: FaturaId preenchido -> CompetenciaCartao
    // Prova que FaturaId tem precedencia sobre Tipo
    [Fact]
    public void Classificar_ComFaturaId_RetornaCompetenciaCartao()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = 250m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = null,
            FaturaId = Guid.NewGuid()
        };

        // Act
        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        // Assert
        Assert.Equal(ClassificacaoLancamento.CompetenciaCartao, resultado);
    }

    // Caso 6: Tipo=Credit com Valor negativo -> Entrada
    // Prova que a funcao NAO olha para o sinal bruto de Valor
    // Regra critica: "usar SEMPRE o campo tipo (DEBIT | CREDIT) combinado com o account_type para classificar entrada/saida. Nunca somar valor cru."
    [Fact]
    public void Classificar_CreditComValorNegativo_RetornaEntradaIgnorandoSinal()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = -500m, // Valor negativo propositalmente
            Tipo = TipoLancamento.Credit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = null,
            FaturaId = null
        };

        // Act
        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        // Assert
        // Espera Entrada porque Tipo=Credit, nao porque Valor > 0
        Assert.Equal(ClassificacaoLancamento.Entrada, resultado);
    }

    // Caso 7: TransferenciaId e FaturaId AMBOS preenchidos -> Transferencia
    // Prova que TransferenciaId tem precedencia sobre FaturaId (completa a cadeia: TransferenciaId > FaturaId > Tipo)
    [Fact]
    public void Classificar_ComTransferenciaIdEFaturaId_RetornaTransferencia()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = 400m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = Guid.NewGuid(),
            FaturaId = Guid.NewGuid()
        };

        // Act
        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        // Assert
        // Espera Transferencia porque TransferenciaId tem precedencia sobre FaturaId
        Assert.Equal(ClassificacaoLancamento.Transferencia, resultado);
    }
}
