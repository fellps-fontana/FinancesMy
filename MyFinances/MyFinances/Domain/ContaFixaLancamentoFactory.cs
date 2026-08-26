namespace MyFinances.Domain;

// Regra critica (regra-de-negocio.md item 6): constroi o Lancamento PENDENTE
// que uma ContaFixa gera para um ano/mes. Funcao PURA -- nao persiste, nao
// checa idempotencia (isso e do Service+Repository). Dia clampado para o
// ultimo dia do mes quando DiaVencimento excede os dias do mes (mesmo padrao
// de FaturaCicloService.CriarDataValida).
public static class ContaFixaLancamentoFactory
{
    public static Lancamento CriarLancamentoPendente(ContaFixa contaFixa, int ano, int mes)
    {
        var diasNoMes = DateTime.DaysInMonth(ano, mes);
        var diaAjustado = Math.Min(contaFixa.DiaVencimento, diasNoMes);
        var data = new DateOnly(ano, mes, diaAjustado);

        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaFixa.ContaId,
            CategoriaId = contaFixa.CategoriaId,
            Descricao = contaFixa.Descricao,
            Valor = contaFixa.Valor,
            Data = data,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pendente,
            Manual = true,
            ContaFixaId = contaFixa.Id
        };
    }

    // Regra critica (regra-de-negocio.md item 6, revisao 2026-07-27): unidade
    // de tempo somada entre uma ocorrencia e a proxima depende da
    // periodicidade -- Mensal soma 1 mes, Anual soma 1 ano.
    public static DateOnly ProximaOcorrencia(DateOnly dataAtual, PeriodicidadeContaFixa periodicidade)
    {
        var proximaData = periodicidade switch
        {
            PeriodicidadeContaFixa.Mensal => dataAtual.AddMonths(1),
            PeriodicidadeContaFixa.Anual => dataAtual.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(periodicidade))
        };

        var diasNoMes = DateTime.DaysInMonth(proximaData.Year, proximaData.Month);
        var diaAjustado = Math.Min(proximaData.Day, diasNoMes);

        return new DateOnly(proximaData.Year, proximaData.Month, diaAjustado);
    }

    // Regra critica (regra-de-negocio.md item 6, geracao sob demanda): decide
    // se o par (ano, mes) e uma ocorrencia valida para esta ContaFixa, dado
    // sua periodicidade. Mensal -> sempre true. Anual -> so quando mes ==
    // contaFixa.MesReferencia.
    public static bool EhOcorrenciaValida(ContaFixa contaFixa, int ano, int mes)
        => contaFixa.Periodicidade switch
        {
            PeriodicidadeContaFixa.Mensal => true,
            PeriodicidadeContaFixa.Anual => mes == contaFixa.MesReferencia,
            _ => false
        };

    // Regra critica (regra-de-negocio.md item 6, limpeza de periodicidade):
    // recalcula o par atual+proxima sob a periodicidade/MesReferencia ATUAIS
    // do objeto ContaFixa passado, a partir de dataReferencia (mesma
    // data-base usada em CriarAsync/ReativarAsync). Funcao pura, nao
    // persiste.
    public static (DateOnly Atual, DateOnly Proxima) CalcularConjuntoAtualEProxima(
        ContaFixa contaFixa, DateOnly dataReferencia)
    {
        DateOnly dataAtualBase = contaFixa.Periodicidade switch
        {
            PeriodicidadeContaFixa.Mensal => dataReferencia,
            PeriodicidadeContaFixa.Anual =>
                new DateOnly(dataReferencia.Year, contaFixa.MesReferencia ?? dataReferencia.Month, 1),
            _ => dataReferencia
        };

        var diasNoMes = DateTime.DaysInMonth(dataAtualBase.Year, dataAtualBase.Month);
        var diaAjustado = Math.Min(contaFixa.DiaVencimento, diasNoMes);
        var dataAtual = new DateOnly(dataAtualBase.Year, dataAtualBase.Month, diaAjustado);

        var dataProxima = ProximaOcorrencia(dataAtual, contaFixa.Periodicidade);

        return (dataAtual, dataProxima);
    }
}
