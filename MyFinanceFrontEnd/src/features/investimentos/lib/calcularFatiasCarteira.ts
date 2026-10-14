import type { AtivoResponse } from "@/features/investimentos/types"

export type FatiaCarteira = {
  ativoId: string
  nome: string
  valor: number
  /** Escala 0-100 (mesma convencao de formatarPercentual). */
  percentual: number
  /** Inicio da fatia no circulo, escala 0-100. */
  inicio: number
}

export type CarteiraDonut = {
  total: number
  fatias: FatiaCarteira[]
}

// Fatias do donut "Carteira": cada ativo ativo com valor atual positivo pesa
// valorAtual / soma dos valores atuais. Apenas agregacao de exibicao sobre
// valores que o backend ja entrega.
export function calcularFatiasCarteira(ativos: ReadonlyArray<AtivoResponse>): CarteiraDonut {
  const elegiveis = ativos.filter((ativo) => ativo.ativa && ativo.valorAtual > 0)
  const total = elegiveis.reduce((soma, ativo) => soma + ativo.valorAtual, 0)

  let acumulado = 0
  const fatias = elegiveis.map<FatiaCarteira>((ativo) => {
    const percentual = total > 0 ? (ativo.valorAtual / total) * 100 : 0
    const fatia = {
      ativoId: ativo.id,
      nome: ativo.nome,
      valor: ativo.valorAtual,
      percentual,
      inicio: acumulado,
    }
    acumulado += percentual
    return fatia
  })

  return { total, fatias }
}
