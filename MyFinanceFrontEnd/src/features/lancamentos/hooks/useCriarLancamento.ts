import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarLancamento } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"
import type { CriarLancamentoRequest } from "@/features/lancamentos/types"

type CriarLancamentoVariables = {
  contaId: string
  request: CriarLancamentoRequest
}

// Criar um lancamento muda o fluxo de caixa daquela conta - invalida so a
// query da conta afetada.
export function useCriarLancamento() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, request }: CriarLancamentoVariables) =>
      criarLancamento(contaId, request),
    onSuccess: (_data, { contaId }) => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.fluxoCaixa(contaId) })
    },
  })
}
