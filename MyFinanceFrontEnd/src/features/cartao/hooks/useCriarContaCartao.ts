import { useMutation } from "@tanstack/react-query"
import { criarContaCartao } from "@/features/cartao/api"
import type { CriarContaCartaoRequest } from "@/features/cartao/types"

export function useCriarContaCartao() {
  return useMutation({
    mutationFn: (request: CriarContaCartaoRequest) => criarContaCartao(request),
  })
}
