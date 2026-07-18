import { useQuery } from "@tanstack/react-query"
import { listarContasReceber } from "@/features/contas-receber/api"
import { contasReceberKeys } from "@/features/contas-receber/query-keys"

export function useContasReceber(status?: string) {
  return useQuery({
    queryKey: contasReceberKeys.lista(status),
    queryFn: () => listarContasReceber(status),
  })
}
