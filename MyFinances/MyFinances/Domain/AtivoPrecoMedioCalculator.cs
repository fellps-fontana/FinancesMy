namespace MyFinances.Domain;

// Funcao pura - formula da regra-de-negocio.md item 8.1:
// preco_medio_novo = (preco_medio_atual * qtd_atual + preco_aporte * qtd_aporte)
//                    / (qtd_atual + qtd_aporte)
public static class AtivoPrecoMedioCalculator
{
    public static decimal Calcular(decimal precoMedioAtual, decimal qtdAtual, decimal precoAporte, decimal qtdAporte)
    {
        return (precoMedioAtual * qtdAtual + precoAporte * qtdAporte) / (qtdAtual + qtdAporte);
    }
}
