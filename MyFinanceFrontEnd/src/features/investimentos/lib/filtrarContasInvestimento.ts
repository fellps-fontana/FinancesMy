import type { ContaResponse } from "@/features/contas/types"

export type OpcaoConta = { value: string; label: string }

// Opcoes {id, nome} para o SelectField de conta.
export function paraOpcoesDeConta(contas: ReadonlyArray<ContaResponse>): OpcaoConta[] {
  return contas.map((conta) => ({ value: conta.id, label: conta.nome }))
}

// Contas elegiveis para vincular a um ativo: tipo Investimento e ativas
// (item 8.5). O backend valida de novo (400 se nao for de investimento).
export function filtrarContasInvestimento(contas: ReadonlyArray<ContaResponse>): ContaResponse[] {
  return contas.filter((conta) => conta.tipo === "Investimento" && conta.ativa)
}
