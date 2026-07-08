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

/**
 * Espelha Dtos/PagarFaturaRequest.cs. `data` segue o mesmo formato ISO
 * (yyyy-MM-dd) usado em CriarCompraRequest. `valor` pode ser MENOR que o
 * `valorPendente` da fatura (regra de negocio item 12, "Pagamento x fatura
 * (revisado)": pagamento parcial e permitido — o backend so rejeita valor
 * <= 0 ou valor que exceda o saldo pendente).
 */
export interface PagarFaturaRequest {
  contaOrigemId: string;
  data: string;
  valor: number;
}

/**
 * Espelha Dtos/RelatorioCategoriaResponseDto.cs. Visao CATEGORICA/COMPETENCIA
 * (regra de negocio item 12, "Duas visoes"): soma as compras do cartao por
 * categoria dentro do mes, ignorando o pagamento de fatura (que e transferencia
 * e vive so no fluxo de caixa). `categoriaId`/`nomeCategoria` vem `null` quando
 * a compra nao tem categoria vinculada (regra de negocio item 7) — o item
 * continua no relatorio, nao e descartado.
 */
export interface RelatorioCategoriaItem {
  categoriaId: string | null;
  nomeCategoria: string | null;
  total: number;
}

export interface RelatorioCategoriaResponse {
  itens: RelatorioCategoriaItem[];
  mes: number;
  ano: number;
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
 * POST ~/api/faturas/{id}/pagamento — rota ABSOLUTA (fora do prefixo
 * /api/cartoes/{contaId}), ver FaturasController.PagarFatura. Registra o
 * pagamento (parcial ou total, regra de negocio item 12 revisado) e devolve
 * a fatura com `valorPago`/`valorPendente` ja recalculados — o front nao
 * soma nada, so invalida as queries dependentes (ver usePagarFatura).
 */
export async function pagarFatura(
  faturaId: string,
  request: PagarFaturaRequest,
): Promise<FaturaResponse> {
  const { data } = await httpClient.post<FaturaResponse>(`/faturas/${faturaId}/pagamento`, request);
  return data;
}

/**
 * GET /api/relatorios/categorias — visao categorica/competencia (regra de
 * negocio item 12). `mes` no formato "yyyy-MM" (o backend faz o split e
 * valida internamente, ver RelatoriosController.ObterGastoPorCategoria).
 * `contaId` opcional filtra para uma unica conta CARTAO; sem ele, o backend
 * soma todas as contas com compras no periodo.
 */
export async function obterRelatorioCategoria(
  mes: string,
  contaId?: string,
): Promise<RelatorioCategoriaResponse> {
  const { data } = await httpClient.get<RelatorioCategoriaResponse>('/relatorios/categorias', {
    params: contaId ? { mes, contaId } : { mes },
  });
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
