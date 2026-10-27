// Espelha PlanoMensalResponse / SalvarPlanoMensalRequest do back. Todo valor
// derivado (sobra, naoAlocado, investimento, estourado...) vem pronto do
// backend - o front so exibe.
export type SublimiteResponse = {
  categoriaId: string
  parentId: string | null
  nome: string
  icone: string | null
  categoriaArquivada: boolean
  percentual: number
  valor: number
  gastoRealizado: number
  fracaoUtilizada: number
  estourado: boolean
}

export type GastoCategoriaResponse = {
  categoriaId: string
  gastoRealizado: number
}

export type PlanoMensalResponse = {
  ano: number
  mes: number
  somenteLeitura: boolean
  rendaPrevista: number
  fixas: number
  percentualInvestimento: number
  investimentoPlanejado: number
  investimento: number
  investimentoReduzido: boolean
  planoNaoFecha: boolean
  sobra: number
  naoAlocado: number
  gastoSemSublimite: number
  sublimites: SublimiteResponse[]
  gastosPorCategoria: GastoCategoriaResponse[]
}

export type SublimiteRequest = {
  categoriaId: string
  percentual: number
}

// Substitui o conjunto inteiro de sublimites (lista vazia = sem sublimites).
export type SalvarPlanoMensalRequest = {
  rendaPrevista: number
  percentualInvestimento: number
  sublimites: SublimiteRequest[]
}
