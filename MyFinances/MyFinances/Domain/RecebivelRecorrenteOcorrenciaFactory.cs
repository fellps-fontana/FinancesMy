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
        => throw new NotImplementedException();

    // Proxima data de ocorrencia a partir de dataAtual: Mensal +1 mes, Anual +1
    // ano, Semanal +7 dias. Clamp de dia ancorado em molde.DiaVencimento
    // (Mensal/Anual). Semanal nao usa DiaVencimento.
    public static DateOnly ProximaOcorrencia(DateOnly dataAtual, RecebivelRecorrente molde)
        => throw new NotImplementedException();

    // Todas as datas de ocorrencia do molde dentro de [inicio, fim] (inclusive):
    // Mensal -> DiaVencimento de cada mes; Anual -> MesReferencia/DiaVencimento de
    // cada ano; Semanal -> a partir da ocorrencia corrente (dia alvo
    // molde.DiaDaSemana dentro da semana de `inicio`, mesmo que ja passou),
    // somando 7 dias. Dia clampado ao ultimo dia do mes quando exceder (Mensal/Anual).
    public static IReadOnlyList<DateOnly> CalcularOcorrenciasNoIntervalo(
        RecebivelRecorrente molde, DateOnly inicio, DateOnly fim)
        => throw new NotImplementedException();
}
