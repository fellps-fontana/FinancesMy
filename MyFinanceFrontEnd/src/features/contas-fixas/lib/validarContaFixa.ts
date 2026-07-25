// Validacao pura do formulario de ContaFixa (regra-de-negocio.md item 6).
// Testavel isoladamente do componente - ver clean-code.md
// "Organizacao (React)".

const DIA_VENCIMENTO_MIN = 1
const DIA_VENCIMENTO_MAX = 31

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

// Conversao pareada com validarValor - so deve ser chamada depois que a
// validacao correspondente retornou null (valor ja confirmado como numero
// positivo valido).
export function converterValorParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}

// dia_vencimento aceita 1-31 (regra-de-negocio.md item 6). Mes com menos
// dias que o valor escolhido (ex: 31 em abril, fevereiro) e ajustado pelo
// backend para o ultimo dia do mes na geracao do Lancamento - o form so
// garante que o numero digitado esteja dentro do range valido, o ajuste de
// calendario nao e responsabilidade da UI.
function validarDiaVencimento(diaVencimento: string): string | null {
  if (diaVencimento.trim().length === 0) {
    return "Informe o dia de vencimento."
  }

  const dia = Number(diaVencimento)

  if (!Number.isInteger(dia) || dia < DIA_VENCIMENTO_MIN || dia > DIA_VENCIMENTO_MAX) {
    return `O dia de vencimento deve ser um numero entre ${DIA_VENCIMENTO_MIN} e ${DIA_VENCIMENTO_MAX}.`
  }

  return null
}

// Conversao pareada com validarDiaVencimento - mesma regra de validar antes
// de converter que converterValorParaNumero.
export function converterDiaVencimentoParaNumero(diaVencimentoBruto: string): number {
  return Number(diaVencimentoBruto)
}

// CRIAR: alem de descricao, valor e dia_vencimento, exige a conta de origem
// (CriarContaFixaRequest.contaId - regra-de-negocio.md item 6: "sempre
// vinculado a uma conta de origem").
export function validarCriarContaFixa(
  descricao: string,
  valor: string,
  diaVencimento: string,
  contaId: string,
): string | null {
  if (descricao.trim().length === 0) {
    return "Informe uma descricao."
  }

  const erroValor = validarValor(valor)
  if (erroValor) {
    return erroValor
  }

  const erroDiaVencimento = validarDiaVencimento(diaVencimento)
  if (erroDiaVencimento) {
    return erroDiaVencimento
  }

  if (contaId.length === 0) {
    return "Selecione a conta de origem."
  }

  return null
}

// EDITAR: EditarContaFixaRequest so aceita valor, diaVencimento e
// categoriaId (types.ts) - contaId (conta de origem) nao faz parte do
// contrato de edicao, entao nao ha o que validar aqui alem de valor e
// dia_vencimento.
export function validarEditarContaFixa(valor: string, diaVencimento: string): string | null {
  const erroValor = validarValor(valor)
  if (erroValor) {
    return erroValor
  }

  return validarDiaVencimento(diaVencimento)
}
