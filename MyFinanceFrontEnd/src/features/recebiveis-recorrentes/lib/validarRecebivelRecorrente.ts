// Validacao pura do formulario de Recebivel Recorrente (regra-de-negocio.md
// item 15). Espelha validarContaFixa.ts: funcao sem estado, testavel
// isolada do componente (clean-code.md "Organizacao (React)"). O backend
// (RecebivelRecorrenteService.ValidarERecortarCampos) e a fonte da verdade -
// isto so evita ida ao servidor para erro obvio.

import type { PeriodicidadeRecebivelRecorrente } from "@/features/recebiveis-recorrentes/types"

const DIA_VENCIMENTO_MIN = 1
const DIA_VENCIMENTO_MAX = 31
const MES_REFERENCIA_MIN = 1
const MES_REFERENCIA_MAX = 12

function validarValor(valor: string): string | null {
  const valorNormalizado = valor.trim().replace(",", ".")

  if (valorNormalizado.length === 0) {
    return "Informe o valor."
  }

  const valorNumerico = Number(valorNormalizado)

  if (Number.isNaN(valorNumerico)) {
    return "Informe um valor valido."
  }

  if (valorNumerico <= 0) {
    return "O valor deve ser maior que zero."
  }

  return null
}

// Conversao pareada com validarValor - so chamar depois que a validacao
// retornou null (valor ja confirmado como numero positivo).
export function converterValorParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}

function validarInteiroNoIntervalo(
  bruto: string,
  min: number,
  max: number,
  mensagemVazio: string,
  mensagemForaDoIntervalo: string,
): string | null {
  if (bruto.trim().length === 0) {
    return mensagemVazio
  }

  const numero = Number(bruto)

  if (!Number.isInteger(numero) || numero < min || numero > max) {
    return mensagemForaDoIntervalo
  }

  return null
}

// Conversao pareada com validarInteiroNoIntervalo - mesma regra de validar
// antes de converter que converterValorParaNumero.
export function converterInteiroParaNumero(bruto: string): number {
  return Number(bruto)
}

// Campo obrigatorio conforme a periodicidade (item 15):
// - MENSAL: diaVencimento (1-31);
// - ANUAL:  mesReferencia (1-12) + diaVencimento (1-31);
// - SEMANAL: diaDaSemana selecionado.
function validarCamposDaPeriodicidade(
  periodicidade: PeriodicidadeRecebivelRecorrente,
  diaVencimento: string,
  mesReferencia: string,
  diaDaSemana: string,
): string | null {
  if (periodicidade === "MENSAL") {
    return validarInteiroNoIntervalo(
      diaVencimento,
      DIA_VENCIMENTO_MIN,
      DIA_VENCIMENTO_MAX,
      "Informe o dia de vencimento.",
      `O dia de vencimento deve ser um numero entre ${DIA_VENCIMENTO_MIN} e ${DIA_VENCIMENTO_MAX}.`,
    )
  }

  if (periodicidade === "ANUAL") {
    const erroMes = validarInteiroNoIntervalo(
      mesReferencia,
      MES_REFERENCIA_MIN,
      MES_REFERENCIA_MAX,
      "Selecione o mes de referencia.",
      `O mes de referencia deve ser um numero entre ${MES_REFERENCIA_MIN} e ${MES_REFERENCIA_MAX}.`,
    )
    if (erroMes) {
      return erroMes
    }

    return validarInteiroNoIntervalo(
      diaVencimento,
      DIA_VENCIMENTO_MIN,
      DIA_VENCIMENTO_MAX,
      "Informe o dia de vencimento.",
      `O dia de vencimento deve ser um numero entre ${DIA_VENCIMENTO_MIN} e ${DIA_VENCIMENTO_MAX}.`,
    )
  }

  // SEMANAL
  if (diaDaSemana.trim().length === 0) {
    return "Selecione o dia da semana."
  }

  return null
}

// CRIAR: exige descricao alem de valor e do campo da periodicidade.
export function validarCriarRecebivelRecorrente(
  descricao: string,
  valor: string,
  periodicidade: PeriodicidadeRecebivelRecorrente,
  diaVencimento: string,
  mesReferencia: string,
  diaDaSemana: string,
): string | null {
  if (descricao.trim().length === 0) {
    return "Informe uma descricao."
  }

  const erroValor = validarValor(valor)
  if (erroValor) {
    return erroValor
  }

  return validarCamposDaPeriodicidade(periodicidade, diaVencimento, mesReferencia, diaDaSemana)
}

// EDITAR: EditarRecebivelRecorrenteRequest nao aceita descricao (types.ts) -
// nao ha o que validar aqui alem de valor e do campo da periodicidade.
export function validarEditarRecebivelRecorrente(
  valor: string,
  periodicidade: PeriodicidadeRecebivelRecorrente,
  diaVencimento: string,
  mesReferencia: string,
  diaDaSemana: string,
): string | null {
  const erroValor = validarValor(valor)
  if (erroValor) {
    return erroValor
  }

  return validarCamposDaPeriodicidade(periodicidade, diaVencimento, mesReferencia, diaDaSemana)
}
