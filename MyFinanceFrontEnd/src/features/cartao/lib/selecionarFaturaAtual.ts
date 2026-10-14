import type { FaturaResponse } from "@/features/cartao/types"

function porVencimentoCrescente(a: FaturaResponse, b: FaturaResponse): number {
  return a.dataVencimento.localeCompare(b.dataVencimento)
}

// Selecao (nao calculo) da fatura "atual" para destaque na tela:
// 1) a ABERTA (acumula compras e recebe a antecipacao, regra item 12);
// 2) sem ABERTA, a nao-FECHADA de menor vencimento (PAGA atual paga
//    antecipadamente ou FUTURA);
// 3) so entao a mais recente. Lista vazia devolve null. Valores e status
//    continuam vindos do backend.
export function selecionarFaturaAtual(faturas: FaturaResponse[]): FaturaResponse | null {
  if (faturas.length === 0) return null

  const aberta = faturas.filter((f) => f.status === "ABERTA").sort(porVencimentoCrescente)[0]
  if (aberta !== undefined) return aberta

  const naoFechada = faturas.filter((f) => f.status !== "FECHADA").sort(porVencimentoCrescente)[0]
  if (naoFechada !== undefined) return naoFechada

  return [...faturas].sort((a, b) => b.dataVencimento.localeCompare(a.dataVencimento))[0]
}
