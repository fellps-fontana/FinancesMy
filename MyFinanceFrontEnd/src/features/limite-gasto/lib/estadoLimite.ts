import type { GastoVsLimiteResponse } from "@/features/limite-gasto/types"

// Threshold de "perto do limite" (regra-de-negocio.md item 14 nao define
// esse corte - so define "estourar" como gasto > limite). Este modulo e o dono
// e a fonte unica do corte visual; os demais consumidores devem importar daqui.
export const LIMIAR_PERTO_DO_LIMITE = 0.8

export type EstadoLimite = "estourado" | "perto" | "ok"

// `estourado` vem do backend; o front so classifica o corte visual.
export function obterEstadoLimite(
  item: Pick<GastoVsLimiteResponse, "estourado" | "percentualUtilizado">,
): EstadoLimite {
  if (item.estourado) return "estourado"
  if (item.percentualUtilizado >= LIMIAR_PERTO_DO_LIMITE) return "perto"
  return "ok"
}

// A razao pode passar de 100% quando estourado: a barra e limitada a 100%,
// o percentual exibido continua o valor real.
export function larguraBarraPercentual(percentualUtilizado: number): number {
  return Math.min(Math.max(percentualUtilizado * 100, 0), 100)
}
