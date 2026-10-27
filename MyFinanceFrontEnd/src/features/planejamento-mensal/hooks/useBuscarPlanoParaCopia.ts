import { useMutation, useQueryClient } from "@tanstack/react-query"
import { buscarPlanoMensal } from "@/features/planejamento-mensal/api"
import { planejamentoMensalKeys } from "@/features/planejamento-mensal/query-keys"
import type { MesPlano } from "@/features/planejamento-mensal/lib/mes"
import type { PlanoMensalResponse } from "@/features/planejamento-mensal/types"
import { ApiError } from "@/shared/api/client"

const STATUS_NAO_ENCONTRADO = 404

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

// Busca sob demanda o plano de um mes escolhido para copiar seus percentuais
// para o rascunho aberto na tela. Reaproveita a mesma queryKey de usePlanoMensal.
export function useBuscarPlanoParaCopia() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ ano, mes }: MesPlano) =>
      queryClient.fetchQuery({
        queryKey: planejamentoMensalKeys.plano(ano, mes),
        queryFn: () => buscarPlanoOuNulo(ano, mes),
      }),
  })
}
