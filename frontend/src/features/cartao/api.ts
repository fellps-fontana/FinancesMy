import { AxiosError } from 'axios';
import { httpClient } from '../../shared/api/httpClient';

/**
 * Tipos espelhando os DTOs/Models do backend (ver Models/Conta.cs,
 * Dtos/CriarContaRequest.cs, Dtos/SaldoCartaoResponseDto.cs). O ASP.NET
 * serializa em camelCase por padrao (AddControllers sem override) — os
 * campos aqui seguem essa convencao.
 */
export type TipoConta = 'BANCO' | 'CARTAO' | 'INVESTIMENTO';
export type OrigemConta = 'OPEN_FINANCE' | 'MANUAL';

export interface CriarContaCartaoRequest {
  nome: string;
  tipo: 'CARTAO';
  diaFechamento: number;
  diaVencimento: number;
}

export interface ContaResponse {
  id: string;
  nome: string;
  origem: OrigemConta;
  tipo: TipoConta | null;
  pierreAccountId: string | null;
  saldoManual: number | null;
  diaFechamento: number | null;
  diaVencimento: number | null;
  ativa: boolean;
}

export interface SaldoCartaoResponse {
  contaId: string;
  saldo: number;
}

/** Espelha o `Status` de Fatura no backend (Models/Fatura.cs). */
export type StatusFatura = 'ABERTA' | 'FECHADA' | 'PAGA';

/**
 * Espelha Dtos/FaturaResponseDto.cs. Ciclo da fatura (regra de negocio item
 * 12: recorte das compras por `dataFechamento` -> `dataVencimento`), com os
 * valores ja calculados pelo backend (`FaturaSaldoCalculator`) — o front nao
 * recalcula nada disso.
 */
export interface FaturaResponse {
  id: string;
  contaId: string;
  dataFechamento: string;
  dataVencimento: string;
  status: StatusFatura;
  valorTotal: number;
  valorPago: number;
  valorPendente: number;
}

/**
 * Espelha Dtos/CriarCompraRequest.cs. `categoriaId` e opcional no backend
 * (Guid?) — hoje sempre enviado como null porque nao ha endpoint de listagem
 * de categorias (regra de negocio item 7), ver LancarCompraForm.tsx.
 * `data` trafega como string ISO (yyyy-MM-dd), formato que o DateOnly do
 * backend aceita e que <input type="date"> ja produz nativamente.
 */
export interface CriarCompraRequest {
  categoriaId: string | null;
  descricao: string;
  valor: number;
  data: string;
}

/**
 * Campos da compra (Lancamento) usados pelo front apos a criacao. O backend
 * devolve a entidade completa, mas so consumimos os campos abaixo — o
 * restante do saldo/visao vem de queries dedicadas (useSaldoCartao).
 */
export interface CompraResponse {
  id: string;
  contaId: string;
  categoriaId: string | null;
  descricao: string | null;
  valor: number;
  data: string;
  faturaId: string | null;
}

interface ErroApiResponse {
  erro: string;
}

/** POST /api/contas — cria a conta CARTAO (ver ContasController.CriarConta). */
export async function criarContaCartao(request: CriarContaCartaoRequest): Promise<ContaResponse> {
  const { data } = await httpClient.post<ContaResponse>('/contas', request);
  return data;
}

/**
 * GET /api/contas/{id}/saldo — saldo calculado do cartao (regra de negocio
 * item 12: compras - pagamentos - estornos). Nunca armazenado, sempre lido
 * do backend.
 */
export async function obterSaldoCartao(contaId: string): Promise<SaldoCartaoResponse> {
  const { data } = await httpClient.get<SaldoCartaoResponse>(`/contas/${contaId}/saldo`);
  return data;
}

/**
 * POST /api/cartoes/{contaId}/compras — lanca uma compra na conta CARTAO
 * (regra de negocio item 12: regime de COMPETENCIA, muda o saldo calculado
 * do cartao mas nao entra no fluxo de caixa/lancamento geral).
 */
export async function criarCompra(
  contaId: string,
  request: CriarCompraRequest,
): Promise<CompraResponse> {
  const { data } = await httpClient.post<CompraResponse>(`/cartoes/${contaId}/compras`, request);
  return data;
}

/**
 * GET /api/cartoes/{contaId}/faturas — lista as faturas da conta CARTAO
 * (regra de negocio item 12: recorte das compras por ciclo de fechamento ->
 * vencimento). O backend ordena por `dataFechamento` desc; ver useFaturas
 * para a decisao de reordenar (ou nao) por vencimento no front.
 *
 * LIMITACAO CONHECIDA (documentada para o Kira): nao ha endpoint para listar
 * as compras individuais de uma fatura especifica — so o agregado (totais)
 * por fatura. A visao compra-a-compra existe hoje apenas na visao
 * categorica (GET /api/relatorios/categorias), que agrega por categoria e
 * mes, nao por fatura. Ate esse endpoint existir, esta tela nao pode listar
 * "as compras desta fatura".
 */
export async function listarFaturas(contaId: string): Promise<FaturaResponse[]> {
  const { data } = await httpClient.get<FaturaResponse[]>(`/cartoes/${contaId}/faturas`);
  return data;
}

/**
 * Extrai a mensagem de erro de negocio devolvida pela API (formato
 * `{ erro: string }`, ver ContasController) a partir de um erro de
 * mutation/query do React Query. Cai para mensagem generica se o formato
 * nao bater (erro de rede, timeout, etc).
 */
export function extrairMensagemErroApi(erro: unknown): string {
  if (erro instanceof AxiosError) {
    const corpo = erro.response?.data as ErroApiResponse | undefined;
    if (corpo?.erro) {
      return corpo.erro;
    }
  }
  return 'Nao foi possivel completar a operacao. Tente novamente.';
}
