import { useMutation, useQueryClient } from "@tanstack/react-query"
import { pagarFatura } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"
import type { PagarFaturaRequest } from "@/features/cartao/types"

type PagarFaturaVariaveis = {
  contaId: string
  faturaId: string
  request: PagarFaturaRequest
}

// A resposta da mutation e a Transferencia criada, nao a fatura atualizada
// (ver PagamentoFaturaService.PagarFaturaAsync) - o valorPago/valorPendente
// novos so chegam invalidando as queries dependentes, nunca lendo o retorno
// da mutation.
export function usePagarFatura() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, faturaId, request }: PagarFaturaVariaveis) =>
      pagarFatura(contaId, faturaId, request),
    onSuccess: (_pagamento, variaveis) => {
      queryClient.invalidateQueries({ queryKey: cartaoKeys.faturas(variaveis.contaId) })
      queryClient.invalidateQueries({ queryKey: cartaoKeys.saldo(variaveis.contaId) })
    },
  })
}
