namespace MyFinances.Domain;

// Calculadora pura do credito gerado por estorno retroativo de compra
// parcelada (regra-de-negocio.md item 12, subsecao "Estorno de compra
// parcelada"). Uma fatura PAGA que recebe um lancamento de estorno pode
// ficar com ValorPendente negativo (credito). Esse credito e abatido
// automaticamente do total da fatura seguinte, sem mudar o status da
// fatura de origem e sem nenhuma acao manual do usuario.
public static class FaturaCreditoCalculator
{
    // Recebe as faturas de UMA conta cartao, em ordem cronologica (por
    // DataVencimento, mais antiga primeiro), e devolve o saldo AJUSTADO de
    // cada uma, encadeando o credito fatura a fatura. O credito que sobra
    // de uma fatura (ValorPendenteAjustado negativo) e passado como
    // entrada para a proxima fatura da lista; uma fatura cujo
    // ValorPendenteAjustado fique positivo ou zero nao propaga nada
    // adiante. Sem estorno nenhum na conta, CreditoRecebido e sempre 0 e
    // ValorPendenteAjustado == ValorPendenteBruto (nenhuma mudanca de
    // comportamento no caso comum).
    public static IReadOnlyList<FaturaSaldoAjustado> CalcularCadeia(
        IReadOnlyList<Fatura> faturasDaContaEmOrdemCronologica)
    {
        var resultado = new List<FaturaSaldoAjustado>();
        decimal creditoPendente = 0m;

        foreach (var fatura in faturasDaContaEmOrdemCronologica)
        {
            var saldo = FaturaSaldoCalculator.Calcular(fatura);
            var valorPendenteBruto = saldo.ValorPendente;

            var creditoRecebido = creditoPendente;
            var valorPendenteAjustado = valorPendenteBruto - creditoRecebido;

            resultado.Add(new FaturaSaldoAjustado(
                fatura.Id,
                saldo.ValorTotal,
                saldo.ValorPago,
                valorPendenteBruto,
                creditoRecebido,
                valorPendenteAjustado));

            // Propagar credito somente se o ajustado ficar negativo
            creditoPendente = valorPendenteAjustado < 0m ? -valorPendenteAjustado : 0m;
        }

        return resultado.AsReadOnly();
    }
}

// CreditoRecebido: credito que entrou nesta fatura, vindo do encadeamento
// da(s) fatura(s) anterior(es) (ou 0, se nao havia credito sobrando).
// ValorPendenteAjustado = ValorPendenteBruto - CreditoRecebido; pode ficar
// negativo, indicando que ainda sobra credito para a proxima fatura da
// cadeia.
public record FaturaSaldoAjustado(
    Guid FaturaId,
    decimal ValorTotal,
    decimal ValorPago,
    decimal ValorPendenteBruto,
    decimal CreditoRecebido,
    decimal ValorPendenteAjustado);
