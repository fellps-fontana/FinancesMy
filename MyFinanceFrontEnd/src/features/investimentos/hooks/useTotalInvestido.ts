import { useQuery } from "@tanstack/react-query"
import { buscarTotalInvestido } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

export function useTotalInvestido() {
  return useQuery({
    queryKey: investimentosKeys.total(),
    queryFn: buscarTotalInvestido,
  })
}
