import { useQuery } from "@tanstack/react-query"
import { listarLimitesGasto } from "@/features/limite-gasto/api"
import { limiteGastoKeys } from "@/features/limite-gasto/query-keys"

export function useLimitesGasto() {
  return useQuery({
    queryKey: limiteGastoKeys.lista(),
    queryFn: listarLimitesGasto,
  })
}
