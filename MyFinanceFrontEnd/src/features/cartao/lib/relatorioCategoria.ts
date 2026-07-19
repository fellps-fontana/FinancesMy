import type { RelatorioCategoriaItem } from "@/features/cartao/types"

export type ItemRelatorioCategoriaOrdenado = RelatorioCategoriaItem & {
  /** "Sem categoria" quando `nomeCategoria` vem null (regra de negocio item 7). */
  nomeExibicao: string
  /** Fracao (0-1) do item sobre o total geral do periodo. */
  percentual: number
}

// Soma o total gasto no periodo (todas as categorias, regime de COMPETENCIA -
// regra de negocio item 12). Funcao pura e testavel, fora do componente
// (clean-code.md: "logica de calculo nao vive no componente").
export function calcularTotalGeral(itens: RelatorioCategoriaItem[]): number {
  return itens.reduce((soma, item) => soma + item.total, 0)
}

// Ordena os itens do relatorio por total desc (maior gasto primeiro) e
// calcula o percentual de cada um sobre o total geral. O item com
// `categoriaId = null` (compra sem categoria vinculada, regra de negocio
// item 7) NAO e descartado - recebe o rotulo "Sem categoria" e participa da
// ordenacao e do total normalmente.
export function ordenarItensRelatorio(
  itens: RelatorioCategoriaItem[],
): ItemRelatorioCategoriaOrdenado[] {
  const totalGeral = calcularTotalGeral(itens)

  return [...itens]
    .sort((a, b) => b.total - a.total)
    .map((item) => ({
      ...item,
      nomeExibicao: item.nomeCategoria ?? "Sem categoria",
      percentual: totalGeral > 0 ? item.total / totalGeral : 0,
    }))
}
