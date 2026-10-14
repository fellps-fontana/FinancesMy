import { apiClient } from "@/shared/api/client"
import type {
  AtivoAporteResponse,
  AtivoResponse,
  AtivosResumoResponse,
  AtualizarValorAtualRequest,
  CriarAtivoRequest,
  RegistrarAporteRequest,
  RegistrarDividendoRequest,
  RendimentoResponse,
  RendimentosResumoResponse,
  TotalInvestidoResponse,
} from "@/features/investimentos/types"

// --- Ativo (regra-de-negocio.md item 8) - sempre vinculado a uma conta de investimento (item 8.5).
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
export function registrarDividendo(
  ativoId: string,
  request: RegistrarDividendoRequest,
): Promise<RendimentoResponse> {
  return apiClient.post<RendimentoResponse>(`/api/ativos/${ativoId}/rendimentos`, request)
}

export function buscarRendimentosResumo(): Promise<RendimentosResumoResponse> {
  return apiClient.get<RendimentosResumoResponse>("/api/ativos/rendimentos-resumo")
}

export function registrarAporte(
  ativoId: string,
  request: RegistrarAporteRequest,
): Promise<AtivoAporteResponse> {
  return apiClient.post<AtivoAporteResponse>(`/api/ativos/${ativoId}/aportes`, request)
}

export function listarAportes(ativoId: string): Promise<AtivoAporteResponse[]> {
  return apiClient.get<AtivoAporteResponse[]>(`/api/ativos/${ativoId}/aportes`)
}

// PATCH /api/ativos/{id}/conta - define ou troca a conta de investimento do
// ativo. 400: conta nao e de investimento ou id vazio; 404: ativo/conta
// inexistente. O ativo nunca volta a ficar sem conta (contaId obrigatorio).
export function vincularContaAtivo(ativoId: string, contaId: string): Promise<AtivoResponse> {
  return apiClient.patch<AtivoResponse>(`/api/ativos/${ativoId}/conta`, { contaId })
}

// GET /api/contas/investimentos/total - total investido consolidado
// (contas de investimento + ativos), calculado no backend.
export function buscarTotalInvestido(): Promise<TotalInvestidoResponse> {
  return apiClient.get<TotalInvestidoResponse>("/api/contas/investimentos/total")
}
