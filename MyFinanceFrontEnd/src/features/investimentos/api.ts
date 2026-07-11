import { apiClient } from "@/shared/api/client"
import type {
  AtivoResponse,
  AtualizarSaldoRequest,
  ContaResponse,
  CotacaoHistoricoResponse,
  CriarContaInvestimentoRequest,
  RegistrarCompraRequest,
  RegistrarVendaRequest,
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

export function listarAtivosDaConta(contaId: string): Promise<AtivoResponse[]> {
  return apiClient.get<AtivoResponse[]>(`/api/contas/${contaId}/ativos`)
}

export function registrarCompraAtivo(
  contaId: string,
  request: RegistrarCompraRequest,
): Promise<AtivoResponse> {
  return apiClient.post<AtivoResponse>(`/api/contas/${contaId}/ativos/compras`, request)
}

export function registrarVendaAtivo(
  contaId: string,
  ativoId: string,
  request: RegistrarVendaRequest,
): Promise<AtivoResponse> {
  return apiClient.post<AtivoResponse>(
    `/api/contas/${contaId}/ativos/${ativoId}/vendas`,
    request,
  )
}

export function buscarCotacaoHistorico(
  ticker: string,
  range = "1mo",
): Promise<CotacaoHistoricoResponse> {
  return apiClient.get<CotacaoHistoricoResponse>(
    `/api/ativos/cotacao/${ticker}/historico?range=${range}`,
  )
}
