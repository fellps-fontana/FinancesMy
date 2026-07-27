import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarCategoria } from "@/features/categorias/api"
import { categoriasKeys } from "@/features/categorias/query-keys"
import type { CriarCategoriaRequest } from "@/features/categorias/types"

// Criar subcategoria (parentId preenchido) tambem so precisa invalidar a
// lista: a hierarquia inteira vem aninhada num unico GET
// (regra-de-negocio.md item 7), nao ha chave separada por parentId.
export function useCriarCategoria() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarCategoriaRequest) => criarCategoria(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: categoriasKeys.lista() })
    },
  })
}
