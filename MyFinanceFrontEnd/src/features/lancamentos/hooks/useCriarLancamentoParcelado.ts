import { useMutation, useQueryClient } from "@tanstack/react-query"
import { dashboardKeys } from "@/features/dashboard/query-keys"
import { criarLancamentoParcelado } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"
import type { CriarLancamentoParceladoRequest } from "@/features/lancamentos/types"
import { limiteGastoKeys } from "@/features/limite-gasto/query-keys"

type CriarLancamentoParceladoVariables = {
  contaId: string
  request: CriarLancamentoParceladoRequest
}

// Criar parcelas muda fluxo de caixa, projecao do dashboard e gasto vs limite.
export function useCriarLancamentoParcelado() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, request }: CriarLancamentoParceladoVariables) =>
      criarLancamentoParcelado(contaId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.all })
      queryClient.invalidateQueries({ queryKey: dashboardKeys.all })
      queryClient.invalidateQueries({ queryKey: limiteGastoKeys.all })
    },
  })
}
