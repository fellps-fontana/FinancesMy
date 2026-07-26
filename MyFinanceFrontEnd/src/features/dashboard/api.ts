import { apiClient } from "@/shared/api/client"
import type { ProjecaoMesResponse } from "@/features/dashboard/types"

export function buscarProjecaoMes(ano: number, mes: number): Promise<ProjecaoMesResponse> {
  return apiClient.get<ProjecaoMesResponse>(`/api/dashboard/projecao-mes?ano=${ano}&mes=${mes}`)
}
