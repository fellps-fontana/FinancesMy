import { useMutation, useQueryClient } from "@tanstack/react-query"
import { arquivarCategoria } from "@/features/categorias/api"
import { categoriasKeys } from "@/features/categorias/query-keys"

// Arquivar e soft-delete (arquivada = true, regra-de-negocio.md item 7) -
// invalida a lista, que e a unica query desta feature.
export function useArquivarCategoria() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => arquivarCategoria(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: categoriasKeys.lista() })
    },
  })
}
