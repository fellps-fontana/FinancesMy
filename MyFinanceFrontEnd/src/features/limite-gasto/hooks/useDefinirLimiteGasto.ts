import { useMutation, useQueryClient } from "@tanstack/react-query"
import { definirLimiteGasto } from "@/features/limite-gasto/api"
import { limiteGastoKeys } from "@/features/limite-gasto/query-keys"
import type { DefinirLimiteGastoRequest } from "@/features/limite-gasto/types"

// Definir/atualizar valor_limite muda todo calculo de gasto-vs-limite
// (regra-de-negocio.md item 14) - invalida `all` pra cobrir lista e
// gasto-vs-limite (qualquer categoria/periodo) de uma vez, sem espalhar
// chave por chave aqui.
export function useDefinirLimiteGasto() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: DefinirLimiteGastoRequest) => definirLimiteGasto(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: limiteGastoKeys.all })
    },
  })
}
