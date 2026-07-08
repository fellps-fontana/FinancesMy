import { useQuery } from '@tanstack/react-query';
import type { UseQueryResult } from '@tanstack/react-query';
import { listarFaturas } from '../api';
import type { FaturaResponse } from '../api';

export const CHAVE_FATURAS = 'faturas-cartao';

/**
 * Faturas da conta CARTAO (regra de negocio item 12), ordenadas por
 * `dataVencimento` desc (fatura mais recente primeiro, como nas abas de mes
 * do mockup "05 Cartao de credito").
 *
 * DECISAO DE UX: o backend ordena por `dataFechamento` desc. Para uma conta
 * com ciclo de fechamento/vencimento fixo essa ordem coincide na pratica com
 * a ordem por `dataVencimento` (o vencimento e sempre alguns dias apos o
 * fechamento, mantendo a mesma posicao relativa entre faturas). Ainda assim,
 * a task pede explicitamente ordenacao por vencimento — reordenamos aqui no
 * front em vez de assumir a garantia do backend, que ordena por outro campo.
 * Comparação em string ISO (yyyy-MM-dd) e segura porque a ordem lexicografica
 * coincide com a ordem cronologica nesse formato.
 */
export function useFaturas(contaId: string | null): UseQueryResult<FaturaResponse[]> {
  return useQuery({
    queryKey: [CHAVE_FATURAS, contaId],
    queryFn: async () => {
      const faturas = await listarFaturas(contaId as string);
      return [...faturas].sort((a, b) => b.dataVencimento.localeCompare(a.dataVencimento));
    },
    enabled: contaId !== null,
  });
}
