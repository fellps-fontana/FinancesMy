import { useQuery } from "@tanstack/react-query"
import { listarRendimentosDoAtivo } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

export function useHistoricoRendimentos(ativoId: string) {
  return useQuery({
    queryKey: investimentosKeys.rendimentos(ativoId),
    queryFn: () => listarRendimentosDoAtivo(ativoId),
  })
}
