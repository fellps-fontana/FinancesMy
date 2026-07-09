import { useQuery } from "@tanstack/react-query"
import { buscarCotacaoHistorico } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

// Consulta sob demanda (regra-de-negocio.md item 8): so busca quando o
// usuario abre a tela do ativo com um ticker definido, nunca em polling.
export function useCotacaoHistorico(ticker: string | undefined, range = "1mo") {
  return useQuery({
    queryKey: investimentosKeys.cotacaoHistorico(ticker ?? "", range),
    queryFn: () => buscarCotacaoHistorico(ticker as string, range),
    enabled: Boolean(ticker),
  })
}
