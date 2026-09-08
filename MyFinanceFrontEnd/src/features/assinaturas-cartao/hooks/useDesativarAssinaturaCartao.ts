import { useMutation, useQueryClient } from "@tanstack/react-query"
import { desativarAssinaturaCartao } from "@/features/assinaturas-cartao/api"
import { assinaturasCartaoKeys } from "@/features/assinaturas-cartao/query-keys"

export function useDesativarAssinaturaCartao() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => desativarAssinaturaCartao(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: assinaturasCartaoKeys.all })
      queryClient.invalidateQueries({ queryKey: assinaturasCartaoKeys.porId(id) })
    },
  })
}
