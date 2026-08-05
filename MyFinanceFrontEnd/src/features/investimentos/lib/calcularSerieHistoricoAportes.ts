import type { AtivoAporteResponse } from "@/features/investimentos/types"

// Ponto do grafico: quantidade acumulada e preco medio, os dois calculados
// SO a partir dos proprios aportes do usuario (regra-de-negocio.md item 8.1)
// - nunca cotacao de mercado (item 8, proibido em todas as fases da v1).
export type PontoHistoricoAportes = {
  data: string
  quantidadeAcumulada: number
  precoMedio: number
}

// Preco medio por media ponderada (regra-de-negocio.md item 8.1):
// preco_medio = valor_investido_acumulado / quantidade_acumulada. Aportes
// chegam do backend sem garantia de ordem -> ordena por data antes de
// acumular, senao a serie do grafico anda pra tras. Funcao pura, testavel
// isolada (stack.md, criterio lib/: sem JSX, sem hook, sem efeito colateral).
export function calcularSerieHistoricoAportes(
  aportes: AtivoAporteResponse[],
): PontoHistoricoAportes[] {
  const ordenados = [...aportes].sort((a, b) => a.data.localeCompare(b.data))

  let quantidadeAcumulada = 0
  let valorInvestidoAcumulado = 0

  return ordenados.map((aporte) => {
    quantidadeAcumulada += aporte.quantidade
    valorInvestidoAcumulado += aporte.valorTotal

    return {
      data: aporte.data,
      quantidadeAcumulada,
      precoMedio: quantidadeAcumulada === 0 ? 0 : valorInvestidoAcumulado / quantidadeAcumulada,
    }
  })
}
