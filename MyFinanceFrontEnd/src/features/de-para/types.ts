import type { CategoriaResponse } from "@/features/categorias/types"

export type DeParaCategoriaResponse = {
  id: string
  categoriaPierre: string
  categoriaId: string
  categoria: CategoriaResponse | null
}

export type CriarDeParaCategoriaRequest = {
  categoriaPierre: string
  categoriaId: string
}

export type EditarDeParaCategoriaRequest = {
  categoriaId: string
}
