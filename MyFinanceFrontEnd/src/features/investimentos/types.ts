// Nomes de campo iguais ao ContaResponse do backend - enums ja chegam
// serializados como string exata (tipo: "Investimento", origem: "Manual").
// `saldo` vem sempre populado (manual ou calculado a partir dos ativos, ver
// regra-de-negocio.md item 10); `saldoManual` fica null quando a conta esta
// em modo carteira de ativos (item 8).
export type ContaResponse = {
  id: string
  nome: string
  tipo: string
  origem: string
  saldo: number
  saldoManual: number | null
  ativa: boolean
}

export type TotalInvestidoResponse = {
  totalInvestido: number
}

export type CriarContaInvestimentoRequest = {
  nome: string
  saldoInicial: number
}

export type AtualizarSaldoRequest = {
  novoSaldo: number
}

// `nome` do ativo pode ser null quando o backend nao recebeu nome na compra
// (ex: ticker ja conhecido de outra operacao). `precoAtual` e definido
// manualmente pelo usuario a cada compra (regra-de-negocio.md item 8.1);
// `precoMedio` e custo historico calculado no back (item 8.2), nunca usado
// para patrimonio.
export type AtivoResponse = {
  id: string
  ticker: string
  nome: string | null
  quantidade: number
  precoMedio: number
  precoAtual: number
  ativa: boolean
}

export type RegistrarCompraRequest = {
  ticker: string
  quantidade: number
  precoUnitario: number
  data: string
  nome?: string
}

export type RegistrarVendaRequest = {
  quantidade: number
  precoUnitario: number
  data: string
  observacao?: string
}

export type PontoCotacaoResponse = {
  data: string
  preco: number
}

export type CotacaoHistoricoResponse = {
  ticker: string
  pontos: PontoCotacaoResponse[]
}
