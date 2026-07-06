import { useMutation, useQuery } from '@tanstack/react-query';
import type { UseMutationResult, UseQueryResult } from '@tanstack/react-query';
import { criarContaCartao, obterSaldoCartao } from '../api';
import type { ContaResponse, CriarContaCartaoRequest, SaldoCartaoResponse } from '../api';

export const CHAVE_SALDO_CARTAO = 'saldo-cartao';

/**
 * Camada de estado de servidor da conta cartao (React Query). Nenhum fetch
 * vive no componente — a pagina so consome estes hooks.
 */

/**
 * Saldo calculado da conta cartao (regra de negocio item 12). So dispara
 * quando ha uma conta selecionada (`contaId` != null).
 */
export function useSaldoCartao(contaId: string | null): UseQueryResult<SaldoCartaoResponse> {
  return useQuery({
    queryKey: [CHAVE_SALDO_CARTAO, contaId],
    queryFn: () => obterSaldoCartao(contaId as string),
    enabled: contaId !== null,
  });
}

export function useCriarContaCartao(): UseMutationResult<
  ContaResponse,
  unknown,
  CriarContaCartaoRequest
> {
  return useMutation({
    mutationFn: (request: CriarContaCartaoRequest) => criarContaCartao(request),
  });
}
