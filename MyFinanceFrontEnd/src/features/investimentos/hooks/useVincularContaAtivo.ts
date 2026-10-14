import { useMutation, useQueryClient } from "@tanstack/react-query"
import { vincularContaAtivo } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"
import { contasKeys } from "@/features/contas/query-keys"

type VincularContaVariables = {
  ativoId: string
  contaId: string
}

// Vincular/trocar a conta muda o saldo calculado das contas e o total
// investido: invalida ativos, resumo, contas e total.
export function useVincularContaAtivo() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ ativoId, contaId }: VincularContaVariables) => vincularContaAtivo(ativoId, contaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.ativos() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.resumoAtivos() })
      queryClient.invalidateQueries({ queryKey: contasKeys.all })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.total() })
    },
  })
}
