import type { GastoVsLimiteResponse } from "@/features/limite-gasto/types"

// Threshold de "perto do limite" (regra-de-negocio.md item 14 nao define
// esse corte - so define "estourar" como gasto > limite). Este modulo e o dono
// e a fonte unica do corte visual; os demais consumidores devem importar daqui.
export const LIMIAR_PERTO_DO_LIMITE = 0.8

const PERCENTUAL_POR_FRACAO = 100
const LARGURA_CHEIA = 100
const formatadorPercentual = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 2 })

export type EstadoLimite = "estourado" | "perto" | "ok"

export function classificarEstadoLimite(
  fracaoUtilizada: number,
  estourado: boolean,
): EstadoLimite {
  if (estourado) return "estourado"
  if (fracaoUtilizada >= LIMIAR_PERTO_DO_LIMITE) return "perto"
  return "ok"
}

// `estourado` vem do backend ou do preview live; o front so classifica o corte visual.
export function obterEstadoLimite(
  item: Pick<GastoVsLimiteResponse, "estourado" | "percentualUtilizado">,
): EstadoLimite {
  return classificarEstadoLimite(item.percentualUtilizado, item.estourado)
}

// Largura visual (0-100) da barra de uso. Estourado => barra cheia, mesmo
// quando o back devolve fracaoUtilizada 0 (sublimite R$ 0 com gasto > 0).
export function larguraBarraUso(fracaoUtilizada: number, estourado: boolean): number {
  if (estourado) return LARGURA_CHEIA
  return Math.min(LARGURA_CHEIA, Math.max(0, fracaoUtilizada * PERCENTUAL_POR_FRACAO))
}

// Texto do uso. Estourado com fracao 0 nao pode mostrar "0%" (contradiz o
// estouro): vira "Estourado". Com fracao > 0 mostra o percentual real.
export function rotuloUso(fracaoUtilizada: number, estourado: boolean): string {
  if (estourado && fracaoUtilizada <= 0) return "Estourado"
  return `${formatadorPercentual.format(fracaoUtilizada * PERCENTUAL_POR_FRACAO)}%`
}
