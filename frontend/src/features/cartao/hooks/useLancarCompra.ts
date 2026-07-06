import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult } from '@tanstack/react-query';
import { criarCompra } from '../api';
import type { CompraResponse, CriarCompraRequest } from '../api';
import { CHAVE_SALDO_CARTAO } from './useContaCartao';
import { CHAVE_FATURAS } from './useFaturas';

interface LancarCompraVariaveis {
  contaId: string;
  request: CriarCompraRequest;
}

/**
 * Mutation de lancar compra no cartao (regra de negocio item 12: a compra e
 * competencia e muda o saldo calculado do cartao). Estado de servidor via
 * React Query — nenhum fetch vive no componente.
 *
 * Ao ter sucesso, invalida a query de saldo (useSaldoCartao) e a de faturas
 * (useFaturas) da mesma conta — toda compra e vinculada a uma fatura (item 12),
 * entao o total dela tambem muda e precisa refletir sem exigir reload.
 */
export function useLancarCompra(): UseMutationResult<
  CompraResponse,
  unknown,
  LancarCompraVariaveis
> {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ contaId, request }: LancarCompraVariaveis) => criarCompra(contaId, request),
    onSuccess: (_compra, variaveis) => {
      void queryClient.invalidateQueries({ queryKey: [CHAVE_SALDO_CARTAO, variaveis.contaId] });
      void queryClient.invalidateQueries({ queryKey: [CHAVE_FATURAS, variaveis.contaId] });
    },
  });
}
