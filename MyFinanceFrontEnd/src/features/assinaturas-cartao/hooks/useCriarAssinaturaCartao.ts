import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarAssinaturaCartao } from "@/features/assinaturas-cartao/api"
import { assinaturasCartaoKeys } from "@/features/assinaturas-cartao/query-keys"
import type { CriarAssinaturaCartaoRequest } from "@/features/assinaturas-cartao/types"

// Criar uma AssinaturaCartao ja gera a primeira compra na fatura aberta
// (AssinaturaCartaoService.CriarAsync, backend) - invalida a lista de
// assinaturas e, por seguranca, tambem deixa a tela de faturas se
// atualizar sozinha ja que o React Query so refaz o fetch quando algo
// observa a query invalidada.
export function useCriarAssinaturaCartao() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarAssinaturaCartaoRequest) => criarAssinaturaCartao(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: assinaturasCartaoKeys.all })
    },
  })
}
