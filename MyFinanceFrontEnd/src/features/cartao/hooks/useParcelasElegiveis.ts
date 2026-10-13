import { useQuery } from "@tanstack/react-query"
import { listarParcelasElegiveis } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"

export function useParcelasElegiveis(contaId: string | null, compraParceladaId: string | null) {
  return useQuery({
    queryKey: cartaoKeys.parcelasElegiveis(contaId ?? "", compraParceladaId ?? ""),
    queryFn: () => listarParcelasElegiveis(contaId as string, compraParceladaId as string),
    enabled: contaId !== null && compraParceladaId !== null,
  })
}
