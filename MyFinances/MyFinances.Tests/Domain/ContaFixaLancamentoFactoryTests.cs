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

    #region Regra 5 (m): ProximaOcorrencia com Mensal soma 1 mes

    [Fact]
    public void ProximaOcorrencia_Mensal_SomaPrimeiroAoUltimoDia()
    {
        // Arrange
        var dataAtual = new DateOnly(2026, 7, 15);
        var periodicidade = PeriodicidadeContaFixa.Mensal;

        // Act
        var proximaData = ContaFixaLancamentoFactory.ProximaOcorrencia(dataAtual, periodicidade);

        // Assert - deve ser agosto (proximo mes), mesmo dia (15)
        Assert.Equal(15, proximaData.Day);
        Assert.Equal(8, proximaData.Month);
        Assert.Equal(2026, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Mensal_MesDozeParaJaneiro()
    {
        // Arrange - dezembro vai pra janeiro do proximo ano
        var dataAtual = new DateOnly(2026, 12, 20);
        var periodicidade = PeriodicidadeContaFixa.Mensal;

        // Act
        var proximaData = ContaFixaLancamentoFactory.ProximaOcorrencia(dataAtual, periodicidade);

        // Assert - deve ser janeiro de 2027, dia 20
        Assert.Equal(20, proximaData.Day);
        Assert.Equal(1, proximaData.Month);
        Assert.Equal(2027, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Mensal_Dia31AbrirParaDia30()
    {
        // Arrange - março (31 dias) -> abril (30 dias)
        var dataAtual = new DateOnly(2026, 3, 31);
        var periodicidade = PeriodicidadeContaFixa.Mensal;

        // Act
        var proximaData = ContaFixaLancamentoFactory.ProximaOcorrencia(dataAtual, periodicidade);

        // Assert - abril tem 30 dias, deve clampar pra 30
        Assert.Equal(30, proximaData.Day);
        Assert.Equal(4, proximaData.Month);
        Assert.Equal(2026, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Mensal_Dia31JaneiroParaFevereiro()
    {
        // Arrange - janeiro (31 dias) para fevereiro (28/29 dias)
        var dataAtual = new DateOnly(2026, 1, 31);
        var periodicidade = PeriodicidadeContaFixa.Mensal;

        // Act
        var proximaData = ContaFixaLancamentoFactory.ProximaOcorrencia(dataAtual, periodicidade);

        // Assert - fevereiro 2026 tem 28 dias, deve clampar pra 28
        Assert.Equal(28, proximaData.Day);
        Assert.Equal(2, proximaData.Month);
        Assert.Equal(2026, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Mensal_Dia31JaneiroPAriFevereiroAnoBissexto()
    {
        // Arrange - janeiro 2024 para fevereiro 2024 (bissexto, 29 dias)
        var dataAtual = new DateOnly(2024, 1, 31);
        var periodicidade = PeriodicidadeContaFixa.Mensal;

        // Act
        var proximaData = ContaFixaLancamentoFactory.ProximaOcorrencia(dataAtual, periodicidade);

        // Assert - fevereiro 2024 eh bissexto (29 dias), deve clampar pra 29
        Assert.Equal(29, proximaData.Day);
        Assert.Equal(2, proximaData.Month);
        Assert.Equal(2024, proximaData.Year);
    }

    #endregion

    #region Regra 6 (n): ProximaOcorrencia com Anual soma 1 ano

    [Fact]
    public void ProximaOcorrencia_Anual_SomaUmAno()
    {
        // Arrange
        var dataAtual = new DateOnly(2026, 7, 15);
        var periodicidade = PeriodicidadeContaFixa.Anual;

        // Act
        var proximaData = ContaFixaLancamentoFactory.ProximaOcorrencia(dataAtual, periodicidade);

        // Assert - deve ser 2027, mesmo mes e dia
        Assert.Equal(15, proximaData.Day);
        Assert.Equal(7, proximaData.Month);
        Assert.Equal(2027, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Anual_Dia31FevereiroAnoBissextoParaAnoComum()
    {
        // Arrange - 29 fevereiro 2024 (bissexto) para 2025 (nao bissexto)
        // Ao somar 1 ano, febr 2025 tem 28 dias, deve clampar
        var dataAtual = new DateOnly(2024, 2, 29);
        var periodicidade = PeriodicidadeContaFixa.Anual;

        // Act
        var proximaData = ContaFixaLancamentoFactory.ProximaOcorrencia(dataAtual, periodicidade);

        // Assert - fevereiro 2025 nao eh bissexto, deve clampar pra 28
        Assert.Equal(28, proximaData.Day);
        Assert.Equal(2, proximaData.Month);
        Assert.Equal(2025, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Anual_Dia31AbrirAnoBissexto()
    {
        // Arrange - janeiro 2023 dia 31 para 2024 (que eh bissexto, mas fevereiro mesmo assim teria 29)
        var dataAtual = new DateOnly(2023, 1, 31);
        var periodicidade = PeriodicidadeContaFixa.Anual;

        // Act
        var proximaData = ContaFixaLancamentoFactory.ProximaOcorrencia(dataAtual, periodicidade);

        // Assert - ainda janeiro, apenas o ano avanca
        Assert.Equal(31, proximaData.Day);
        Assert.Equal(1, proximaData.Month);
        Assert.Equal(2024, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Anual_MesmesDiaEMes()
    {
        // Arrange - verificar que so o ano muda
        var dataAtual = new DateOnly(2025, 11, 22);
        var periodicidade = PeriodicidadeContaFixa.Anual;

        // Act
        var proximaData = ContaFixaLancamentoFactory.ProximaOcorrencia(dataAtual, periodicidade);

        // Assert
        Assert.Equal(22, proximaData.Day);
        Assert.Equal(11, proximaData.Month);
        Assert.Equal(2026, proximaData.Year);
    }

    #endregion

    #region Regra 7 (o): EhOcorrenciaValida para Mensal sempre true, Anual so no MesReferencia

    [Fact]
    public void EhOcorrenciaValida_Mensal_SempreSempreTrue()
    {
        // Arrange - ContaFixa com periodicidade Mensal
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Descricao = "Aluguel",
            Valor = 1000m,
            DiaVencimento = 15,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            MesReferencia = null,
            Ativa = true
        };

        // Act & Assert - qualquer mes e valido para Mensal
        Assert.True(ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, 2026, 1));
        Assert.True(ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, 2026, 7));
        Assert.True(ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, 2026, 12));
        Assert.True(ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, 2027, 6));
    }

    [Fact]
    public void EhOcorrenciaValida_Anual_SoTrueQuandoMesIgualMesReferencia()
    {
        // Arrange - ContaFixa com periodicidade Anual e MesReferencia = julho
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Descricao = "Seguro anual",
            Valor = 500m,
            DiaVencimento = 20,
            Periodicidade = PeriodicidadeContaFixa.Anual,
            MesReferencia = 7,
            Ativa = true
        };

        // Act & Assert - so julho e valido, todos os outros meses sao false
        Assert.False(ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, 2026, 1));
        Assert.False(ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, 2026, 6));
        Assert.True(ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, 2026, 7));
        Assert.False(ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, 2026, 8));
        Assert.False(ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, 2026, 12));
        Assert.True(ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, 2027, 7));
    }

    #endregion

    #region Regra 8 (p): CalcularConjuntoAtualEProxima recalcula o par sob periodicidade ATUAL

    [Fact]
    public void CalcularConjuntoAtualEProxima_Mensal_RetornaAtualEProximoMes()
    {
        // Arrange - ContaFixa Mensal, dataReferencia = julho 15
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Descricao = "Conta",
            Valor = 100m,
            DiaVencimento = 15,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            MesReferencia = null,
            Ativa = true
        };
        var dataReferencia = new DateOnly(2026, 7, 15);

        // Act
        var (atual, proxima) = ContaFixaLancamentoFactory.CalcularConjuntoAtualEProxima(contaFixa, dataReferencia);

        // Assert - atual = julho 15, proxima = agosto 15
        Assert.Equal(new DateOnly(2026, 7, 15), atual);
        Assert.Equal(new DateOnly(2026, 8, 15), proxima);
    }

    [Fact]
    public void CalcularConjuntoAtualEProxima_Mensal_ClampDiaAoUltimoDiaDomes()
    {
        // Arrange - dia 31 em mes com 30 dias e depois mes com 28 (fevereiro)
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Descricao = "Conta",
            Valor = 100m,
            DiaVencimento = 31,
            Periodicidade = PeriodicidadeContaFixa.Mensal,
            MesReferencia = null,
            Ativa = true
        };
        var dataReferencia = new DateOnly(2026, 1, 31);

        // Act
        var (atual, proxima) = ContaFixaLancamentoFactory.CalcularConjuntoAtualEProxima(contaFixa, dataReferencia);

        // Assert - atual = janeiro 31, proxima = fevereiro 28 (clampado, 2026 nao eh bissexto)
        Assert.Equal(new DateOnly(2026, 1, 31), atual);
        Assert.Equal(new DateOnly(2026, 2, 28), proxima);
    }

    [Fact]
    public void CalcularConjuntoAtualEProxima_Anual_RetornaAtualEProximoAno()
    {
        // Arrange - ContaFixa Anual em julho
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Descricao = "Seguro anual",
            Valor = 500m,
            DiaVencimento = 20,
            Periodicidade = PeriodicidadeContaFixa.Anual,
            MesReferencia = 7,
            Ativa = true
        };
        var dataReferencia = new DateOnly(2026, 7, 20);

        // Act
        var (atual, proxima) = ContaFixaLancamentoFactory.CalcularConjuntoAtualEProxima(contaFixa, dataReferencia);

        // Assert - atual = 2026 julho 20, proxima = 2027 julho 20
        Assert.Equal(new DateOnly(2026, 7, 20), atual);
        Assert.Equal(new DateOnly(2027, 7, 20), proxima);
    }

    [Fact]
    public void CalcularConjuntoAtualEProxima_AnualBissexto_ClampDe29FevPara28()
    {
        // Arrange - data referencia = fevereiro 29, 2024 (bissexto), prox ano = 2025 (nao bissexto)
        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Descricao = "Conta",
            Valor = 100m,
            DiaVencimento = 29,
            Periodicidade = PeriodicidadeContaFixa.Anual,
            MesReferencia = 2,
            Ativa = true
        };
        var dataReferencia = new DateOnly(2024, 2, 29);

        // Act
        var (atual, proxima) = ContaFixaLancamentoFactory.CalcularConjuntoAtualEProxima(contaFixa, dataReferencia);

        // Assert - atual = 2024 fevereiro 29, proxima = 2025 fevereiro 28 (clampado)
        Assert.Equal(new DateOnly(2024, 2, 29), atual);
        Assert.Equal(new DateOnly(2025, 2, 28), proxima);
    }

    #endregion
}
