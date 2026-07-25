using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class ContaFixaLancamentoFactoryTests
{
    #region Regra 1 (a-f): CriarLancamentoPendente gera Lancamento com propriedades corretas

    [Fact]
    public void CriarLancamentoPendente_DiaVencimentoNormal_GeraDataComDiaCorreto()
    {
        // Arrange
        var contaFixaId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = contaId,
            CategoriaId = categoriaId,
            Descricao = "Aluguel",
            Valor = 1500m,
            DiaVencimento = 15,
            Ativa = true
        };
        var ano = 2026;
        var mes = 7;

        // Act
        var lancamento = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, ano, mes);

        // Assert - dia correto
        Assert.Equal(15, lancamento.Data.Day);
        Assert.Equal(7, lancamento.Data.Month);
        Assert.Equal(2026, lancamento.Data.Year);
    }

    [Fact]
    public void CriarLancamentoPendente_Dia31EmMes30Dias_ClampParaUltimoDia()
    {
        // Arrange - Abril tem 30 dias, dia 31 deve clampar pra 30
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            Descricao = "Conta",
            Valor = 100m,
            DiaVencimento = 31,
            Ativa = true
        };
        var ano = 2026;
        var mes = 4; // Abril = 30 dias

        // Act
        var lancamento = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, ano, mes);

        // Assert - deve ser dia 30
        Assert.Equal(30, lancamento.Data.Day);
        Assert.Equal(4, lancamento.Data.Month);
    }

    [Fact]
    public void CriarLancamentoPendente_Dia31EmFevereiro_ClampParaDia28AnoComum()
    {
        // Arrange - 2026 e ano comum (nao bissexto)
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            Descricao = "Conta",
            Valor = 100m,
            DiaVencimento = 31,
            Ativa = true
        };
        var ano = 2026; // Nao eh bissexto
        var mes = 2; // Fevereiro = 28 dias

        // Act
        var lancamento = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, ano, mes);

        // Assert - deve ser dia 28
        Assert.Equal(28, lancamento.Data.Day);
        Assert.Equal(2, lancamento.Data.Month);
    }

    [Fact]
    public void CriarLancamentoPendente_Dia31EmFevereiro_ClampParaDia29AnoBissexto()
    {
        // Arrange - 2024 eh ano bissexto
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid(),
            Descricao = "Conta",
            Valor = 100m,
            DiaVencimento = 31,
            Ativa = true
        };
        var ano = 2024; // Eh bissexto
        var mes = 2; // Fevereiro = 29 dias

        // Act
        var lancamento = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, ano, mes);

        // Assert - deve ser dia 29
        Assert.Equal(29, lancamento.Data.Day);
        Assert.Equal(2, lancamento.Data.Month);
    }

    [Fact]
    public void CriarLancamentoPendente_SempreTipoDEBIT_StatusPendente_ManualTrue()
    {
        // Arrange
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            CategoriaId = null,
            Descricao = "Teste",
            Valor = 50m,
            DiaVencimento = 1,
            Ativa = true
        };

        // Act
        var lancamento = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, 2026, 7);

        // Assert
        Assert.Equal(TipoLancamento.Debit, lancamento.Tipo);
        Assert.Equal(StatusLancamento.Pendente, lancamento.Status);
        Assert.True(lancamento.Manual);
    }

    [Fact]
    public void CriarLancamentoPendente_CopiaValoresContaFixa()
    {
        // Arrange
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            CategoriaId = categoriaId,
            Descricao = "Seguro Veículo",
            Valor = 250.75m,
            DiaVencimento = 20,
            Ativa = true
        };

        // Act
        var lancamento = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, 2026, 8);

        // Assert - copia ContaId, CategoriaId, Descricao, Valor
        Assert.Equal(contaId, lancamento.ContaId);
        Assert.Equal(categoriaId, lancamento.CategoriaId);
        Assert.Equal("Seguro Veículo", lancamento.Descricao);
        Assert.Equal(250.75m, lancamento.Valor);
    }

    [Fact]
    public void CriarLancamentoPendente_ContaFixaIdApontaParaOrigem()
    {
        // Arrange
        var contaFixaId = Guid.NewGuid();
        var contaFixa = new ContaFixa
        {
            Id = contaFixaId,
            ContaId = Guid.NewGuid(),
            CategoriaId = null,
            Descricao = "Teste",
            Valor = 100m,
            DiaVencimento = 10,
            Ativa = true
        };

        // Act
        var lancamento = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, 2026, 9);

        // Assert
        Assert.Equal(contaFixaId, lancamento.ContaFixaId);
    }

    #endregion
}
