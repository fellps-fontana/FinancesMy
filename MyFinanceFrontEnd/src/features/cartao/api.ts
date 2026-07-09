import { apiClient } from "@/shared/api/client"
import type {
  CompraResponse,
  ContaResponse,
  CriarCompraRequest,
  CriarContaCartaoRequest,
  FaturaResponse,
  PagamentoFaturaResponse,
  PagarFaturaRequest,
  RelatorioCategoriaResponse,
  SaldoCartaoResponse,
} from "@/features/cartao/types"

// POST /api/contas - cria a conta CARTAO (ver ContasController.CriarConta).
export function criarContaCartao(request: CriarContaCartaoRequest): Promise<ContaResponse> {
  return apiClient.post<ContaResponse>("/api/contas", request)
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

// GAP CONHECIDO: nao ha, hoje, nenhum controller/service de relatorio no
// backend (MyFinances/Controllers so tem AuthController, CartaoComprasController,
// ContasController e FaturasController - confirmado por busca no projeto).
// Esta funcao chama o contrato que a tela de relatorio por categoria precisa
// (regra de negocio item 12, visao categorica/competencia); ate o backend
// implementar o endpoint, toda chamada aqui resulta em erro e a tela mostra
// o estado correspondente (ver RelatorioCategoriaPage.tsx) - nenhum dado
// mockado.
export function obterRelatorioCategoria(mes: string, contaId?: string): Promise<RelatorioCategoriaResponse> {
  const query = contaId ? `?mes=${mes}&contaId=${contaId}` : `?mes=${mes}`
  return apiClient.get<RelatorioCategoriaResponse>(`/api/relatorios/categorias${query}`)
}
