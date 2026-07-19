namespace MyFinances.Domain;

// Regra de sinal (regra-de-negocio.md item 2, CRITICA): o sinal do campo Valor
// nao e confiavel para determinar entrada ou saida. Esta funcao usa APENAS Tipo
// (Debit/Credit) e os vinculos estruturais (TransferenciaId/FaturaId) do
// lancamento -- nunca le Valor. Precedencia: Transferencia > CompetenciaCartao > Tipo.
public static class ClassificacaoLancamentoService
{
    public static ClassificacaoLancamento Classificar(Lancamento lancamento)
    {
        throw new NotImplementedException();
    }
}
