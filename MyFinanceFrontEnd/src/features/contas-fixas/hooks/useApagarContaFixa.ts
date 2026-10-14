import { useMutation, useQueryClient } from "@tanstack/react-query"
import { apagarContaFixa } from "@/features/contas-fixas/api"
import { contasFixasKeys } from "@/features/contas-fixas/query-keys"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"
import { dashboardKeys } from "@/features/dashboard/query-keys"

// Apagar remove a ContaFixa e os Lancamentos vinculados - alem da lista de
// contas fixas, invalida fluxo de caixa (lancamentos) e projecao do dashboard.
export function useApagarContaFixa() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => apagarContaFixa(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: contasFixasKeys.lista() })
      queryClient.removeQueries({ queryKey: contasFixasKeys.porId(id) })
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.all })
      queryClient.invalidateQueries({ queryKey: dashboardKeys.all })
    },
  })
}
