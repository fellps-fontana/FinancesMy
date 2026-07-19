import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarAtivo } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"
import type { CriarAtivoRequest } from "@/features/investimentos/types"

export function useCriarAtivo() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarAtivoRequest) => criarAtivo(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.ativos() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.resumoAtivos() })
    },
  })
}
