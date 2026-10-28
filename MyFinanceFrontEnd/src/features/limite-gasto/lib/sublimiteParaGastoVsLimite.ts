import type { PlanoMensalResponse, SublimiteResponse } from "@/features/planejamento-mensal/types"
import type { GastoVsLimiteResponse } from "@/features/limite-gasto/types"

// Mapeamento puro (sem regra): o back ja entrega valor, gasto, fracao e estouro.
export function sublimiteParaGastoVsLimite(sublimite: SublimiteResponse): GastoVsLimiteResponse {
  return {
    categoriaId: sublimite.categoriaId,
    categoriaNome: sublimite.nome,
    valorLimite: sublimite.valor,
    percentualSublimite: sublimite.percentual,
    gastoRealizado: sublimite.gastoRealizado,
    percentualUtilizado: sublimite.fracaoUtilizada,
    estourado: sublimite.estourado,
  }
}

// Mes sem plano (null) ou ainda carregando (undefined) nao tem sublimites.
export function sublimitesParaGastoVsLimite(
  plano: PlanoMensalResponse | null | undefined,
): GastoVsLimiteResponse[] {
  return plano ? plano.sublimites.map(sublimiteParaGastoVsLimite) : []
}
