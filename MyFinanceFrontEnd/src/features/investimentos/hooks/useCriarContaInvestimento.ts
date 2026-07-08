import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarContaInvestimento } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"
import type { CriarContaInvestimentoRequest } from "@/features/investimentos/types"

export function useCriarContaInvestimento() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarContaInvestimentoRequest) => criarContaInvestimento(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.contas() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.total() })
    },
  })
}
