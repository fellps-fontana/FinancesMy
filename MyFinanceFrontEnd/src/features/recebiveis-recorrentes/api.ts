import { apiClient } from "@/shared/api/client"
import type {
  CriarRecebivelRecorrenteRequest,
  EditarRecebivelRecorrenteRequest,
  RecebivelRecorrenteResponse,
} from "@/features/recebiveis-recorrentes/types"

// Uma funcao por endpoint (stack.md "features/<modulo>/ - api.ts"): so monta
// o request e devolve a Promise, sem decidir cache/invalidacao (isso e do
// hook). Rotas reais confirmadas em RecebivelRecorrenteController.

export function listarRecebiveisRecorrentes(
  ativa?: boolean,
): Promise<RecebivelRecorrenteResponse[]> {
  const query = ativa !== undefined ? `?ativa=${ativa}` : ""
  return apiClient.get<RecebivelRecorrenteResponse[]>(`/api/recebiveis-recorrentes${query}`)
}

export function obterRecebivelRecorrentePorId(
  id: string,
): Promise<RecebivelRecorrenteResponse> {
  return apiClient.get<RecebivelRecorrenteResponse>(`/api/recebiveis-recorrentes/${id}`)
}

export function criarRecebivelRecorrente(
  request: CriarRecebivelRecorrenteRequest,
): Promise<RecebivelRecorrenteResponse> {
  return apiClient.post<RecebivelRecorrenteResponse>("/api/recebiveis-recorrentes", request)
}

export function editarRecebivelRecorrente(
  id: string,
  request: EditarRecebivelRecorrenteRequest,
): Promise<RecebivelRecorrenteResponse> {
  return apiClient.put<RecebivelRecorrenteResponse>(`/api/recebiveis-recorrentes/${id}`, request)
}

export function desativarRecebivelRecorrente(id: string): Promise<void> {
  return apiClient.post<void>(`/api/recebiveis-recorrentes/${id}/desativar`)
}

export function reativarRecebivelRecorrente(id: string): Promise<void> {
  return apiClient.post<void>(`/api/recebiveis-recorrentes/${id}/reativar`)
}
