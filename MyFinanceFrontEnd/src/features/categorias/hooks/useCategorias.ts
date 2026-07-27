import { useQuery } from "@tanstack/react-query"
import { listarCategorias } from "@/features/categorias/api"
import { categoriasKeys } from "@/features/categorias/query-keys"
import type { TipoCategoria } from "@/features/categorias/types"

// O backend ja devolve a hierarquia aninhada (subcategorias dentro da
// categoria-pai, regra-de-negocio.md item 7) num unico GET - nenhum calculo
// de arvore acontece aqui, so a leitura da lista.
export function useCategorias(tipo?: TipoCategoria, arquivada?: boolean) {
  return useQuery({
    queryKey: categoriasKeys.lista(tipo, arquivada),
    queryFn: () => listarCategorias(tipo, arquivada),
  })
}
