import { apiClient } from "@/shared/api/client"
import type {
  DefinirLimiteGastoRequest,
  GastoVsLimiteResponse,
  LimiteGastoResponse,
} from "@/features/limite-gasto/types"

export function listarLimitesGasto(): Promise<LimiteGastoResponse[]> {
  return apiClient.get<LimiteGastoResponse[]>("/api/limites-gasto")
}

// POST e idempotente do lado do dominio: categoria ja com limite so atualiza
// o valor (200), categoria sem limite cria um novo (201) - o controller
// decide o status, o front so repassa o response (regra-de-negocio.md item
// 14: limite_gasto e 1:1 com categoria).
export function definirLimiteGasto(
  request: DefinirLimiteGastoRequest,
): Promise<LimiteGastoResponse> {
  return apiClient.post<LimiteGastoResponse>("/api/limites-gasto", request)
}

export function removerLimiteGasto(categoriaId: string): Promise<void> {
  return apiClient.delete<void>(`/api/limites-gasto/${categoriaId}`)
}

export function buscarGastoVsLimitePorCategoria(
  categoriaId: string,
  ano: number,
  mes: number,
): Promise<GastoVsLimiteResponse> {
  return apiClient.get<GastoVsLimiteResponse>(
    `/api/limites-gasto/gasto-vs-limite/${categoriaId}?ano=${ano}&mes=${mes}`,
  )
}

export function buscarGastoVsLimiteTodasCategorias(
  ano: number,
  mes: number,
): Promise<GastoVsLimiteResponse[]> {
  return apiClient.get<GastoVsLimiteResponse[]>(
    `/api/limites-gasto/gasto-vs-limite?ano=${ano}&mes=${mes}`,
  )
}
