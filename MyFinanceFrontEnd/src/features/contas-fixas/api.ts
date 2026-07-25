import { apiClient } from "@/shared/api/client"
import type {
  ContaFixaResponse,
  CriarContaFixaRequest,
  EditarContaFixaRequest,
} from "@/features/contas-fixas/types"

export function listarContasFixas(ativa?: boolean): Promise<ContaFixaResponse[]> {
  const query = ativa !== undefined ? `?ativa=${ativa}` : ""
  return apiClient.get<ContaFixaResponse[]>(`/api/contas-fixas${query}`)
}

export function obterContaFixaPorId(id: string): Promise<ContaFixaResponse> {
  return apiClient.get<ContaFixaResponse>(`/api/contas-fixas/${id}`)
}

export function criarContaFixa(request: CriarContaFixaRequest): Promise<ContaFixaResponse> {
  return apiClient.post<ContaFixaResponse>("/api/contas-fixas", request)
}

export function editarContaFixa(
  id: string,
  request: EditarContaFixaRequest,
): Promise<ContaFixaResponse> {
  return apiClient.put<ContaFixaResponse>(`/api/contas-fixas/${id}`, request)
}

export function desativarContaFixa(id: string): Promise<void> {
  return apiClient.post<void>(`/api/contas-fixas/${id}/desativar`)
}

export function reativarContaFixa(id: string): Promise<void> {
  return apiClient.post<void>(`/api/contas-fixas/${id}/reativar`)
}
