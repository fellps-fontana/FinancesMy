import type { RecebivelRecorrenteResponse } from "@/features/recebiveis-recorrentes/types"

// Total recorrente mensal = soma do valor dos moldes ATIVOS e MENSAIS (mesma
// regra do "Total fixo mensal" de Contas fixas). Anuais e semanais ficam
// fora: nao ocorrem uma vez por mes. Soma de exibicao, sem regra de dominio.
export function somarTotalRecorrenteMensal(recebiveis: ReadonlyArray<RecebivelRecorrenteResponse>): number {
  return recebiveis
    .filter((recebivel) => recebivel.ativa && recebivel.periodicidade === "MENSAL")
    .reduce((total, recebivel) => total + recebivel.valor, 0)
}
