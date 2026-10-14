import { useMutation, useQueryClient } from "@tanstack/react-query"
import { editarConta } from "@/features/contas/api"
import { contasKeys } from "@/features/contas/query-keys"
import type { EditarContaRequest } from "@/features/contas/types"

type EditarContaVariables = {
  id: string
  request: EditarContaRequest
}

// Edicao de conta (item 10.2): invalida as listas de contas para nome, icone,
// cor e subtitulo refletirem sem reload.
export function useEditarConta() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: EditarContaVariables) => editarConta(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contasKeys.all })
    },
  })
}
