import { useMutation, useQueryClient } from "@tanstack/react-query"
import { reativarCategoria } from "@/features/categorias/api"
import { categoriasKeys } from "@/features/categorias/query-keys"

// Reativar volta arquivada para false (regra-de-negocio.md item 7) - mesma
// invalidacao de useArquivarCategoria.
export function useReativarCategoria() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => reativarCategoria(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: categoriasKeys.lista() })
    },
  })
}
