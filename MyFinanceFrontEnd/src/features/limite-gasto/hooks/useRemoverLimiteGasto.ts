import { useMutation, useQueryClient } from "@tanstack/react-query"
import { removerLimiteGasto } from "@/features/limite-gasto/api"
import { limiteGastoKeys } from "@/features/limite-gasto/query-keys"

// Remover o limite tira a categoria de qualquer comparativo gasto-vs-limite
// (regra-de-negocio.md item 14) - mesma invalidacao cruzada de
// useDefinirLimiteGasto, cobrindo lista e gasto-vs-limite via `all`.
export function useRemoverLimiteGasto() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (categoriaId: string) => removerLimiteGasto(categoriaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: limiteGastoKeys.all })
    },
  })
}
