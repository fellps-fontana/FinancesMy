// Comparativo gasto variavel x sublimite do plano mensal (item 14/20) de uma
// categoria, no mes consultado. Derivado de SublimiteResponse por
// lib/sublimiteParaGastoVsLimite.ts. Categoria-pai soma tambem o gasto das subcategorias diretas
// (item 14, hierarquia) - essa soma ja vem pronta do backend, o front so
// exibe.
export type GastoVsLimiteResponse = {
  categoriaId: string
  categoriaNome: string
  valorLimite: number
  // Percentual do sublimite dentro do plano do mes (0-100), como cadastrado.
  percentualSublimite: number
  gastoRealizado: number
  // Fracao 0..1+ (1 = 100%), vinda pronta do back (fracaoUtilizada do plano).
  percentualUtilizado: number
  estourado: boolean
}
