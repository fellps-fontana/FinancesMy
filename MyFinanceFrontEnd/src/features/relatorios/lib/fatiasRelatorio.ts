import type { LinhaGastoPorCategoria } from "@/features/relatorios/types"

// Tons do acento roxo por posicao da linha (mockup s_relatorio). Cada tom tem
// as classes de quadradinho/barra (fundo) e de traco do donut, para legenda,
// barra e fatia sempre combinarem. Ciclam se houver mais linhas que tons.
export type CorFatia = { fundo: string; traco: string }

const PALETA: ReadonlyArray<CorFatia> = [
  { fundo: "bg-primary", traco: "stroke-primary" },
  { fundo: "bg-primary/75", traco: "stroke-primary/75" },
  { fundo: "bg-primary/55", traco: "stroke-primary/55" },
  { fundo: "bg-primary/40", traco: "stroke-primary/40" },
  { fundo: "bg-accent-soft", traco: "stroke-accent-soft" },
  { fundo: "bg-accent-deep", traco: "stroke-accent-deep" },
]

export function corDaFatia(indice: number): CorFatia {
  return PALETA[indice % PALETA.length]
}

// Largura (0-100) de uma barra a partir de um percentual ja entregue pelo
// backend; so limita ao intervalo visivel.
export function larguraDaBarra(percentual: number): number {
  return Math.min(100, Math.max(0, percentual))
}

// Largura (0-100) da barra do limite. Estourado (flag do backend) => cheia,
// inclusive limite R$ 0; senao e so a proporcao visual gasto/limite.
export function larguraDoLimite(gastoRealizado: number, valorLimite: number, estourado: boolean): number {
  if (estourado) return 100
  return valorLimite > 0 ? (gastoRealizado / valorLimite) * 100 : 0
}

export type FatiaDonut = {
  chave: string
  percentual: number
  /** Inicio da fatia no circulo, escala 0-100. */
  inicio: number
  cor: CorFatia
}

export const CHAVE_SEM_CATEGORIA = "sem-categoria"

export function chaveDaLinha(linha: LinhaGastoPorCategoria): string {
  return linha.categoriaId ?? CHAVE_SEM_CATEGORIA
}

// Geometria do donut: cada fatia ocupa `percentual` (do backend) do circulo,
// uma depois da outra, na mesma ordem das linhas.
export function calcularFatiasDonut(linhas: ReadonlyArray<LinhaGastoPorCategoria>): FatiaDonut[] {
  let acumulado = 0
  return linhas.map((linha, indice) => {
    const fatia = { chave: chaveDaLinha(linha), percentual: linha.percentual, inicio: acumulado, cor: corDaFatia(indice) }
    acumulado += linha.percentual
    return fatia
  })
}
