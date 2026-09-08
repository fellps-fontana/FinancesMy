import { useMutation, useQueryClient } from "@tanstack/react-query"
import { excluirContaReceber } from "@/features/contas-receber/api"
import { contasReceberKeys } from "@/features/contas-receber/query-keys"

export function useExcluirContaReceber() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => excluirContaReceber(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.all })
      queryClient.invalidateQueries({ queryKey: ["recebiveisRecorrentes"] })
    },
  })
}
