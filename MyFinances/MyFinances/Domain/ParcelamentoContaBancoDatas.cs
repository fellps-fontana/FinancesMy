namespace MyFinances.Domain;

// Calculo puro das datas de um lancamento parcelado em conta banco (item 17).
public static class ParcelamentoContaBancoDatas
{
    private const int QuantidadeMinimaParcelas = 2;

    // Parcela i (1-based) cai i-1 meses depois de `primeira`, no mesmo dia da
    // PRIMEIRA parcela; se o mes nao tem o dia, usa o ultimo dia do mes (clamp).
    // Sem deriva: 31/jan -> 28/fev -> 31/mar (sempre a partir de `primeira`).
    // Lanca ArgumentException se quantidade < 2.
    public static IReadOnlyList<DateOnly> Calcular(DateOnly primeira, int quantidade)
    {
        if (quantidade < QuantidadeMinimaParcelas)
        {
            throw new ArgumentException("Quantidade de parcelas deve ser no minimo 2.", nameof(quantidade));
        }

        return Enumerable.Range(0, quantidade)
            .Select(indice => primeira.AddMonths(indice))
            .ToList()
            .AsReadOnly();
    }
}
