// Duas naturezas da mesma entidade conta_receber (regra-de-negocio.md item
// 13): RECEBIVEL e uma expectativa solta, sem vinculo com conta/origem no
// sistema; EMPRESTIMO tem `pessoa` preenchida e nasceu de uma transferencia
// de perna unica saindo de uma conta do usuario. `valorTotal` e fixo (sem
// juros/correcao); `saldoPendente` e `status` sao derivados dos recebimentos
// ja registrados (ver formula no item 13), nunca editados direto pelo front.
export type ContaReceberResponse = {
  id: string
  tipo: string
  descricao: string
  pessoa: string | null
  valorTotal: number
  saldoPendente: number
  status: string
  dataRegistro: string
  dataPrevista: string | null
}

export type RecebimentoResponse = {
  id: string
  valor: number
  data: string
  contaId: string
  categoriaId: string | null
  contaReceberId: string | null
}

export type TotalAReceberEsperadoResponse = {
  totalAReceberEsperadoNoMes: number
}

export type CriarRecebivelRequest = {
  descricao: string
  valorTotal: number
  dataRegistro: string
  dataPrevista?: string
  categoriaId?: string
}

// `contaOrigemId` e obrigatorio aqui (e so aqui, entre os dois tipos): o
// valor emprestado sai de uma conta real do usuario via transferencia de
// perna unica, sem conta destino (regra-de-negocio.md item 13).
export type CriarEmprestimoRequest = {
  descricao: string
  pessoa: string
  valorTotal: number
  contaOrigemId: string
  dataRegistro: string
  dataPrevista?: string
  categoriaId?: string
}

// Recebimento que exceder o saldo_pendente atual e rejeitado pelo backend
// (item 13, saldo_pendente nunca fica negativo) - o front so repassa o erro
// vindo da API, a validacao de dominio nao vive aqui.
export type RegistrarRecebimentoRequest = {
  valor: number
  data: string
  contaDestinoId: string
  categoriaId?: string
}
