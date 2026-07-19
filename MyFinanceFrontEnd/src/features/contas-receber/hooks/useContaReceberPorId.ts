import { useQuery } from "@tanstack/react-query"
import { obterContaReceberPorId } from "@/features/contas-receber/api"
import { contasReceberKeys } from "@/features/contas-receber/query-keys"

export function useContaReceberPorId(id: string) {
  return useQuery({
    queryKey: contasReceberKeys.porId(id),
    queryFn: () => obterContaReceberPorId(id),
    enabled: Boolean(id),
  })
}
