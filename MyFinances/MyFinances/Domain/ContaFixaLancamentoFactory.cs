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
}
