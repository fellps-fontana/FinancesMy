import { apiClient } from "@/shared/api/client"
import type {
  PlanoMensalResponse,
  SalvarPlanoMensalRequest,
} from "@/features/planejamento-mensal/types"

// 404 = mes sem plano salvo; quem decide tratar como "sem plano" e o hook.
export function buscarPlanoMensal(ano: number, mes: number): Promise<PlanoMensalResponse> {
  return apiClient.get<PlanoMensalResponse>(`/api/planejamento-mensal/${ano}/${mes}`)
}

export function salvarPlanoMensal(
  ano: number,
  mes: number,
  request: SalvarPlanoMensalRequest,
): Promise<PlanoMensalResponse> {
  return apiClient.put<PlanoMensalResponse>(`/api/planejamento-mensal/${ano}/${mes}`, request)
}
