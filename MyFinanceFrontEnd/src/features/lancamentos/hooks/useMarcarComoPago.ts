import { useMutation, useQueryClient } from "@tanstack/react-query"
import { marcarLancamentoComoPago } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"

type MarcarComoPagoVariables = {
  contaId: string
  lancamentoId: string
}

// Conciliacao manual (regra-de-negocio.md item 5, "conta de pagamento
// manual: ao marcar como paga, sai automatico") - PENDENTE -> PAGO muda o
// fluxo de caixa daquela conta.
export function useMarcarComoPago() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, lancamentoId }: MarcarComoPagoVariables) =>
      marcarLancamentoComoPago(contaId, lancamentoId),
    onSuccess: (_data, { contaId }) => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.fluxoCaixa(contaId) })
    },
  })
}
