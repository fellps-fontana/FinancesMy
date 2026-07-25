import { useMutation, useQueryClient } from "@tanstack/react-query"
import { editarContaFixa } from "@/features/contas-fixas/api"
import { contasFixasKeys } from "@/features/contas-fixas/query-keys"
import type { EditarContaFixaRequest } from "@/features/contas-fixas/types"

type EditarContaFixaVariables = {
  id: string
  request: EditarContaFixaRequest
}

// Editar valor/diaVencimento/categoria propaga para os Lancamentos futuros
// ainda nao pagos vinculados a ContaFixa (regra-de-negocio.md item 6) -
// invalida a lista e o registro pontual.
export function useEditarContaFixa() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: EditarContaFixaVariables) => editarContaFixa(id, request),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: contasFixasKeys.lista() })
      queryClient.invalidateQueries({ queryKey: contasFixasKeys.porId(id) })
    },
  })
}
