import { useQuery } from "@tanstack/react-query"
import { buscarGastoVsLimitePorCategoria } from "@/features/limite-gasto/api"
import { limiteGastoKeys } from "@/features/limite-gasto/query-keys"

export function useGastoVsLimite(categoriaId: string, ano: number, mes: number) {
  return useQuery({
    queryKey: limiteGastoKeys.gastoVsLimitePorCategoria(categoriaId, ano, mes),
    queryFn: () => buscarGastoVsLimitePorCategoria(categoriaId, ano, mes),
    enabled: Boolean(categoriaId),
  })
}
