import { useMutation, useQueryClient } from "@tanstack/react-query"
import { desativarConta } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

export function useDesativarConta() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => desativarConta(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.contas() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.total() })
    },
  })
}
