import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarContaCartao } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"
import type { CriarContaCartaoRequest } from "@/features/cartao/types"

export function useCriarContaCartao() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarContaCartaoRequest) => criarContaCartao(request),
    onSuccess: () => {
      // Invalida a lista de contas CARTAO (useContaCartaoAtual) para que a
      // conta recem-criada apareca sem exigir reload.
      queryClient.invalidateQueries({ queryKey: cartaoKeys.contasCartao() })
    },
  })
}
