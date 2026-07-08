import { useQuery } from "@tanstack/react-query"
import { listarContasInvestimento } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

export function useContasInvestimento() {
  return useQuery({
    queryKey: investimentosKeys.contas(),
    queryFn: listarContasInvestimento,
  })
}
