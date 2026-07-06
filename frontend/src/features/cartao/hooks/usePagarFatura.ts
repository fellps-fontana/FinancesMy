import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult } from '@tanstack/react-query';
import { pagarFatura } from '../api';
import type { FaturaResponse, PagarFaturaRequest } from '../api';
import { CHAVE_FATURAS } from './useFaturas';
import { CHAVE_SALDO_CARTAO } from './useContaCartao';

interface PagarFaturaVariaveis {
  faturaId: string;
  contaId: string;
  request: PagarFaturaRequest;
}

/**
 * Mutation de pagamento de fatura (regra de negocio item 12, "Pagamento x
 * fatura (revisado)"): pode ser antecipado (fatura ABERTA) e parcial (varios
 * pagamentos ate quitar) — por isso o valor enviado nao e recalculado aqui,
 * so repassado do formulario. Estado de servidor via React Query.
 *
 * Ao ter sucesso, invalida a lista de faturas (CHAVE_FATURAS) e o saldo do
 * cartao (CHAVE_SALDO_CARTAO) da mesma conta, para refletirem o novo
 * `valorPago`/`valorPendente` sem exigir reload.
 */
export function usePagarFatura(): UseMutationResult<
  FaturaResponse,
  unknown,
  PagarFaturaVariaveis
> {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ faturaId, request }: PagarFaturaVariaveis) => pagarFatura(faturaId, request),
    onSuccess: (_fatura, variaveis) => {
      void queryClient.invalidateQueries({ queryKey: [CHAVE_FATURAS, variaveis.contaId] });
      void queryClient.invalidateQueries({ queryKey: [CHAVE_SALDO_CARTAO, variaveis.contaId] });
    },
  });
}
