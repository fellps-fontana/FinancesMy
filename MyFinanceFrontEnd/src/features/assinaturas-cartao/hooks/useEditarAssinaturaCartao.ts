import { useMutation, useQueryClient } from "@tanstack/react-query"
import { editarAssinaturaCartao } from "@/features/assinaturas-cartao/api"
import { assinaturasCartaoKeys } from "@/features/assinaturas-cartao/query-keys"
import type { EditarAssinaturaCartaoRequest } from "@/features/assinaturas-cartao/types"

type EditarAssinaturaCartaoVariables = {
  id: string
  request: EditarAssinaturaCartaoRequest
}

// Editar valor/dia/categoria propaga para a compra ja lancada na fatura
// ABERTA (AssinaturaCartaoService.EditarAsync, backend) - invalida a lista
// e o registro pontual.
export function useEditarAssinaturaCartao() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: EditarAssinaturaCartaoVariables) =>
      editarAssinaturaCartao(id, request),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: assinaturasCartaoKeys.all })
      queryClient.invalidateQueries({ queryKey: assinaturasCartaoKeys.porId(id) })
    },
  })
}
