import { useMutation, useQueryClient } from "@tanstack/react-query"
import { registrarAporte } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"
import type { RegistrarAporteRequest } from "@/features/investimentos/types"

type RegistrarAporteVariables = {
  ativoId: string
  request: RegistrarAporteRequest
}

// Registrar aporte recalcula quantidade/precoMedio do ativo (media ponderada,
// regra-de-negocio.md item 8.1), por isso invalida ativos() e resumoAtivos()
// alem do historico de aportes do proprio ativo - mesma invalidacao cruzada
// de useCriarAtivo.ts.
export function useRegistrarAporte() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ ativoId, request }: RegistrarAporteVariables) =>
      registrarAporte(ativoId, request),
    onSuccess: (_data, { ativoId }) => {
      queryClient.invalidateQueries({ queryKey: investimentosKeys.ativos() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.resumoAtivos() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.aportes(ativoId) })
    },
  })
}
