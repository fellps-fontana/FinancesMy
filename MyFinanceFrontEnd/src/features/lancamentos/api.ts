import { apiClient } from "@/shared/api/client"
import type {
  CriarLancamentoRequest,
  CriarTransferenciaRequest,
  EditarLancamentoRequest,
  LancamentoResponse,
  TransferenciaResponse,
} from "@/features/lancamentos/types"

// Uma funcao por endpoint real de LancamentosController.cs
// (rota base: /api/contas/{contaId}/lancamentos).

export function criarLancamento(
  contaId: string,
  request: CriarLancamentoRequest,
): Promise<LancamentoResponse> {
  return apiClient.post<LancamentoResponse>(`/api/contas/${contaId}/lancamentos`, request)
}

export function editarLancamento(
  contaId: string,
  lancamentoId: string,
  request: EditarLancamentoRequest,
): Promise<LancamentoResponse> {
  return apiClient.put<LancamentoResponse>(
    `/api/contas/${contaId}/lancamentos/${lancamentoId}`,
    request,
  )
}

// Conciliacao manual (regra-de-negocio.md item 5, "Conta de pagamento
// manual (v1): ao marcar como paga, sai automatico"). Endpoint nao devolve
// corpo (Ok() vazio no controller).
export function marcarLancamentoComoPago(contaId: string, lancamentoId: string): Promise<void> {
  return apiClient.post<void>(`/api/contas/${contaId}/lancamentos/${lancamentoId}/pagamentos`)
}

export function removerLancamento(contaId: string, lancamentoId: string): Promise<void> {
  return apiClient.delete<void>(`/api/contas/${contaId}/lancamentos/${lancamentoId}`)
}

export function listarFluxoCaixa(contaId: string): Promise<LancamentoResponse[]> {
  return apiClient.get<LancamentoResponse[]>(`/api/contas/${contaId}/lancamentos/fluxo-caixa`)
}

// Uma funcao por endpoint real de TransferenciasController.cs
// (rota base: /api/transferencias). Transferencia entre contas de mesma
// titularidade (regra-de-negocio.md item 3) - nao gasto nem receita.
export function criarTransferencia(
  request: CriarTransferenciaRequest,
): Promise<TransferenciaResponse> {
  return apiClient.post<TransferenciaResponse>("/api/transferencias", request)
}
