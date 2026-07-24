import { useQuery } from "@tanstack/react-query"
import { listarContasFixas } from "@/features/contas-fixas/api"
import { contasFixasKeys } from "@/features/contas-fixas/query-keys"

export function useContasFixas(ativa?: boolean) {
  return useQuery({
    queryKey: contasFixasKeys.lista(ativa),
    queryFn: () => listarContasFixas(ativa),
  })
}
