// Periodicidade (regra-de-negocio.md item 6, revisao 2026-07-27): "MENSAL"
// (padrao) ou "ANUAL". ContaFixaResponse.Periodicidade serializa via
// ToStorageValue() (mesmo padrao de LancamentoResponseDto.Tipo/
// ContaReceberResponse.Tipo), entao Response e Request usam a mesma forma
// canonica em caixa alta.
export type PeriodicidadeContaFixa = "MENSAL" | "ANUAL"

// ContaFixa e um molde (regra-de-negocio.md item 6): ao criar ou reativar
// (ativa false->true), o backend gera o Lancamento DEBIT do mes vigente a
// partir de `diaVencimento` (ajustado se o mes tiver menos dias). Editar
// valor/diaVencimento/periodicidade/categoria propaga para os Lancamentos
// futuros ainda nao pagos; desativar remove esses mesmos Lancamentos
// futuros. Nao existe conta fixa do tipo CREDIT.
export type ContaFixaResponse = {
  id: string
  contaId: string
  categoriaId: string | null
  descricao: string
  valor: number
  diaVencimento: number
  periodicidade: PeriodicidadeContaFixa
  ativa: boolean
}

export type CriarContaFixaRequest = {
  contaId: string
  descricao: string
  valor: number
  diaVencimento: number
  categoriaId?: string
  periodicidade?: "MENSAL" | "ANUAL"
}

export type EditarContaFixaRequest = {
  valor: number
  diaVencimento: number
  categoriaId?: string
  periodicidade?: "MENSAL" | "ANUAL"
}
