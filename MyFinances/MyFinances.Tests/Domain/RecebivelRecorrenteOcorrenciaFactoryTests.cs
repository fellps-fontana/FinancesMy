using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class RecebivelRecorrenteOcorrenciaFactoryTests
{
    #region Regra ProximaOcorrencia: MENSAL +1 mes, ANUAL +1 ano, SEMANAL +7 dias

    [Fact]
    public void ProximaOcorrencia_Mensal_SomaPrimeiroMesAncoroDiaVencimento()
    {
        // Arrange
        var dataAtual = new DateOnly(2026, 7, 15);
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita mensal",
            Valor = 1000m,
            Periodicidade = PeriodicidadeRecebivel.Mensal,
            DiaVencimento = 15,
            Ativa = true
        };

        // Act
        var proximaData = RecebivelRecorrenteOcorrenciaFactory.ProximaOcorrencia(dataAtual, molde);

        // Assert - deve ser agosto (proximo mes), dia 15
        Assert.Equal(15, proximaData.Day);
        Assert.Equal(8, proximaData.Month);
        Assert.Equal(2026, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Mensal_AventuraDezembroParaJaneiro()
    {
        // Arrange
        var dataAtual = new DateOnly(2026, 12, 20);
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita",
            Valor = 500m,
            Periodicidade = PeriodicidadeRecebivel.Mensal,
            DiaVencimento = 20,
            Ativa = true
        };

        // Act
        var proximaData = RecebivelRecorrenteOcorrenciaFactory.ProximaOcorrencia(dataAtual, molde);

        // Assert - deve ser janeiro de 2027, dia 20
        Assert.Equal(20, proximaData.Day);
        Assert.Equal(1, proximaData.Month);
        Assert.Equal(2027, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Anual_SomaUmAnoAncoraMesReferencia()
    {
        // Arrange
        var dataAtual = new DateOnly(2026, 7, 15);
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita anual",
            Valor = 1000m,
            Periodicidade = PeriodicidadeRecebivel.Anual,
            MesReferencia = 7,
            DiaVencimento = 15,
            Ativa = true
        };

        // Act
        var proximaData = RecebivelRecorrenteOcorrenciaFactory.ProximaOcorrencia(dataAtual, molde);

        // Assert - deve ser 2027, mes 7, dia 15
        Assert.Equal(15, proximaData.Day);
        Assert.Equal(7, proximaData.Month);
        Assert.Equal(2027, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Semanal_SomaSeteDias()
    {
        // Arrange
        var dataAtual = new DateOnly(2026, 8, 26); // Quarta
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita semanal",
            Valor = 100m,
            Periodicidade = PeriodicidadeRecebivel.Semanal,
            DiaDaSemana = DiaDaSemana.Quarta,
            Ativa = true
        };

        // Act
        var proximaData = RecebivelRecorrenteOcorrenciaFactory.ProximaOcorrencia(dataAtual, molde);

        // Assert - deve ser 7 dias depois
        Assert.Equal(new DateOnly(2026, 9, 2), proximaData);
    }

    #endregion

    #region Regra Clamp: DiaVencimento=31 em fevereiro -> dia 28/29 (ultimo dia do mes)

    [Fact]
    public void ProximaOcorrencia_Mensal_Dia31EmFevereiro_ClampParaDia28AnoComum()
    {
        // Arrange
        var dataAtual = new DateOnly(2026, 1, 31);
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Conta",
            Valor = 100m,
            Periodicidade = PeriodicidadeRecebivel.Mensal,
            DiaVencimento = 31,
            Ativa = true
        };

        // Act
        var proximaData = RecebivelRecorrenteOcorrenciaFactory.ProximaOcorrencia(dataAtual, molde);

        // Assert - fevereiro 2026 tem 28 dias, deve clampar pra 28
        Assert.Equal(28, proximaData.Day);
        Assert.Equal(2, proximaData.Month);
        Assert.Equal(2026, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Mensal_Dia31EmFevereiro_ClampParaDia29AnoBissexto()
    {
        // Arrange - 2024 eh ano bissexto
        var dataAtual = new DateOnly(2024, 1, 31);
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Conta",
            Valor = 100m,
            Periodicidade = PeriodicidadeRecebivel.Mensal,
            DiaVencimento = 31,
            Ativa = true
        };

        // Act
        var proximaData = RecebivelRecorrenteOcorrenciaFactory.ProximaOcorrencia(dataAtual, molde);

        // Assert - fevereiro 2024 eh bissexto (29 dias), deve clampar pra 29
        Assert.Equal(29, proximaData.Day);
        Assert.Equal(2, proximaData.Month);
        Assert.Equal(2024, proximaData.Year);
    }

    [Fact]
    public void ProximaOcorrencia_Mensal_AncoraSempreEmDiaVencimentoNunca_NoClampAntigo()
    {
        // Arrange - regra critica: ancora sempre em DiaVencimento do molde, nao no dia ja clampado
        // Comeca em 28/02 (clampado de 31), proxima deve ser 31/03 (nao 28/03)
        var dataAtual = new DateOnly(2026, 2, 28);
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Conta",
            Valor = 100m,
            Periodicidade = PeriodicidadeRecebivel.Mensal,
            DiaVencimento = 31, // Ancora sempre em 31, mesmo que fevereiro tenha 28
            Ativa = true
        };

        // Act
        var proximaData = RecebivelRecorrenteOcorrenciaFactory.ProximaOcorrencia(dataAtual, molde);

        // Assert - deve ser 31/03, nao 28/03 (comprova que ancora em DiaVencimento=31, nao no dia atual=28)
        Assert.Equal(31, proximaData.Day);
        Assert.Equal(3, proximaData.Month);
        Assert.Equal(2026, proximaData.Year);
    }

    #endregion

    #region Regra CalcularOcorrenciasNoIntervalo: MENSAL, ANUAL, SEMANAL

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Mensal_RetornaTodasOcorrenciasDoMes()
    {
        // Arrange
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita mensal",
            Valor = 1000m,
            Periodicidade = PeriodicidadeRecebivel.Mensal,
            DiaVencimento = 10,
            Ativa = true
        };
        var inicio = new DateOnly(2026, 8, 1);
        var fim = new DateOnly(2026, 10, 31);

        // Act
        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);

        // Assert - agosto 10, setembro 10, outubro 10
        Assert.Equal(3, ocorrencias.Count);
        Assert.Contains(new DateOnly(2026, 8, 10), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 10), ocorrencias);
        Assert.Contains(new DateOnly(2026, 10, 10), ocorrencias);
    }

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Anual_RetornaOcorrenciasDoMesReferencia()
    {
        // Arrange
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita anual",
            Valor = 5000m,
            Periodicidade = PeriodicidadeRecebivel.Anual,
            MesReferencia = 3,
            DiaVencimento = 15,
            Ativa = true
        };
        var inicio = new DateOnly(2025, 1, 1);
        var fim = new DateOnly(2027, 12, 31);

        // Act
        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);

        // Assert - deve retornar 15/03 de 2025, 2026, 2027 (3 anos cobridos)
        Assert.Equal(3, ocorrencias.Count);
        Assert.Contains(new DateOnly(2025, 3, 15), ocorrencias);
        Assert.Contains(new DateOnly(2026, 3, 15), ocorrencias);
        Assert.Contains(new DateOnly(2027, 3, 15), ocorrencias);
    }

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Semanal_RetornaTodasAsQuartas()
    {
        // Arrange - interval [segunda 24/08, sabado 13/09], molde DiaDaSemana=Quarta
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita semanal",
            Valor = 100m,
            Periodicidade = PeriodicidadeRecebivel.Semanal,
            DiaDaSemana = DiaDaSemana.Quarta,
            Ativa = true
        };
        var inicio = new DateOnly(2026, 8, 24); // Segunda
        var fim = new DateOnly(2026, 9, 13);   // Domingo

        // Act
        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);

        // Assert - quarta da semana de 24/08 = 26/08, depois +7 = 02/09, +7 = 09/09
        Assert.Equal(3, ocorrencias.Count);
        Assert.Contains(new DateOnly(2026, 8, 26), ocorrencias); // Quarta da semana de 24/08
        Assert.Contains(new DateOnly(2026, 9, 2), ocorrencias);  // +7
        Assert.Contains(new DateOnly(2026, 9, 9), ocorrencias);  // +7
    }

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Semanal_InicioAntesDoAlvoNaSemana_IncluiOAlvoDaSemana()
    {
        // Arrange - inicio na terca (25/08), quarta e o dia seguinte, dentro do intervalo
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita semanal",
            Valor = 100m,
            Periodicidade = PeriodicidadeRecebivel.Semanal,
            DiaDaSemana = DiaDaSemana.Quarta,
            Ativa = true
        };
        var inicio = new DateOnly(2026, 8, 25); // Terca
        var fim = new DateOnly(2026, 9, 20);

        // Act
        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);

        // Assert - quarta 26/08 entra (>= inicio e <= fim), depois +7 = 02/09, +7 = 09/09, +7 = 16/09
        Assert.Equal(4, ocorrencias.Count);
        Assert.Contains(new DateOnly(2026, 8, 26), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 2), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 9), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 16), ocorrencias);
    }

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Semanal_InicioNoProprioQuarta_Entra()
    {
        // Arrange - inicio NA quarta (26/08), a quarta corrente ainda entra se >= inicio
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita semanal",
            Valor = 100m,
            Periodicidade = PeriodicidadeRecebivel.Semanal,
            DiaDaSemana = DiaDaSemana.Quarta,
            Ativa = true
        };
        var inicio = new DateOnly(2026, 8, 26); // Quarta
        var fim = new DateOnly(2026, 9, 10);

        // Act
        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);

        // Assert - quarta 26/08 entra porque = inicio, depois +7 = 02/09, +7 = 09/09
        Assert.Equal(3, ocorrencias.Count);
        Assert.Contains(new DateOnly(2026, 8, 26), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 2), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 9), ocorrencias);
    }

    [Fact]
    public void ProximaOcorrencia_Semanal_DataAtualNoAlvo_RetornaSeteDiasDepois()
    {
        // Arrange - dataAtual no dia alvo, deve retornar a proxima ocorrencia (7 dias depois)
        var dataAtual = new DateOnly(2026, 8, 26); // Quarta
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita semanal",
            Valor = 100m,
            Periodicidade = PeriodicidadeRecebivel.Semanal,
            DiaDaSemana = DiaDaSemana.Quarta,
            Ativa = true
        };

        // Act
        var proximaData = RecebivelRecorrenteOcorrenciaFactory.ProximaOcorrencia(dataAtual, molde);

        // Assert - deve ser 7 dias depois na mesma quarta
        Assert.Equal(new DateOnly(2026, 9, 2), proximaData);
    }

    [Fact]
    public void ProximaOcorrencia_Semanal_DataAtualForaDoAlvo_RetornaProximoAlvo()
    {
        // Arrange - dataAtual em sexta, alvo = segunda, deve retornar a proxima segunda
        var dataAtual = new DateOnly(2026, 8, 28); // Sexta
        var molde = new RecebivelRecorrente
        {
            Id = Guid.NewGuid(),
            Descricao = "Receita semanal",
            Valor = 100m,
            Periodicidade = PeriodicidadeRecebivel.Semanal,
            DiaDaSemana = DiaDaSemana.Segunda,
            Ativa = true
        };

        // Act
        var proximaData = RecebivelRecorrenteOcorrenciaFactory.ProximaOcorrencia(dataAtual, molde);

        // Assert - proxima segunda depois de sexta 28/08 = 31/08 (a segunda seguinte, 3 dias depois)
        Assert.Equal(new DateOnly(2026, 8, 31), proximaData);
    }

    #endregion

    #region Regra CriarOcorrenciaPendente: ContaReceber com propriedades corretas

    [Fact]
    public void CriarOcorrenciaPendente_GeraRecebivelPendenteComPropriedadesCorretas()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var molde = new RecebivelRecorrente
        {
            Id = moldeId,
            Descricao = "Receita mensal",
            Valor = 1500m,
            Periodicidade = PeriodicidadeRecebivel.Mensal,
            DiaVencimento = 10,
            CategoriaId = categoriaId,
            Ativa = true
        };
        var dataOcorrencia = new DateOnly(2026, 8, 10);
        var dataGeracao = new DateOnly(2026, 8, 1);

        // Act
        var contaReceber = RecebivelRecorrenteOcorrenciaFactory.CriarOcorrenciaPendente(molde, dataOcorrencia, dataGeracao);

        // Assert - tipo, status, pessoa, valor, datas, categoria
        Assert.Equal(TipoContaReceber.Recebivel, contaReceber.Tipo);
        Assert.Equal(StatusContaReceber.Pendente, contaReceber.Status);
        Assert.Null(contaReceber.Pessoa);
        Assert.Equal(1500m, contaReceber.ValorTotal);
        Assert.Equal(dataOcorrencia, contaReceber.DataPrevista);
        Assert.Equal(dataGeracao, contaReceber.DataRegistro);
        Assert.Equal(categoriaId, contaReceber.CategoriaId);
        Assert.Equal(moldeId, contaReceber.RecebivelRecorrenteId);
    }

    [Fact]
    public void CriarOcorrenciaPendente_SemCategoria_PessoaSempreMula()
    {
        // Arrange
        var moldeId = Guid.NewGuid();
        var molde = new RecebivelRecorrente
        {
            Id = moldeId,
            Descricao = "Receita sem categoria",
            Valor = 500m,
            Periodicidade = PeriodicidadeRecebivel.Semanal,
            DiaDaSemana = DiaDaSemana.Sexta,
            CategoriaId = null,
            Ativa = true
        };
        var dataOcorrencia = new DateOnly(2026, 8, 28);
        var dataGeracao = new DateOnly(2026, 8, 21);

        // Act
        var contaReceber = RecebivelRecorrenteOcorrenciaFactory.CriarOcorrenciaPendente(molde, dataOcorrencia, dataGeracao);

        // Assert
        Assert.Null(contaReceber.Pessoa);
        Assert.Null(contaReceber.CategoriaId);
        Assert.Equal(moldeId, contaReceber.RecebivelRecorrenteId);
    }

    #endregion
}
