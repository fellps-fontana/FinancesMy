import { apiClient } from "@/shared/api/client"
import type {
  AssinaturaCartaoResponse,
  CriarAssinaturaCartaoRequest,
  EditarAssinaturaCartaoRequest,
} from "@/features/assinaturas-cartao/types"

export function listarAssinaturasCartao(
  contaId?: string,
  ativa?: boolean,
): Promise<AssinaturaCartaoResponse[]> {
  const params = new URLSearchParams()
  if (contaId !== undefined) params.set("contaId", contaId)
  if (ativa !== undefined) params.set("ativa", String(ativa))
  const query = params.toString()
  return apiClient.get<AssinaturaCartaoResponse[]>(
    `/api/assinaturas-cartao${query ? `?${query}` : ""}`,
  )
}

export function obterAssinaturaCartaoPorId(id: string): Promise<AssinaturaCartaoResponse> {
  return apiClient.get<AssinaturaCartaoResponse>(`/api/assinaturas-cartao/${id}`)
}

export function criarAssinaturaCartao(
  request: CriarAssinaturaCartaoRequest,
): Promise<AssinaturaCartaoResponse> {
  return apiClient.post<AssinaturaCartaoResponse>("/api/assinaturas-cartao", request)
}

export function editarAssinaturaCartao(
  id: string,
  request: EditarAssinaturaCartaoRequest,
): Promise<AssinaturaCartaoResponse> {
  return apiClient.put<AssinaturaCartaoResponse>(`/api/assinaturas-cartao/${id}`, request)
}

export function desativarAssinaturaCartao(id: string): Promise<void> {
  return apiClient.post<void>(`/api/assinaturas-cartao/${id}/desativar`)
}

export function reativarAssinaturaCartao(id: string): Promise<void> {
  return apiClient.post<void>(`/api/assinaturas-cartao/${id}/reativar`)
}
