namespace MyFinances.Domain;

// Regra critica (regra-de-negocio.md item 6): constroi o Lancamento PENDENTE
// que uma ContaFixa gera para um ano/mes. Funcao PURA -- nao persiste, nao
// checa idempotencia (isso e do Service+Repository). Dia clampado para o
// ultimo dia do mes quando DiaVencimento excede os dias do mes (mesmo padrao
// de FaturaCicloService.CriarDataValida).
public static class ContaFixaLancamentoFactory
{
    // Ajusta dia de vencimento para o ultimo dia do mes se o dia informado
    // exceder o numero de dias do mes (ex: 31 em abril vira 30).
    public static int AjustarDiaParaUltimoDiaDoMesSeNecessario(int diaVencimento, int ano, int mes)
    {
        var diasNoMes = DateTime.DaysInMonth(ano, mes);
        return Math.Min(diaVencimento, diasNoMes);
    }

    public static Lancamento CriarLancamentoPendente(ContaFixa contaFixa, int ano, int mes)
    {
        var diaAjustado = AjustarDiaParaUltimoDiaDoMesSeNecessario(contaFixa.DiaVencimento, ano, mes);
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

    // Regra critica (regra-de-negocio.md item 6): calcula a proxima ocorrencia
    // da periodicidade configurada. MENSAL soma 1 mes; ANUAL soma 1 ano.
    // Recalcula o dia respeitando o diaVencimento original da ContaFixa:
    // se diaVencimento = 31 e dataAtual = 30/04 (clampado de abril), a proxima
    // ocorrencia sera 31/05 (maio tem 31 dias), nao 30/05.
    public static DateOnly ProximaOcorrencia(
        DateOnly dataAtual,
        PeriodicidadeContaFixa periodicidade,
        int diaVencimentoOriginal)
    {
        var proximaData = periodicidade switch
        {
            PeriodicidadeContaFixa.Mensal => dataAtual.AddMonths(1),
            PeriodicidadeContaFixa.Anual => dataAtual.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(periodicidade))
        };

        var diaAjustado = AjustarDiaParaUltimoDiaDoMesSeNecessario(
            diaVencimentoOriginal, proximaData.Year, proximaData.Month);

        return new DateOnly(proximaData.Year, proximaData.Month, diaAjustado);
    }
}
