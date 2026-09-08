import { useMutation, useQueryClient } from "@tanstack/react-query"
import { reativarAssinaturaCartao } from "@/features/assinaturas-cartao/api"
import { assinaturasCartaoKeys } from "@/features/assinaturas-cartao/query-keys"

export function useReativarAssinaturaCartao() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => reativarAssinaturaCartao(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: assinaturasCartaoKeys.all })
      queryClient.invalidateQueries({ queryKey: assinaturasCartaoKeys.porId(id) })
    },
  })
}
