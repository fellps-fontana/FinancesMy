import { useMutation, useQueryClient } from "@tanstack/react-query"
import { editarLancamento } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"
import type { EditarLancamentoRequest } from "@/features/lancamentos/types"

type EditarLancamentoVariables = {
  contaId: string
  lancamentoId: string
  request: EditarLancamentoRequest
}

// Editar um lancamento muda o fluxo de caixa daquela conta - invalida so a
// query da conta afetada.
export function useEditarLancamento() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, lancamentoId, request }: EditarLancamentoVariables) =>
      editarLancamento(contaId, lancamentoId, request),
    onSuccess: (_data, { contaId }) => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.fluxoCaixa(contaId) })
    },
  })
}
