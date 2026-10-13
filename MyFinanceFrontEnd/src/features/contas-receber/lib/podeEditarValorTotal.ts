import type { ContaReceberResponse } from "@/features/contas-receber/types"

// Apenas habilita o campo na UI; a regra real e imposta pelo backend (409).
export function podeEditarValorTotal(
  conta: Pick<ContaReceberResponse, "tipo" | "status">,
): boolean {
  return conta.tipo === "RECEBIVEL" && conta.status === "PENDENTE"
}
