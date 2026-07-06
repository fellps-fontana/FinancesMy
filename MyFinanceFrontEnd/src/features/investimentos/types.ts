// Nomes de campo iguais ao ContaResponse do backend - enums ja chegam
// serializados como string exata (tipo: "Investimento", origem: "Manual").
export type ContaResponse = {
  id: string
  nome: string
  tipo: string
  origem: string
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
