import { apiClient } from "@/shared/api/client"
import type {
  AtivoResponse,
  AtivosResumoResponse,
  AtualizarSaldoRequest,
  AtualizarValorAtualRequest,
  ContaResponse,
  CriarAtivoRequest,
  CriarContaInvestimentoRequest,
  TotalInvestidoResponse,
} from "@/features/investimentos/types"

// --- Ativo (regra-de-negocio.md item 8) - standalone, sem vinculo com Conta.
export function listarAtivos(): Promise<AtivoResponse[]> {
  return apiClient.get<AtivoResponse[]>("/api/ativos")
}

export function criarAtivo(request: CriarAtivoRequest): Promise<AtivoResponse> {
  return apiClient.post<AtivoResponse>("/api/ativos", request)
}

export function atualizarValorAtualAtivo(
  id: string,
  request: AtualizarValorAtualRequest,
): Promise<void> {
  return apiClient.patch<void>(`/api/ativos/${id}/valor-atual`, request)
}

export function desativarAtivo(id: string): Promise<void> {
  return apiClient.patch<void>(`/api/ativos/${id}/desativar`)
}

export function buscarResumoAtivos(): Promise<AtivosResumoResponse> {
  return apiClient.get<AtivosResumoResponse>("/api/ativos/resumo")
}

// --- Conta de investimento simples (cofrinho/XP) - item 8/10, modulo
// separado de Ativo. Endpoints inalterados em relacao ao modulo anterior.
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
