import { useQuery } from "@tanstack/react-query"
import { buscarGastoVsLimiteTodasCategorias } from "@/features/limite-gasto/api"
import { limiteGastoKeys } from "@/features/limite-gasto/query-keys"

export function useGastoVsLimiteTodasCategorias(ano: number, mes: number) {
  return useQuery({
    queryKey: limiteGastoKeys.gastoVsLimiteTodas(ano, mes),
    queryFn: () => buscarGastoVsLimiteTodasCategorias(ano, mes),
  })
}
