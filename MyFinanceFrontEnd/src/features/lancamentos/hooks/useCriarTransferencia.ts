import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarTransferencia } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"
import type { CriarTransferenciaRequest } from "@/features/lancamentos/types"

// Transferencia entre contas de mesma titularidade (regra-de-negocio.md item
// 3) muda o fluxo de caixa das duas contas envolvidas (origem e destino) -
// uma unica invalidacao por prefixo (lancamentosKeys.all) cobre as duas de
// uma vez, em vez de duas chamadas pontuais por conta.
export function useCriarTransferencia() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarTransferenciaRequest) => criarTransferencia(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.all })
    },
  })
}
