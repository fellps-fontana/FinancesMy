import { apiClient } from "@/shared/api/client"
import type {
  CategoriaResponse,
  CriarCategoriaRequest,
  EditarCategoriaRequest,
  TipoCategoria,
} from "@/features/categorias/types"

export function criarCategoria(request: CriarCategoriaRequest): Promise<CategoriaResponse> {
  return apiClient.post<CategoriaResponse>("/api/categorias", request)
}

export function editarCategoria(
  id: string,
  request: EditarCategoriaRequest,
): Promise<CategoriaResponse> {
  return apiClient.put<CategoriaResponse>(`/api/categorias/${id}`, request)
}

// Sem parametro parentId: o backend ja devolve a hierarquia aninhada em
// CategoriaResponse.subcategorias (regra-de-negocio.md item 7) — nao ha
// necessidade de filtrar por pai nesta camada.
export function listarCategorias(
  tipo?: TipoCategoria,
  arquivada?: boolean,
): Promise<CategoriaResponse[]> {
  const params = new URLSearchParams()
  if (tipo !== undefined) params.set("tipo", tipo)
  if (arquivada !== undefined) params.set("arquivada", String(arquivada))

  const query = params.toString()
  return apiClient.get<CategoriaResponse[]>(`/api/categorias${query ? `?${query}` : ""}`)
}

export function arquivarCategoria(id: string): Promise<void> {
  return apiClient.patch<void>(`/api/categorias/${id}/arquivar`)
}

export function reativarCategoria(id: string): Promise<void> {
  return apiClient.post<void>(`/api/categorias/${id}/reativar`)
}
