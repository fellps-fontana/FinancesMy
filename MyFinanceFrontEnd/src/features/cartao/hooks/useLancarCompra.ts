import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarCompra } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"
import type { CriarCompraRequest } from "@/features/cartao/types"

type LancarCompraVariaveis = {
  contaId: string
  request: CriarCompraRequest
}

// Toda compra fica vinculada a uma fatura (regra de negocio item 12) e muda
// o saldo calculado do cartao - invalida as duas queries para refletir sem
// exigir reload.
export function useLancarCompra() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaId, request }: LancarCompraVariaveis) => criarCompra(contaId, request),
    onSuccess: (_compra, variaveis) => {
      queryClient.invalidateQueries({ queryKey: cartaoKeys.saldo(variaveis.contaId) })
      queryClient.invalidateQueries({ queryKey: cartaoKeys.faturas(variaveis.contaId) })
    },
  })
}
