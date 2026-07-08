import { apiClient } from "@/shared/api/client"
import type {
  AtualizarSaldoRequest,
  ContaResponse,
  CriarContaInvestimentoRequest,
  TotalInvestidoResponse,
} from "@/features/investimentos/types"

export function listarContasInvestimento(): Promise<ContaResponse[]> {
  return apiClient.get<ContaResponse[]>("/api/contas?tipo=investimento")
}

export function criarContaInvestimento(
  request: CriarContaInvestimentoRequest,
): Promise<ContaResponse> {
  return apiClient.post<ContaResponse>("/api/contas", request)
}

export function atualizarSaldoConta(id: string, request: AtualizarSaldoRequest): Promise<void> {
  return apiClient.patch<void>(`/api/contas/${id}/saldo`, request)
}

export function desativarConta(id: string): Promise<void> {
  return apiClient.patch<void>(`/api/contas/${id}/desativar`)
}

export function buscarTotalInvestido(): Promise<TotalInvestidoResponse> {
  return apiClient.get<TotalInvestidoResponse>("/api/contas/investimentos/total")
}
