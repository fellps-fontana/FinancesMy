import { useMutation, useQueryClient } from "@tanstack/react-query"
import { removerLancamento } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"

type RemoverLancamentoVariables = {
  contaId: string
  lancamentoId: string
}

// Remover um lancamento muda o fluxo de caixa daquela conta - invalida so a
// query da conta afetada.
export function useRemoverLancamento() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, lancamentoId }: RemoverLancamentoVariables) =>
      removerLancamento(contaId, lancamentoId),
    onSuccess: (_data, { contaId }) => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.fluxoCaixa(contaId) })
    },
  })
}
