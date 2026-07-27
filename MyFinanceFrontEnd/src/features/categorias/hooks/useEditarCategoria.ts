import { useMutation, useQueryClient } from "@tanstack/react-query"
import { editarCategoria } from "@/features/categorias/api"
import { categoriasKeys } from "@/features/categorias/query-keys"
import type { EditarCategoriaRequest } from "@/features/categorias/types"

type EditarCategoriaVariables = {
  id: string
  request: EditarCategoriaRequest
}

// Tipo e imutavel (regra-de-negocio.md item 7) - EditarCategoriaRequest so
// tem nome/parentId, nao ha hook separado para mudar tipo.
export function useEditarCategoria() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: EditarCategoriaVariables) => editarCategoria(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: categoriasKeys.lista() })
    },
  })
}
