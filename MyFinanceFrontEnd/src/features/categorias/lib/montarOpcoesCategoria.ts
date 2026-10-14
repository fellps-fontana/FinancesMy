import type { CategoriaResponse } from "@/features/categorias/types"

export type OpcaoCategoria = {
  id: string
  label: string
  icone?: string
}

// Achata categoria + subcategorias (regra-de-negocio.md item 7); arquivada
// nunca e selecionavel. GET /api/categorias devolve subcategorias tambem
// soltas na raiz, entao so itens sem parentId iniciam a travessia.
export function montarOpcoesCategoria(categorias: CategoriaResponse[]): OpcaoCategoria[] {
  const opcoes: OpcaoCategoria[] = []

  for (const categoria of categorias) {
    if (categoria.parentId) continue

    if (!categoria.arquivada) {
      opcoes.push({ id: categoria.id, label: categoria.nome, icone: categoria.icone })
    }
    for (const sub of categoria.subcategorias) {
      if (!sub.arquivada) {
        opcoes.push({ id: sub.id, label: `${categoria.nome} > ${sub.nome}`, icone: sub.icone })
      }
    }
  }

  return opcoes
}
