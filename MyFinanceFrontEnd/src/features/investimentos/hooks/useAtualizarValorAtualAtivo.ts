import { useMutation, useQueryClient } from "@tanstack/react-query"
import { atualizarValorAtualAtivo } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"
import type { AtualizarValorAtualRequest } from "@/features/investimentos/types"

type AtualizarValorAtualVariables = {
  id: string
  request: AtualizarValorAtualRequest
}

export function useAtualizarValorAtualAtivo() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: AtualizarValorAtualVariables) =>
      atualizarValorAtualAtivo(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.ativos() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.resumoAtivos() })
    },
  })
}
