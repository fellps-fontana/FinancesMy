import { apiClient } from "@/shared/api/client"
import type {
  CompraParceladaResponse,
  CompraResponse,
  ContaResponse,
  CriarCompraParceladaRequest,
  CriarCompraRequest,
  CriarContaCartaoRequest,
  FaturaResponse,
  PagamentoFaturaResponse,
  PagarFaturaRequest,
  SaldoCartaoResponse,
} from "@/features/cartao/types"

// POST /api/contas - cria a conta CARTAO (ver ContasController.CriarConta).
export function criarContaCartao(request: CriarContaCartaoRequest): Promise<ContaResponse> {
  return apiClient.post<ContaResponse>("/api/contas", request)
}

// GET /api/contas?tipo=cartao (ContasController.ListarContas ->
// IContaService.ListarContasPorTipo). Lista as contas CARTAO cadastradas -
// endpoint confirmado no backend atual (antes so suportava tipo=investimento).
export function listarContasCartao(): Promise<ContaResponse[]> {
  return apiClient.get<ContaResponse[]>("/api/contas?tipo=cartao")
}

// GET /api/contas?tipo=banco - lista as contas BANCO cadastradas, usadas
// como origem do pagamento de fatura (regra de negocio item 3: transferencia
// de mesma titularidade conta corrente -> cartao).
export function listarContasBanco(): Promise<ContaResponse[]> {
  return apiClient.get<ContaResponse[]>("/api/contas?tipo=banco")
}

// GET /api/contas/{id}/saldo - saldo calculado do cartao (regra de negocio
// item 12: compras - pagamentos - estornos). Nunca armazenado, sempre lido
// do backend.
export function obterSaldoCartao(contaId: string): Promise<SaldoCartaoResponse> {
  return apiClient.get<SaldoCartaoResponse>(`/api/contas/${contaId}/saldo`)
}

// POST /api/contas/{contaId}/compras (CartaoComprasController.CriarCompra) -
// lanca uma compra na conta CARTAO. Regime de COMPETENCIA (regra de negocio
// item 12): muda o saldo calculado do cartao, mas nao entra no fluxo de
// caixa/lancamento geral.
export function criarCompra(contaId: string, request: CriarCompraRequest): Promise<CompraResponse> {
  return apiClient.post<CompraResponse>(`/api/contas/${contaId}/compras`, request)
}

// POST /api/contas/{contaId}/compras-parceladas
// (CartaoComprasParceladasController.CriarCompraParcelada) - lanca uma
// compra parcelada na conta CARTAO (regra de negocio item 12, subsecao
// "Parcelamento"): gera N Lancamentos, um por parcela, cada um em regime de
// COMPETENCIA como uma compra a vista comum.
export function criarCompraParcelada(
  contaId: string,
  request: CriarCompraParceladaRequest,
): Promise<CompraParceladaResponse> {
  return apiClient.post<CompraParceladaResponse>(`/api/contas/${contaId}/compras-parceladas`, request)
}

// GET /api/contas/{contaId}/faturas (FaturasController.ListarFaturas) - lista
// as faturas da conta CARTAO. O backend nao ordena (FaturaRepository.ListarPorConta
// so filtra por contaId) - ver hooks/useFaturas.ts para a ordenacao aplicada no front.
export function listarFaturas(contaId: string): Promise<FaturaResponse[]> {
  return apiClient.get<FaturaResponse[]>(`/api/contas/${contaId}/faturas`)
}

// POST /api/contas/{contaId}/faturas/{faturaId}/pagamentos
// (FaturasController.PagarFatura). Registra o pagamento (parcial ou total,
// regra de negocio item 12) e devolve a Transferencia criada - NAO a fatura
// atualizada (ver PagamentoFaturaResponse em types.ts).
export function pagarFatura(
  contaId: string,
  faturaId: string,
  request: PagarFaturaRequest,
): Promise<PagamentoFaturaResponse> {
  return apiClient.post<PagamentoFaturaResponse>(
    `/api/contas/${contaId}/faturas/${faturaId}/pagamentos`,
    request,
  )
}
