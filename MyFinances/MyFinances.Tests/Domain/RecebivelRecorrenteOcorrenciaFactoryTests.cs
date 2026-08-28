using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class RecebivelRecorrenteOcorrenciaFactoryTests
{
    #region PrimeiraOcorrenciaAPartirDe: ancora por periodicidade (MENSAL/ANUAL/SEMANAL)

    [Fact]
    public void PrimeiraOcorrenciaAPartirDe_Mensal_DiaDoMesAindaNaoPassou_RetornaEsteMes()
    {
        var molde = MoldeMensal(diaVencimento: 15);

        var data = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2026, 7, 10));

        Assert.Equal(new DateOnly(2026, 7, 15), data);
    }

    [Fact]
    public void PrimeiraOcorrenciaAPartirDe_Mensal_DiaDoMesJaPassou_RetornaProximoMes()
    {
        var molde = MoldeMensal(diaVencimento: 15);

        var data = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2026, 7, 16));

        Assert.Equal(new DateOnly(2026, 8, 15), data);
    }

    [Fact]
    public void PrimeiraOcorrenciaAPartirDe_Mensal_DezembroParaJaneiro()
    {
        var molde = MoldeMensal(diaVencimento: 20);

        var data = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2026, 12, 21));

        Assert.Equal(new DateOnly(2027, 1, 20), data);
    }

    [Fact]
    public void PrimeiraOcorrenciaAPartirDe_Anual_MesReferenciaJaPassou_RetornaProximoAno()
    {
        var molde = MoldeAnual(mesReferencia: 7, diaVencimento: 15);

        var data = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2026, 8, 1));

        Assert.Equal(new DateOnly(2027, 7, 15), data);
    }

    [Fact]
    public void PrimeiraOcorrenciaAPartirDe_Anual_MesReferenciaNoFuturo_RetornaEsteAno()
    {
        var molde = MoldeAnual(mesReferencia: 7, diaVencimento: 15);

        var data = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2026, 1, 5));

        Assert.Equal(new DateOnly(2026, 7, 15), data);
    }

    [Fact]
    public void PrimeiraOcorrenciaAPartirDe_Semanal_DataAtualNoAlvo_RetornaODia()
    {
        var molde = MoldeSemanal(DiaDaSemana.Quarta);

        var data = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2026, 8, 26)); // Quarta

        Assert.Equal(new DateOnly(2026, 8, 26), data);
    }

    [Fact]
    public void PrimeiraOcorrenciaAPartirDe_Semanal_DataAtualAntesDoAlvo_RetornaProximoAlvo()
    {
        var molde = MoldeSemanal(DiaDaSemana.Segunda);

        var data = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2026, 8, 28)); // Sexta

        Assert.Equal(new DateOnly(2026, 8, 31), data); // proxima segunda
    }

    #endregion

    #region Clamp de dia: DiaVencimento excede os dias do mes -> ultimo dia do mes

    [Fact]
    public void PrimeiraOcorrenciaAPartirDe_Mensal_Dia31EmFevereiro_ClampAnoComum()
    {
        var molde = MoldeMensal(diaVencimento: 31);

        var data = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2026, 2, 1));

        Assert.Equal(new DateOnly(2026, 2, 28), data);
    }

    [Fact]
    public void PrimeiraOcorrenciaAPartirDe_Mensal_Dia31EmFevereiro_ClampAnoBissexto()
    {
        var molde = MoldeMensal(diaVencimento: 31);

        var data = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2024, 2, 1));

        Assert.Equal(new DateOnly(2024, 2, 29), data);
    }

    [Fact]
    public void PrimeiraOcorrenciaAPartirDe_Anual_MesReferenciaFevereiroDia31_Clampa()
    {
        var molde = MoldeAnual(mesReferencia: 2, diaVencimento: 31);

        var anoComum = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2026, 1, 1));
        var anoBissexto = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, new DateOnly(2024, 1, 1));

        Assert.Equal(new DateOnly(2026, 2, 28), anoComum);
        Assert.Equal(new DateOnly(2024, 2, 29), anoBissexto);
    }

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Mensal_Dia31_AncoraCadaMesNoDiaVencimento_NaoNoClampAnterior()
    {
        // Regra critica: cada mes clampa a partir de DiaVencimento=31, nunca do
        // dia ja clampado do mes anterior. Marco tem 31 dias -> 31/03, nao 28/03.
        var molde = MoldeMensal(diaVencimento: 31);

        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(
            molde, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30));

        Assert.Equal(
            new[]
            {
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 28),
                new DateOnly(2026, 3, 31),
                new DateOnly(2026, 4, 30),
            },
            ocorrencias);
    }

    #endregion

    #region CalcularOcorrenciasNoIntervalo: MENSAL, ANUAL, SEMANAL

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Mensal_RetornaTodasOcorrenciasDoMes()
    {
        var molde = MoldeMensal(diaVencimento: 10);

        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(
            molde, new DateOnly(2026, 8, 1), new DateOnly(2026, 10, 31));

        Assert.Equal(3, ocorrencias.Count);
        Assert.Contains(new DateOnly(2026, 8, 10), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 10), ocorrencias);
        Assert.Contains(new DateOnly(2026, 10, 10), ocorrencias);
    }

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Anual_RetornaOcorrenciasDoMesReferencia()
    {
        var molde = MoldeAnual(mesReferencia: 3, diaVencimento: 15);

        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(
            molde, new DateOnly(2025, 1, 1), new DateOnly(2027, 12, 31));

        Assert.Equal(3, ocorrencias.Count);
        Assert.Contains(new DateOnly(2025, 3, 15), ocorrencias);
        Assert.Contains(new DateOnly(2026, 3, 15), ocorrencias);
        Assert.Contains(new DateOnly(2027, 3, 15), ocorrencias);
    }

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Semanal_RetornaTodasAsQuartas()
    {
        var molde = MoldeSemanal(DiaDaSemana.Quarta);
        var inicio = new DateOnly(2026, 8, 24); // Segunda
        var fim = new DateOnly(2026, 9, 13);   // Domingo

        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);

        Assert.Equal(3, ocorrencias.Count);
        Assert.Contains(new DateOnly(2026, 8, 26), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 2), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 9), ocorrencias);
    }

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Semanal_InicioAntesDoAlvoNaSemana_IncluiOAlvoDaSemana()
    {
        var molde = MoldeSemanal(DiaDaSemana.Quarta);
        var inicio = new DateOnly(2026, 8, 25); // Terca
        var fim = new DateOnly(2026, 9, 20);

        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);

        Assert.Equal(4, ocorrencias.Count);
        Assert.Contains(new DateOnly(2026, 8, 26), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 2), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 9), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 16), ocorrencias);
    }

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_Semanal_InicioNoProprioQuarta_Entra()
    {
        var molde = MoldeSemanal(DiaDaSemana.Quarta);
        var inicio = new DateOnly(2026, 8, 26); // Quarta
        var fim = new DateOnly(2026, 9, 10);

        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);

        Assert.Equal(3, ocorrencias.Count);
        Assert.Contains(new DateOnly(2026, 8, 26), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 2), ocorrencias);
        Assert.Contains(new DateOnly(2026, 9, 9), ocorrencias);
    }

    [Fact]
    public void CalcularOcorrenciasNoIntervalo_InicioDepoisDoFim_RetornaVazio()
    {
        var molde = MoldeMensal(diaVencimento: 10);

        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(
            molde, new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 1));

        Assert.Empty(ocorrencias);
    }

    #endregion

    #region CriarOcorrenciaPendente: ContaReceber com propriedades corretas

    [Fact]
    public void CriarOcorrenciaPendente_GeraRecebivelPendenteComPropriedadesCorretas()
    {
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

        var contaReceber = RecebivelRecorrenteOcorrenciaFactory.CriarOcorrenciaPendente(molde, dataOcorrencia, dataGeracao);

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
    public void CriarOcorrenciaPendente_SemCategoria_PessoaSempreNula()
    {
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

        var contaReceber = RecebivelRecorrenteOcorrenciaFactory.CriarOcorrenciaPendente(
            molde, new DateOnly(2026, 8, 28), new DateOnly(2026, 8, 21));

        Assert.Null(contaReceber.Pessoa);
        Assert.Null(contaReceber.CategoriaId);
        Assert.Equal(moldeId, contaReceber.RecebivelRecorrenteId);
    }

    #endregion

    private static RecebivelRecorrente MoldeMensal(int diaVencimento) => new()
    {
        Id = Guid.NewGuid(),
        Descricao = "Receita mensal",
        Valor = 1000m,
        Periodicidade = PeriodicidadeRecebivel.Mensal,
        DiaVencimento = diaVencimento,
        Ativa = true
    };

    private static RecebivelRecorrente MoldeAnual(int mesReferencia, int diaVencimento) => new()
    {
        Id = Guid.NewGuid(),
        Descricao = "Receita anual",
        Valor = 5000m,
        Periodicidade = PeriodicidadeRecebivel.Anual,
        MesReferencia = mesReferencia,
        DiaVencimento = diaVencimento,
        Ativa = true
    };

    private static RecebivelRecorrente MoldeSemanal(DiaDaSemana diaDaSemana) => new()
    {
        Id = Guid.NewGuid(),
        Descricao = "Receita semanal",
        Valor = 100m,
        Periodicidade = PeriodicidadeRecebivel.Semanal,
        DiaDaSemana = diaDaSemana,
        Ativa = true
    };
}
