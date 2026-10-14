import type { FaturaResponse } from "@/features/cartao/types"

// Selecao (nao calculo) da fatura "atual" para destaque na tela: a fatura
// ABERTA de vencimento mais proximo, que e a que ainda acumula compras e para
// onde a antecipacao traz parcelas (regra item 12). Sem ABERTA, cai na mais
// recente. Valores e status continuam vindos do backend.
export function selecionarFaturaAtual(faturas: FaturaResponse[]): FaturaResponse | null {
  if (faturas.length === 0) return null
  const abertas = faturas
    .filter((f) => f.status === "ABERTA")
    .sort((a, b) => a.dataVencimento.localeCompare(b.dataVencimento))
  if (abertas.length > 0) return abertas[0]
  return [...faturas].sort((a, b) => b.dataVencimento.localeCompare(a.dataVencimento))[0]
}
