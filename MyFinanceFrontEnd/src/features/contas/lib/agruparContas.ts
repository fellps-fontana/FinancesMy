import type { ContaResponse, TipoConta } from "@/features/contas/types"

export type GrupoContas = {
  tipo: TipoConta
  titulo: string
  contas: ContaResponse[]
}

const ORDEM_GRUPOS: ReadonlyArray<{ tipo: TipoConta; titulo: string }> = [
  { tipo: "Banco", titulo: "Contas bancarias" },
  { tipo: "Investimento", titulo: "Investimentos" },
  { tipo: "Cartao", titulo: "Cartoes" },
]

// Agrupa as contas por tipo, na ordem Banco / Investimento / Cartao, e omite
// grupos vazios. Funcao pura de exibicao (nenhum valor e recalculado).
export function agruparContas(contas: ReadonlyArray<ContaResponse>): GrupoContas[] {
  return ORDEM_GRUPOS.map(({ tipo, titulo }) => ({
    tipo,
    titulo,
    contas: contas.filter((conta) => conta.tipo === tipo),
  })).filter((grupo) => grupo.contas.length > 0)
}
