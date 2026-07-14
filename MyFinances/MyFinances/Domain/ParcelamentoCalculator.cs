namespace MyFinances.Domain;

// Calculo puro do split de valor de uma compra parcelada (item 12,
// regra-de-negocio.md). Sem I/O, sem estado — recebe valor_total e
// quantidade_parcelas, devolve o valor de cada parcela na ordem 1..N.
public static class ParcelamentoCalculator
{
    // Divide valorTotal em quantidadeParcelas partes. Cada parcela, exceto
    // a ultima, e truncada em 2 casas decimais; a ultima recebe o resto do
    // arredondamento, garantindo que a soma das partes bate exatamente com
    // valorTotal. Lanca ArgumentException se valorTotal <= 0 ou
    // quantidadeParcelas < 2.
    public static IReadOnlyList<decimal> CalcularValoresParcelas(decimal valorTotal, int quantidadeParcelas)
    {
        if (quantidadeParcelas < 2)
        {
            throw new ArgumentException("Quantidade de parcelas deve ser no mínimo 2.", nameof(quantidadeParcelas));
        }

        if (valorTotal <= 0)
        {
            throw new ArgumentException("Valor total deve ser maior que zero.", nameof(valorTotal));
        }

        var parcelas = new List<decimal>();

        for (int i = 0; i < quantidadeParcelas - 1; i++)
        {
            decimal valorParcela = Math.Floor(valorTotal / quantidadeParcelas * 100) / 100;
            parcelas.Add(valorParcela);
        }

        decimal somaParcelas = parcelas.Aggregate(0m, (acc, val) => acc + val);
        decimal ultimaParcela = valorTotal - somaParcelas;
        parcelas.Add(ultimaParcela);

        return parcelas.AsReadOnly();
    }
}
