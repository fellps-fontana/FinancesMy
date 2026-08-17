import { useMutation, useQueryClient } from "@tanstack/react-query"
import { marcarLancamentoComoPago } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"

type MarcarComoPagoVariables = {
  contaId: string
  lancamentoId: string
}

// Conciliacao manual (regra-de-negocio.md item 5, "conta de pagamento
// manual: ao marcar como paga, sai automatico") - PENDENTE -> PAGO muda o
// fluxo de caixa. Invalida por prefixo (lancamentosKeys.all), mesmo
// raciocinio de useCriarLancamento.ts.
export function useMarcarComoPago() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, lancamentoId }: MarcarComoPagoVariables) =>
      marcarLancamentoComoPago(contaId, lancamentoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.all })
    },
  })
}
