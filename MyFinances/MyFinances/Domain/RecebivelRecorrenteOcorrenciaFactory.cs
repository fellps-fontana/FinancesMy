namespace MyFinances.Domain;

// Regra critica (regra-de-negocio.md item 15): constroi a ContaReceber PENDENTE
// que um RecebivelRecorrente materializa para uma data de ocorrencia, e calcula
// as datas de ocorrencia ancoradas na periodicidade. Funcao PURA -- nao persiste,
// nao checa idempotencia (isso e do Gerador + Repository). Dia clampado para o
// ultimo dia do mes quando DiaVencimento excede os dias do mes (mesmo padrao de
// ContaFixaLancamentoFactory / FaturaCicloService.CriarDataValida).
public static class RecebivelRecorrenteOcorrenciaFactory
{
    // Cria a ocorrencia (ContaReceber tipo RECEBIVEL, status PENDENTE) para a
    // data informada. Herda categoria do molde. Pessoa sempre null. ValorTotal
    // = molde.Valor. DataPrevista = dataOcorrencia. DataRegistro = dataGeracao.
    public static ContaReceber CriarOcorrenciaPendente(
        RecebivelRecorrente molde, DateOnly dataOcorrencia, DateOnly dataGeracao)
    {
        return new ContaReceber
        {
            Id = Guid.NewGuid(),
            Tipo = TipoContaReceber.Recebivel,
            Descricao = molde.Descricao,
            Pessoa = null,
            ValorTotal = molde.Valor,
            DataRegistro = dataGeracao,
            DataPrevista = dataOcorrencia,
            CategoriaId = molde.CategoriaId,
            Status = StatusContaReceber.Pendente,
            RecebivelRecorrenteId = molde.Id
        };
    }

    // Primeira data de ocorrencia do molde >= minInclusive. E a "proxima
    // ocorrencia" quando minInclusive = hoje (usada pela janela do gerador).
    public static DateOnly PrimeiraOcorrenciaAPartirDe(RecebivelRecorrente molde, DateOnly minInclusive)
    {
        switch (molde.Periodicidade)
        {
            case PeriodicidadeRecebivel.Mensal:
            {
                var candidata = DataAncorada(minInclusive.Year, minInclusive.Month, molde.DiaVencimento!.Value);
                if (candidata < minInclusive)
                {
                    var proximo = new DateOnly(minInclusive.Year, minInclusive.Month, 1).AddMonths(1);
                    candidata = DataAncorada(proximo.Year, proximo.Month, molde.DiaVencimento.Value);
                }
                return candidata;
            }
            case PeriodicidadeRecebivel.Anual:
            {
                var candidata = DataAncorada(minInclusive.Year, molde.MesReferencia!.Value, molde.DiaVencimento!.Value);
                if (candidata < minInclusive)
                {
                    candidata = DataAncorada(minInclusive.Year + 1, molde.MesReferencia.Value, molde.DiaVencimento.Value);
                }
                return candidata;
            }
            case PeriodicidadeRecebivel.Semanal:
            {
                var alvo = (int)molde.DiaDaSemana!.Value.ToDayOfWeek();
                var dias = ((alvo - (int)minInclusive.DayOfWeek) + 7) % 7;
                return minInclusive.AddDays(dias);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(molde));
        }
    }

    // Todas as datas de ocorrencia do molde dentro de [inicio, fim] (inclusive):
    // Mensal -> DiaVencimento clampado de cada mes; Anual -> MesReferencia/
    // DiaVencimento clampado de cada ano; Semanal -> a partir do primeiro
    // DiaDaSemana alvo >= inicio, somando 7 dias.
    public static IReadOnlyList<DateOnly> CalcularOcorrenciasNoIntervalo(
        RecebivelRecorrente molde, DateOnly inicio, DateOnly fim)
    {
        var ocorrencias = new List<DateOnly>();
        if (inicio > fim)
        {
            return ocorrencias;
        }

        switch (molde.Periodicidade)
        {
            case PeriodicidadeRecebivel.Mensal:
            {
                var mes = new DateOnly(inicio.Year, inicio.Month, 1);
                while (mes <= fim)
                {
                    var data = DataAncorada(mes.Year, mes.Month, molde.DiaVencimento!.Value);
                    if (data >= inicio && data <= fim)
                    {
                        ocorrencias.Add(data);
                    }
                    mes = mes.AddMonths(1);
                }
                break;
            }
            case PeriodicidadeRecebivel.Anual:
            {
                for (var ano = inicio.Year; ano <= fim.Year; ano++)
                {
                    var data = DataAncorada(ano, molde.MesReferencia!.Value, molde.DiaVencimento!.Value);
                    if (data >= inicio && data <= fim)
                    {
                        ocorrencias.Add(data);
                    }
                }
                break;
            }
            case PeriodicidadeRecebivel.Semanal:
            {
                var data = PrimeiraOcorrenciaAPartirDe(molde, inicio);
                while (data <= fim)
                {
                    ocorrencias.Add(data);
                    data = data.AddDays(7);
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(molde));
        }

        return ocorrencias;
    }

    // Dia clampado ao ultimo dia do mes quando dia excede os dias do mes/ano.
    private static DateOnly DataAncorada(int ano, int mes, int dia)
    {
        var diasNoMes = DateTime.DaysInMonth(ano, mes);
        return new DateOnly(ano, mes, Math.Min(dia, diasNoMes));
    }
}
