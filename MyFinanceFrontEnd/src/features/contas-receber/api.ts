import { apiClient } from "@/shared/api/client"
import type {
  ContaReceberResponse,
  CriarEmprestimoRequest,
  CriarRecebivelRequest,
  RecebimentoResponse,
  RegistrarRecebimentoRequest,
  TotalAReceberEsperadoResponse,
} from "@/features/contas-receber/types"

export function listarContasReceber(status?: string): Promise<ContaReceberResponse[]> {
  const query = status ? `?status=${status}` : ""
  return apiClient.get<ContaReceberResponse[]>(`/api/contas-receber${query}`)
}

export function obterContaReceberPorId(id: string): Promise<ContaReceberResponse> {
  return apiClient.get<ContaReceberResponse>(`/api/contas-receber/${id}`)
}

export function criarRecebivel(request: CriarRecebivelRequest): Promise<ContaReceberResponse> {
  return apiClient.post<ContaReceberResponse>("/api/contas-receber/recebiveis", request)
}

export function criarEmprestimo(request: CriarEmprestimoRequest): Promise<ContaReceberResponse> {
  return apiClient.post<ContaReceberResponse>("/api/contas-receber/emprestimos", request)
}

export function registrarRecebimento(
  contaReceberId: string,
  request: RegistrarRecebimentoRequest,
): Promise<RecebimentoResponse> {
  return apiClient.post<RecebimentoResponse>(
    `/api/contas-receber/${contaReceberId}/recebimentos`,
    request,
  )
}

export function buscarTotalAReceberEsperadoNoMes(
  ano: number,
  mes: number,
): Promise<TotalAReceberEsperadoResponse> {
  return apiClient.get<TotalAReceberEsperadoResponse>(
    `/api/contas-receber/total-esperado-mes?ano=${ano}&mes=${mes}`,
  )
}
