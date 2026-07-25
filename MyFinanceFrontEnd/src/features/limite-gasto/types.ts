// Limite de gasto por categoria (regra-de-negocio.md item 14): alerta de
// orcamento mensal, SEM bloqueio de lancamento. `percentualUtilizado` e
// `estourado` vem crus do backend (gasto_realizado_no_mes / valor_limite e
// gasto_realizado_no_mes > valor_limite) - nenhum threshold de UX (ex:
// "perto do limite") e decidido aqui, so pelos consumidores (TASK-059 a 062).
export type LimiteGastoResponse = {
  id: string
  categoriaId: string
  categoriaNome: string
  valorLimite: number
  periodo: string
}

// Comparativo gasto realizado x limite definido para uma categoria, no mes
// consultado. Categoria-pai soma tambem o gasto das subcategorias diretas
// (item 14, hierarquia) - essa soma ja vem pronta do backend, o front so
// exibe.
export type GastoVsLimiteResponse = {
  categoriaId: string
  categoriaNome: string
  valorLimite: number
  gastoRealizado: number
  percentualUtilizado: number
  estourado: boolean
}

export type DefinirLimiteGastoRequest = {
  categoriaId: string
  valorLimite: number
}
