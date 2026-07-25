// ContaFixa e um molde (regra-de-negocio.md item 6): ao criar ou reativar
// (ativa false->true), o backend gera o Lancamento DEBIT do mes vigente a
// partir de `diaVencimento` (ajustado se o mes tiver menos dias). Editar
// valor/diaVencimento/categoria propaga para os Lancamentos futuros ainda
// nao pagos; desativar remove esses mesmos Lancamentos futuros. Nao existe
// conta fixa do tipo CREDIT.
export type ContaFixaResponse = {
  id: string
  contaId: string
  categoriaId: string | null
  descricao: string
  valor: number
  diaVencimento: number
  ativa: boolean
}

export type CriarContaFixaRequest = {
  contaId: string
  descricao: string
  valor: number
  diaVencimento: number
  categoriaId?: string
}

export type EditarContaFixaRequest = {
  valor: number
  diaVencimento: number
  categoriaId?: string
}
