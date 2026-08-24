import { useMutation, useQueryClient } from "@tanstack/react-query"
import { editarLancamento } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"
import type { EditarLancamentoRequest } from "@/features/lancamentos/types"

type EditarLancamentoVariables = {
  contaId: string
  lancamentoId: string
  request: EditarLancamentoRequest
}

// Editar um lancamento muda o fluxo de caixa - invalida por prefixo
// (lancamentosKeys.all), mesmo raciocinio de useCriarLancamento.ts.
export function useEditarLancamento() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, lancamentoId, request }: EditarLancamentoVariables) =>
      editarLancamento(contaId, lancamentoId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.all })
    },
  })
}
