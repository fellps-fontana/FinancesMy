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
        throw new NotImplementedException();
    }
}
