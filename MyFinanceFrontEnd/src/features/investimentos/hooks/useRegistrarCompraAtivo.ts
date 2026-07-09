import { useMutation, useQueryClient } from "@tanstack/react-query"
import { registrarCompraAtivo } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"
import type { RegistrarCompraRequest } from "@/features/investimentos/types"

type RegistrarCompraVariables = {
  contaId: string
  request: RegistrarCompraRequest
}

// Compra muda quantidade/preco_medio/preco_atual do ativo, que por sua vez
// muda o saldo calculado da conta INVESTIMENTO (regra-de-negocio.md item 10)
// - por isso invalida tanto a lista de ativos quanto contas/total.
export function useRegistrarCompraAtivo() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, request }: RegistrarCompraVariables) =>
      registrarCompraAtivo(contaId, request),
    onSuccess: (_data, { contaId }) => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.ativos(contaId) })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.contas() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.total() })
    },
  })
}
