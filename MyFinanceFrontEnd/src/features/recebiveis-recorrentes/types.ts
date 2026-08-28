// Recebivel Recorrente (regra-de-negocio.md item 15): molde de entrada
// esperada que recorre no tempo. O backend materializa as ocorrencias como
// registros de Conta a Receber (item 13) - esta feature cuida SO do molde,
// nunca das ocorrencias (a tela de Contas a Receber ja mostra as
// ocorrencias). Familia semantica de ContaFixa (item 6), mas do lado
// CREDIT/receita: nao existe conta de origem, so a expectativa de entrada.

// Periodicidade (item 15): MENSAL usa `diaVencimento`; ANUAL usa
// `mesReferencia` + `diaVencimento`; SEMANAL usa `diaDaSemana`. Response e
// Request compartilham a forma canonica em caixa alta - mesmo padrao de
// ContaFixaResponse.periodicidade (ToStorageValue no backend).
export type PeriodicidadeRecebivelRecorrente = "MENSAL" | "ANUAL" | "SEMANAL"

// Dia da semana da recorrencia SEMANAL - codigo curto em caixa alta, igual
// ao contrato do backend (DiaDaSemana.ToStorageValue). O rotulo em portugues
// ("Segunda".."Domingo") e resolvido na apresentacao (lib/formatarRecorrencia).
export type DiaDaSemana = "SEG" | "TER" | "QUA" | "QUI" | "SEX" | "SAB" | "DOM"

export type RecebivelRecorrenteResponse = {
  id: string
  descricao: string
  valor: number
  periodicidade: PeriodicidadeRecebivelRecorrente
  // Preenchidos conforme a periodicidade (item 15): MENSAL/ANUAL trazem
  // diaVencimento; ANUAL tambem traz mesReferencia; SEMANAL traz diaDaSemana.
  // Os campos incompativeis com a periodicidade vem null.
  diaVencimento: number | null
  mesReferencia: number | null
  diaDaSemana: DiaDaSemana | null
  categoriaId: string | null
  ativa: boolean
}

// CRIAR: alem de descricao e valor, envia a periodicidade e SO o campo de
// data que ela exige. categoriaId e opcional (FK opcional, item 15).
export type CriarRecebivelRecorrenteRequest = {
  descricao: string
  valor: number
  periodicidade: PeriodicidadeRecebivelRecorrente
  diaVencimento?: number
  mesReferencia?: number
  diaDaSemana?: DiaDaSemana
  categoriaId?: string
}

// EDITAR: o contrato do backend nao aceita `descricao` - ela so e exibida no
// form de edicao para dar contexto, nunca reenviada. Os demais campos seguem
// a mesma regra de "envia so o que a periodicidade exige".
export type EditarRecebivelRecorrenteRequest = {
  valor: number
  periodicidade: PeriodicidadeRecebivelRecorrente
  diaVencimento?: number
  mesReferencia?: number
  diaDaSemana?: DiaDaSemana
  categoriaId?: string
}
