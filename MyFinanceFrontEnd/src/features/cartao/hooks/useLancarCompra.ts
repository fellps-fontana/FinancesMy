import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarCompra, criarCompraParcelada } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"
import type { CompraParceladaResponse, CompraResponse, CriarCompraRequest } from "@/features/cartao/types"

type LancarCompraVariaveis = {
  contaId: string
  request: CriarCompraRequest
  // Presente e >= 2 quando a compra e parcelada (regra de negocio item 12,
  // subsecao "Parcelamento"). Ausente ou 1 = compra a vista, mesmo
  // comportamento de sempre.
  numeroParcelas?: number
}

// Resultado tipado como uniao discriminada: o container (ContaCartaoPage)
// usa `tipo` para decidir se mostra a confirmacao de agrupamento
// ("Notebook 1/10") ou o fluxo de compra a vista de sempre.
export type LancarCompraResultado =
  | { tipo: "avista"; compra: CompraResponse }
  | { tipo: "parcelada"; compraParcelada: CompraParceladaResponse }

// Toda compra fica vinculada a uma fatura (regra de negocio item 12) e muda
// o saldo calculado do cartao - invalida as duas queries para refletir sem
// exigir reload. Compra parcelada (regra de negocio item 12, subsecao
// "Parcelamento") usa outro endpoint - o valor de cada parcela e calculado
// no backend (valorTotal / quantidadeParcelas), nunca aqui.
export function useLancarCompra() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      contaId,
      request,
      numeroParcelas,
    }: LancarCompraVariaveis): Promise<LancarCompraResultado> => {
      if (numeroParcelas !== undefined && numeroParcelas >= 2) {
        const compraParcelada = await criarCompraParcelada(contaId, {
          descricao: request.descricao,
          valorTotal: request.valor,
          quantidadeParcelas: numeroParcelas,
          categoriaId: request.categoriaId,
          dataCompra: request.data,
        })
        return { tipo: "parcelada", compraParcelada }
      }

      const compra = await criarCompra(contaId, request)
      return { tipo: "avista", compra }
    },
    onSuccess: (_resultado, variaveis) => {
      queryClient.invalidateQueries({ queryKey: cartaoKeys.saldo(variaveis.contaId) })
      queryClient.invalidateQueries({ queryKey: cartaoKeys.faturas(variaveis.contaId) })
    },
  })
}
