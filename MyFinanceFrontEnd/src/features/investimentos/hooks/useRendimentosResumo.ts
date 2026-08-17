import { useQuery } from "@tanstack/react-query"
import { buscarRendimentosResumo } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

export function useRendimentosResumo() {
  return useQuery({
    queryKey: investimentosKeys.rendimentosResumo(),
    queryFn: buscarRendimentosResumo,
  })
}
