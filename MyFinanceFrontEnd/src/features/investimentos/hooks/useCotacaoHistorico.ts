import { useQuery } from "@tanstack/react-query"
import { buscarCotacaoHistorico } from "@/features/investimentos/api"
import { investimentosKeys } from "@/features/investimentos/query-keys"

// Consulta sob demanda (regra-de-negocio.md item 8 e "Escopo: v1 vs v2"): so
// busca quando o usuario abre a tela/grafico do ativo com um ticker definido
// (enabled) e quando troca o range (nova queryKey) - nunca em polling.
// O QueryClient global (src/app/App.tsx) define refetchInterval de 5min como
// default para refletir o sync do Pierre (fora do escopo desta feature) - sem
// o override abaixo, esta query herdaria esse intervalo e ficaria batendo na
// API externa (Brapi) sozinha enquanto o grafico permanece aberto, o que e
// polling de verdade e viola o item 8 (cotacao automatica e v2). Por isso
// refetchInterval e refetchIntervalInBackground sao desativados explicitamente
// aqui, de forma local, sem tocar no default global de outras queries.
export function useCotacaoHistorico(ticker: string | undefined, range = "1mo") {
  return useQuery({
    queryKey: investimentosKeys.cotacaoHistorico(ticker ?? "", range),
    queryFn: () => buscarCotacaoHistorico(ticker as string, range),
    enabled: Boolean(ticker),
    refetchInterval: false,
    refetchIntervalInBackground: false,
  })
}
