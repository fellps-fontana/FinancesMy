import type { FluxoCaixaItem } from "@/features/lancamentos/types"

export type ContagemParcelas = {
  pendentes: number
  pagas: number
}

export const SEM_PARCELAS: ContagemParcelas = { pendentes: 0, pagas: 0 }

// Contagem exibida no dialogo de cancelamento (item 17). So informa o usuario;
// quem exclui e decide o que e PENDENTE e o backend.
export function contarParcelasPorCompra(itens: FluxoCaixaItem[]): Map<string, ContagemParcelas> {
  const contagens = new Map<string, ContagemParcelas>()

  for (const item of itens) {
    if (item.tipoItem !== "LANCAMENTO" || item.lancamento.compraParceladaId === null) continue

    const atual = contagens.get(item.lancamento.compraParceladaId) ?? { ...SEM_PARCELAS }
    if (item.lancamento.status === "PENDENTE") atual.pendentes += 1
    if (item.lancamento.status === "PAGO") atual.pagas += 1
    contagens.set(item.lancamento.compraParceladaId, atual)
  }

  return contagens
}
