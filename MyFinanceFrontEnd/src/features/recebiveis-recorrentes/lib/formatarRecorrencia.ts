// Apresentacao pura da recorrencia (regra-de-negocio.md item 15): converte
// os campos crus do molde num rotulo legivel. Sem estado, sem fetch -
// criterio lib/ do stack.md. NENHUM calculo de proxima ocorrencia acontece
// aqui: o backend materializa as ocorrencias, o front so descreve o molde.

import type {
  DiaDaSemana,
  RecebivelRecorrenteResponse,
} from "@/features/recebiveis-recorrentes/types"

// Rotulo em portugues do dia da semana (o backend guarda o codigo curto
// "SEG".."DOM").
const NOME_DIA_DA_SEMANA: Record<DiaDaSemana, string> = {
  SEG: "Segunda",
  TER: "Terça",
  QUA: "Quarta",
  QUI: "Quinta",
  SEX: "Sexta",
  SAB: "Sábado",
  DOM: "Domingo",
}

// Opcoes do select de mes de referencia (periodicidade ANUAL). `valor` e o
// numero 1-12 que vai no request; `nome` e o rotulo exibido.
export const MESES_DO_ANO: ReadonlyArray<{ valor: number; nome: string }> = [
  { valor: 1, nome: "Janeiro" },
  { valor: 2, nome: "Fevereiro" },
  { valor: 3, nome: "Março" },
  { valor: 4, nome: "Abril" },
  { valor: 5, nome: "Maio" },
  { valor: 6, nome: "Junho" },
  { valor: 7, nome: "Julho" },
  { valor: 8, nome: "Agosto" },
  { valor: 9, nome: "Setembro" },
  { valor: 10, nome: "Outubro" },
  { valor: 11, nome: "Novembro" },
  { valor: 12, nome: "Dezembro" },
]

export function nomeDiaDaSemana(dia: DiaDaSemana): string {
  return NOME_DIA_DA_SEMANA[dia]
}

function doisDigitos(numero: number): string {
  return String(numero).padStart(2, "0")
}

// "Mensal - dia 10" | "Anual - 10/03" | "Semanal - toda Segunda".
export function formatarRecorrencia(molde: RecebivelRecorrenteResponse): string {
  switch (molde.periodicidade) {
    case "MENSAL":
      return `Mensal - dia ${molde.diaVencimento ?? "-"}`
    case "ANUAL":
      return `Anual - ${doisDigitos(molde.diaVencimento ?? 0)}/${doisDigitos(molde.mesReferencia ?? 0)}`
    case "SEMANAL":
      return molde.diaDaSemana
        ? `Semanal - toda ${NOME_DIA_DA_SEMANA[molde.diaDaSemana]}`
        : "Semanal"
  }
}
