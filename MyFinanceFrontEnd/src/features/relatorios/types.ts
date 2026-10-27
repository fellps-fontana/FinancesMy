// Relatorio de gasto por categoria (regra-de-negocio.md itens 14 e 19).
// Totais, percentuais (ja em 0-100) e variacao vem prontos do backend - o
// front so exibe, nunca recalcula.
export type LinhaGastoPorCategoria = {
  // null => linha "Sem categoria"
  categoriaId: string | null
  nome: string
  icone: string | null
  valor: number
  percentual: number
}

export type RelatorioGastoPorCategoriaResponse = {
  ano: number
  mes: number
  totalGasto: number
  totalMesAnterior: number
  // null quando nao ha base de comparacao (mes anterior zerado)
  variacaoPercentual: number | null
  linhas: LinhaGastoPorCategoria[]
}

export type LimiteRelatorio = {
  valorLimite: number
  gastoRealizado: number
  // Calculado pelo back; o front nao recalcula estouro.
  estourado: boolean
}

export type ContaTipoRelatorio = "BANCO" | "CARTAO" | "INVESTIMENTO"

export type LancamentoRelatorio = {
  id: string
  descricao: string | null
  // "yyyy-MM-dd"
  data: string
  contaId: string
  contaNome: string
  contaTipo: ContaTipoRelatorio | null
  subcategoriaNome: string | null
  valor: number
}

export type RelatorioCategoriaDetalheResponse = {
  categoriaId: string | null
  nome: string
  icone: string | null
  total: number
  percentualDoTotal: number
  // presente so se a categoria tem limite
  limite: LimiteRelatorio | null
  lancamentos: LancamentoRelatorio[]
}
