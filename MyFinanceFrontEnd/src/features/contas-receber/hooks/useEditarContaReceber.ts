import { useMutation, useQueryClient } from "@tanstack/react-query"
import { editarContaReceber } from "@/features/contas-receber/api"
import { contasReceberKeys } from "@/features/contas-receber/query-keys"
import type { EditarContaReceberRequest } from "@/features/contas-receber/types"

type EditarContaReceberVariables = {
  id: string
  request: EditarContaReceberRequest
}

// Editar pode mudar data prevista ou valor total, o que afeta o total
// esperado do mes - invalida lista, registro pontual e total esperado.
export function useEditarContaReceber() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: EditarContaReceberVariables) =>
      editarContaReceber(id, request),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.lista() })
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.porId(id) })
      queryClient.invalidateQueries({ queryKey: [...contasReceberKeys.all, "totalEsperadoMes"] })
    },
  })
}
