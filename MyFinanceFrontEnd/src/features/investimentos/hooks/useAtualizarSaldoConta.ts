import { useMutation, useQueryClient } from "@tanstack/react-query"
import { atualizarSaldoConta } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"
import type { AtualizarSaldoRequest } from "@/features/investimentos/types"

type AtualizarSaldoVariables = {
  id: string
  request: AtualizarSaldoRequest
}

export function useAtualizarSaldoConta() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: AtualizarSaldoVariables) => atualizarSaldoConta(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.contas() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.total() })
    },
  })
}
