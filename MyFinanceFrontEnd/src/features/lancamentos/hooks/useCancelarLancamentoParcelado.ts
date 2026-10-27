import { useMutation, useQueryClient } from "@tanstack/react-query"
import { dashboardKeys } from "@/features/dashboard/query-keys"
import { cancelarLancamentoParcelado } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"
import { planejamentoMensalKeys } from "@/features/planejamento-mensal/query-keys"

type CancelarLancamentoParceladoVariables = {
  contaId: string
  compraParceladaId: string
}

// Cancelar exclui as parcelas nao pagas: mesmo impacto da criacao.
export function useCancelarLancamentoParcelado() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, compraParceladaId }: CancelarLancamentoParceladoVariables) =>
      cancelarLancamentoParcelado(contaId, compraParceladaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.all })
      queryClient.invalidateQueries({ queryKey: dashboardKeys.all })
      queryClient.invalidateQueries({ queryKey: planejamentoMensalKeys.all })
    },
  })
}
