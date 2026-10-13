namespace MyFinances.Domain;

// Item 19: variacao = (mesAtual - mesAnterior) / mesAnterior, em percentual.
public static class VariacaoMensalCalculator
{
    private const int CasasDecimaisPercentual = 2;
    private const decimal PercentualMaximo = 100m;

    // Null se totalMesAnterior == 0. Percentual (12.50 = +12,5%), 2 casas, AwayFromZero; pode ser negativo.
    public static decimal? Calcular(decimal totalMes, decimal totalMesAnterior)
    {
        if (totalMesAnterior == 0m)
            return null;

        var variacao = (totalMes - totalMesAnterior) * PercentualMaximo / totalMesAnterior;
        return Math.Round(variacao, CasasDecimaisPercentual, MidpointRounding.AwayFromZero);
    }
}
