import { useQuery } from "@tanstack/react-query"
import { listarAtivosDaConta } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

export function useAtivosDaConta(contaId: string) {
  return useQuery({
    queryKey: investimentosKeys.ativos(contaId),
    queryFn: () => listarAtivosDaConta(contaId),
    enabled: Boolean(contaId),
  })
}
