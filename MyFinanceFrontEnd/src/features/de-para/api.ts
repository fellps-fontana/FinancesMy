import { apiClient } from "@/shared/api/client"
import type {
  CriarDeParaCategoriaRequest,
  DeParaCategoriaResponse,
  EditarDeParaCategoriaRequest,
} from "@/features/de-para/types"

const BASE = "/api/deparacategorias"

export function listarDePara(categoriaPierre?: string): Promise<DeParaCategoriaResponse[]> {
  const query =
    categoriaPierre !== undefined
      ? `?${new URLSearchParams({ categoriaPierre }).toString()}`
      : ""
  return apiClient.get<DeParaCategoriaResponse[]>(`${BASE}${query}`)
}

export function criarDePara(request: CriarDeParaCategoriaRequest): Promise<DeParaCategoriaResponse> {
  return apiClient.post<DeParaCategoriaResponse>(BASE, request)
}

export function editarDePara(
  id: string,
  request: EditarDeParaCategoriaRequest,
): Promise<DeParaCategoriaResponse> {
  return apiClient.put<DeParaCategoriaResponse>(`${BASE}/${id}`, request)
}

export function removerDePara(id: string): Promise<void> {
  return apiClient.delete<void>(`${BASE}/${id}`)
}
