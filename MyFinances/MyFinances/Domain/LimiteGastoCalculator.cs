namespace MyFinances.Domain;

public static class LimiteGastoCalculator
{
    public static LimiteGastoStatus Calcular(LimiteGasto limiteGasto, IEnumerable<Lancamento> lancamentosDoPeriodo)
    {
        var gastoRealizado = lancamentosDoPeriodo
            .Where(l => l.Tipo == TipoLancamento.Debit && !l.Oculto)
            .Sum(l => l.Valor);

        var percentualUtilizado = limiteGasto.ValorLimite > 0
            ? gastoRealizado / limiteGasto.ValorLimite
            : 0m;

        var estourado = gastoRealizado > limiteGasto.ValorLimite;

        return new LimiteGastoStatus(limiteGasto.ValorLimite, gastoRealizado, percentualUtilizado, estourado);
    }
}

public record LimiteGastoStatus(decimal ValorLimite, decimal GastoRealizado, decimal PercentualUtilizado, bool Estourado);
