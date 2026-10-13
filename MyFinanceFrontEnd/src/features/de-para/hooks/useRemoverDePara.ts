import { useMutation, useQueryClient } from "@tanstack/react-query"
import { removerDePara } from "@/features/de-para/api"
import { deParaKeys } from "@/features/de-para/query-keys"

export function useRemoverDePara() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => removerDePara(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: deParaKeys.lista() })
    },
  })
}
