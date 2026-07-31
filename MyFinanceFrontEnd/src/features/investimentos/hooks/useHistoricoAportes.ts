import { useQuery } from "@tanstack/react-query"
import { listarAportes } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

// Historico completo de aportes por ativo (regra-de-negocio.md item 8.1) -
// base do grafico de aportes por ativo na tela "Investimentos". So busca
// quando ativoId estiver definido (ex: modal/detalhe aberto para um ativo
// especifico).
export function useHistoricoAportes(ativoId: string | undefined) {
  return useQuery({
    queryKey: investimentosKeys.aportes(ativoId ?? ""),
    queryFn: () => listarAportes(ativoId as string),
    enabled: Boolean(ativoId),
  })
}
