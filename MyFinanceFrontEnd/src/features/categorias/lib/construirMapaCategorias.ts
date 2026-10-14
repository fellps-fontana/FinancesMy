import type { CategoriaResponse } from "@/features/categorias/types"

export type CategoriaResolvida = { nome: string; icone?: string }

// Mapa id -> nome/icone, incluindo subcategorias. Quem quiser resolver
// categoria arquivada (item 7: arquivar nao apaga o vinculo) passa as listas
// de ativas e arquivadas juntas.
export function construirMapaCategorias(
  categorias: CategoriaResponse[],
): Record<string, CategoriaResolvida> {
  const mapa: Record<string, CategoriaResolvida> = {}

  for (const categoria of categorias) {
    mapa[categoria.id] = { nome: categoria.nome, icone: categoria.icone }
    for (const sub of categoria.subcategorias) {
      mapa[sub.id] = { nome: sub.nome, icone: sub.icone }
    }
  }

  return mapa
}
