import { useMutation, useQueryClient } from "@tanstack/react-query"
import { desativarAtivo } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

export function useDesativarAtivo() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => desativarAtivo(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.ativos() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.resumoAtivos() })
    },
  })
}
