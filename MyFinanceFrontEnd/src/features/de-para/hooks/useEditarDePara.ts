import { useMutation, useQueryClient } from "@tanstack/react-query"
import { editarDePara } from "@/features/de-para/api"
import { deParaKeys } from "@/features/de-para/query-keys"
import type { EditarDeParaCategoriaRequest } from "@/features/de-para/types"

type EditarDeParaVariables = {
  id: string
  request: EditarDeParaCategoriaRequest
}

export function useEditarDePara() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: EditarDeParaVariables) => editarDePara(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: deParaKeys.lista() })
    },
  })
}
