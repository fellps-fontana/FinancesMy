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
