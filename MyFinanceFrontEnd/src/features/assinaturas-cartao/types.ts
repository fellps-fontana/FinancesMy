// AssinaturaCartao (regra de negocio: compra recorrente lancada
// automaticamente na fatura do cartao, ex: Netflix/Spotify/academia).
// So e permitida para conta do tipo CARTAO (AssinaturaCartaoService.CriarAsync,
// backend). Espelha o mesmo padrao de ContaFixaResponse (features/contas-fixas),
// mas sem periodicidade (assinatura de cartao e sempre mensal, gera uma
// compra por ciclo de fatura) e sem contaId editavel (a conta de origem so e
// definida na criacao).
export type AssinaturaCartaoResponse = {
  id: string
  contaId: string
  categoriaId: string | null
  descricao: string
  valor: number
  diaReferencia: number
  ativa: boolean
}

export type CriarAssinaturaCartaoRequest = {
  contaId: string
  descricao: string
  valor: number
  diaReferencia: number
  categoriaId?: string
}

export type EditarAssinaturaCartaoRequest = {
  descricao: string
  valor: number
  diaReferencia: number
  categoriaId?: string
}
