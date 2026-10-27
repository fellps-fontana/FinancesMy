type ComHierarquia = { categoriaId: string; parentId: string | null }

export type GrupoSublimite<T extends ComHierarquia> = { pai: T; filhos: T[] }

// Apresentacao em 2 niveis (item 7): pai seguido das subcategorias dele.
export function agruparPorPai<T extends ComHierarquia>(itens: T[]): GrupoSublimite<T>[] {
  return itens
    .filter((i) => i.parentId === null)
    .map((pai) => ({ pai, filhos: itens.filter((i) => i.parentId === pai.categoriaId) }))
}
