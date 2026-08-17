import { apiClient } from "@/shared/api/client"
import type {
  CriarLancamentoRequest,
  CriarTransferenciaRequest,
  EditarLancamentoRequest,
  FluxoCaixaItem,
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

// NAO REMOVIDO nesta tarefa apesar de LancamentosPage.tsx ter migrado para
// listarFluxoCaixaTodasContas (endpoint agregado abaixo): ainda e consumido
// por features/dashboard/hooks/useUltimosLancamentos.ts (fora do escopo desta
// tarefa - ver ARQUIVOS PERMITIDOS/NAO FAZER no briefing), que compoe
// "ultimos lancamentos" iterando listarContasBanco + esta funcao por conta.
// Remover quebraria o build do dashboard. Candidato a remocao quando o
// dashboard migrar para o endpoint agregado.
export function listarFluxoCaixa(contaId: string): Promise<LancamentoResponse[]> {
  return apiClient.get<LancamentoResponse[]>(`/api/contas/${contaId}/lancamentos/fluxo-caixa`)
}

// GET /api/lancamentos/fluxo-caixa (endpoint agregado, LancamentosController)
// - fluxo de caixa (CAIXA, regra-de-negocio.md item 12) de TODAS as contas do
// usuario numa unica chamada, cada linha ja classificada como LANCAMENTO ou
// TRANSFERENCIA (ver FluxoCaixaItem, types.ts). Substitui a necessidade de
// LancamentosPage.tsx escolher uma conta antes de exibir qualquer lancamento.
export function listarFluxoCaixaTodasContas(): Promise<FluxoCaixaItem[]> {
  return apiClient.get<FluxoCaixaItem[]>("/api/lancamentos/fluxo-caixa")
}

// Uma funcao por endpoint real de TransferenciasController.cs
// (rota base: /api/transferencias). Transferencia entre contas de mesma
// titularidade (regra-de-negocio.md item 3) - nao gasto nem receita.
export function criarTransferencia(
  request: CriarTransferenciaRequest,
): Promise<TransferenciaResponse> {
  return apiClient.post<TransferenciaResponse>("/api/transferencias", request)
}
