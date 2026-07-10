import type { AtivoResponse } from "@/features/investimentos/types"

// Valor de mercado da posicao = quantidade x preco_atual (regra-de-negocio.md
// item 8.1: preco_atual e definido manualmente pelo usuario e usado no
// patrimonio; preco_medio e so custo historico, nunca entra aqui). Calculo
// puro e testavel, fora do componente - ver clean-code.md "Organizacao
// (React)".
export function calcularValorAtivo(ativo: AtivoResponse): number {
  return ativo.quantidade * ativo.precoAtual
}
