import { apiClient } from "@/shared/api/client"
import type {
  AtivoResponse,
  AtivosResumoResponse,
  AtualizarValorAtualRequest,
  CriarAtivoRequest,
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

// Conta de investimento simples (cofrinho/XP, item 8/10) foi migrada para a
// feature `contas/` (TASK-127/130) - listagem, edicao de saldo_manual e
// desativacao agora vivem em features/contas/api.ts, junto de Conta tipo
// Banco. Nao duplicar aqui (correcao pos-review, TASK-130).
