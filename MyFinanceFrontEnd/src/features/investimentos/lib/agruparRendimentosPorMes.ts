import type { RendimentoResponse } from "@/features/investimentos/types"

// Ponto do grafico empilhado por mes: um bucket por competencia (AAAA-MM),
// com os dois tipos de Rendimento somados separadamente (regra-de-negocio.md
// item 8.4). DIVIDENDO e VALORIZACAO tem origem MUITO diferente (manual vs
// derivado automatico de valorAtual) e nunca devem virar um unico numero
// somado -- por isso dois campos, nunca um "total".
export type PontoRendimentosPorMes = {
  mes: string
  dividendo: number
  valorizacao: number
}

// Bucketiza o historico flat de Rendimento (agregado de todos os ativos, via
// useRendimentosResumo) por mes de competencia e por tipo. Funcao pura,
// testavel isolada, sem JSX/hook/fetch (stack.md, criterio lib/) -- nenhum
// agrupamento roda no componente (clean-code.md "Organizacao (React)":
// logica de calculo nao vive no componente). Preserva o sinal de
// VALORIZACAO: pode ser negativo (desvalorizacao, item 8.4) e a soma do
// bucket deve refletir isso, nunca usar valor absoluto. DIVIDENDO ja chega
// sempre > 0 na origem (item 8.4, validacao de criacao), entao somar direto
// nunca inverte seu sinal.
export function agruparRendimentosPorMes(
  historico: RendimentoResponse[],
): PontoRendimentosPorMes[] {
  const buckets = new Map<string, PontoRendimentosPorMes>()

  for (const rendimento of historico) {
    const mes = rendimento.data.slice(0, 7)
    const bucket = buckets.get(mes) ?? { mes, dividendo: 0, valorizacao: 0 }

    if (rendimento.tipo === "DIVIDENDO") {
      bucket.dividendo += rendimento.valor
    } else {
      bucket.valorizacao += rendimento.valor
    }

    buckets.set(mes, bucket)
  }

  return [...buckets.values()].sort((a, b) => a.mes.localeCompare(b.mes))
}
