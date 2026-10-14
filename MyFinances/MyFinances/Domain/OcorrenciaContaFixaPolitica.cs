namespace MyFinances.Domain;

// Item 6: criterio unico de consumada / nao consumada de uma ocorrencia de ContaFixa.
// Banco (Fatura == null): consumada <=> Status == Pago; nao consumada <=> Status != Pago (Pendente, Sugerido).
// Cartao (Fatura != null): consumada <=> Fatura.Status == Paga (Status do lancamento e irrelevante).
public static class OcorrenciaContaFixaPolitica
{
    public static bool EhConsumada(Lancamento ocorrencia)
    {
        return ocorrencia.Fatura == null
            ? ocorrencia.Status == StatusLancamento.Pago
            : ocorrencia.Fatura.Status == StatusFatura.Paga;
    }

    public static bool EhNaoConsumada(Lancamento ocorrencia) => !EhConsumada(ocorrencia);
}
