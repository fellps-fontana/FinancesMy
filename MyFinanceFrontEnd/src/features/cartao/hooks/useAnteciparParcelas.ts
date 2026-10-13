import { useMutation, useQueryClient } from "@tanstack/react-query"
import { anteciparParcelas } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"
import { dashboardKeys } from "@/features/dashboard/query-keys"
import type { AnteciparParcelasRequest } from "@/features/cartao/types"

type AnteciparParcelasVariaveis = {
  contaId: string
  compraParceladaId: string
  request: AnteciparParcelasRequest
}

// Antecipar move parcelas entre faturas: invalida faturas, saldo do cartao,
// elegiveis da compra e a projecao do dashboard (que soma por fatura/mes).
export function useAnteciparParcelas() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, compraParceladaId, request }: AnteciparParcelasVariaveis) =>
      anteciparParcelas(contaId, compraParceladaId, request),
    onSuccess: (_resposta, variaveis) => {
      queryClient.invalidateQueries({ queryKey: cartaoKeys.faturas(variaveis.contaId) })
      queryClient.invalidateQueries({ queryKey: cartaoKeys.saldo(variaveis.contaId) })
      queryClient.invalidateQueries({
        queryKey: cartaoKeys.parcelasElegiveis(variaveis.contaId, variaveis.compraParceladaId),
      })
      queryClient.invalidateQueries({ queryKey: dashboardKeys.all })
    },
  })
}
