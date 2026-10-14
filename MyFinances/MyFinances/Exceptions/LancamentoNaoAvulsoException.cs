using MyFinances.Domain;

namespace MyFinances.Exceptions;

// Lancamento com vinculo (ou nao manual) nao edita/exclui pelo endpoint de
// lancamento manual (regra-de-negocio item 18). Controller -> 409 { erro, origemVinculo }.
public class LancamentoNaoAvulsoException : Exception
{
    public OrigemVinculoLancamento Origem { get; }

    public LancamentoNaoAvulsoException(OrigemVinculoLancamento origem)
        : base($"Lancamento nao avulso (origem {origem.ToStorageValue()}) deve ser alterado na origem.")
    {
        Origem = origem;
    }
}
