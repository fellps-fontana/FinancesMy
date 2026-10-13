import type { ParcelaElegivelResponse } from "@/features/cartao/types"

export type PreviaAntecipacao = {
  parcelas: ParcelaElegivelResponse[]
  quantidade: number
  valorTotal: number
}

// Apenas selecao e soma para exibicao: a antecipacao vai das ULTIMAS para
// tras (regra item 12), entao as N elegiveis de maior parcelaNumero (ordem
// decrescente; ex.: elegiveis 4..10, quantidade 2 => parcelas 10 e 9) sao as
// que o backend move para a fatura atual. O
// valor definitivo vem de AnteciparParcelasResponse.valorAntecipado.
export function montarPreviaAntecipacao(
  elegiveis: ParcelaElegivelResponse[],
  quantidade: number,
): PreviaAntecipacao {
  const n = Math.max(0, Math.min(Math.floor(quantidade), elegiveis.length))
  const parcelas = [...elegiveis].sort((a, b) => b.parcelaNumero - a.parcelaNumero).slice(0, n)
  const valorTotal = parcelas.reduce((soma, p) => soma + p.valor, 0)
  return { parcelas, quantidade: parcelas.length, valorTotal }
}
