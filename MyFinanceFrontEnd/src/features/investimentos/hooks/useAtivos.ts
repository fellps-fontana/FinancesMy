import { useQuery } from "@tanstack/react-query"
import { listarAtivos } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

export function useAtivos() {
  return useQuery({
    queryKey: investimentosKeys.ativos(),
    queryFn: listarAtivos,
  })
}
