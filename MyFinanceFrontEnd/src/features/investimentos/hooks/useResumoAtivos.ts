import { useQuery } from "@tanstack/react-query"
import { buscarResumoAtivos } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

export function useResumoAtivos() {
  return useQuery({
    queryKey: investimentosKeys.resumoAtivos(),
    queryFn: buscarResumoAtivos,
  })
}
