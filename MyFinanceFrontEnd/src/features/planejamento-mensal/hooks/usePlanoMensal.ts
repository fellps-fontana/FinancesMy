import { useQuery } from "@tanstack/react-query"
import { buscarPlanoMensal } from "@/features/planejamento-mensal/api"
import { planejamentoMensalKeys } from "@/features/planejamento-mensal/query-keys"
import type { PlanoMensalResponse } from "@/features/planejamento-mensal/types"
import { ApiError } from "@/shared/api/client"

const STATUS_NAO_ENCONTRADO = 404

// Mes sem plano salvo nao e erro: o hook resolve com `null` (data === null),
// distinguivel de carregando (data === undefined) e de falha (isError).
async function buscarPlanoOuNulo(ano: number, mes: number): Promise<PlanoMensalResponse | null> {
  try {
    return await buscarPlanoMensal(ano, mes)
  } catch (error) {
    if (error instanceof ApiError && error.status === STATUS_NAO_ENCONTRADO) {
      return null
    }
    throw error
  }
}

export function usePlanoMensal(ano: number, mes: number) {
  return useQuery({
    queryKey: planejamentoMensalKeys.plano(ano, mes),
    queryFn: () => buscarPlanoOuNulo(ano, mes),
  })
}
