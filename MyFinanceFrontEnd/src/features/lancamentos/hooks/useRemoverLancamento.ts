import { useMutation, useQueryClient } from "@tanstack/react-query"
import { dashboardKeys } from "@/features/dashboard/query-keys"
import { planejamentoMensalKeys } from "@/features/planejamento-mensal/query-keys"
import { removerLancamento } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"

type RemoverLancamentoVariables = {
  contaId: string
  lancamentoId: string
}

// Remover um lancamento muda o fluxo de caixa - invalida por prefixo
// (lancamentosKeys.all), mesmo raciocinio de useCriarLancamento.ts.
export function useRemoverLancamento() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, lancamentoId }: RemoverLancamentoVariables) =>
      removerLancamento(contaId, lancamentoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: lancamentosKeys.all })
      queryClient.invalidateQueries({ queryKey: dashboardKeys.all })
      queryClient.invalidateQueries({ queryKey: planejamentoMensalKeys.all })
    },
  })
}
