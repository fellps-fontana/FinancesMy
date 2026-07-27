import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarTransferencia } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"
import type { CriarTransferenciaRequest } from "@/features/lancamentos/types"

// Transferencia entre contas de mesma titularidade (regra-de-negocio.md item
// 3) e representada como duas pernas - uma saida na origem, uma entrada no
// destino. Por isso invalida o fluxo de caixa das DUAS contas envolvidas, nao
// so a de origem.
export function useCriarTransferencia() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarTransferenciaRequest) => criarTransferencia(request),
    onSuccess: (_data, request) => {
      queryClient.invalidateQueries({
        queryKey: lancamentosKeys.fluxoCaixa(request.contaOrigemId),
      })
      queryClient.invalidateQueries({
        queryKey: lancamentosKeys.fluxoCaixa(request.contaDestinoId),
      })
    },
  })
}
