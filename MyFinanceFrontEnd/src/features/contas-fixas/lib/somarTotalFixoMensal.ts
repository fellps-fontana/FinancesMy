import type { ContaFixaResponse } from "@/features/contas-fixas/types"

// O backend nao expoe total agregado de contas fixas: soma simples, so para
// exibicao, dos valores das contas ATIVAS e MENSAIS (anuais nao pesam todo
// mes). Nao alimenta nenhuma regra de dominio.
export function somarTotalFixoMensal(contasFixas: ContaFixaResponse[]): number {
  return contasFixas
    .filter((contaFixa) => contaFixa.ativa && contaFixa.periodicidade === "MENSAL")
    .reduce((total, contaFixa) => total + contaFixa.valor, 0)
}
