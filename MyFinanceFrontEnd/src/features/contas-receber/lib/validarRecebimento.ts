// Validacao pura do formulario de registro de recebimento (regra-de-negocio.md
// item 13: recebimento incremental sobre um conta_receber existente). O
// backend e quem decide se o valor excede o saldo_pendente atual
// (ValorRecebimentoExcedeSaldoPendenteException, 422, saldo_pendente nunca
// fica negativo) - aqui so garantimos que o valor informado e um numero
// positivo antes de chamar a API, mesmo espirito de validarContaReceber.ts
// (parse/regex nao duplicado a esmo, cada dominio com sua propria regra de
// "o que e valido" pro seu campo).
function validarValorRecebimento(valor: string): string | null {
  const valorNormalizado = valor.trim().replace(",", ".")

  if (valorNormalizado.length === 0) {
    return "Informe o valor recebido."
  }

  const valorNumerico = Number(valorNormalizado)

  if (Number.isNaN(valorNumerico)) {
    return "Informe um valor valido."
  }

  if (valorNumerico <= 0) {
    return "O valor recebido deve ser maior que zero."
  }

  return null
}

// Conversao pareada com validarValorRecebimento - so deve ser chamada depois
// que a validacao correspondente retornou null (valor ja confirmado como
// numero positivo valido).
export function converterValorRecebimentoParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}

// Recebimento incremental (item 13): valor > 0 e contaDestinoId selecionado
// sao os dois requisitos client-side. Se o valor exceder o saldo_pendente, a
// rejeicao vem do backend - nao ha calculo de saldo aqui (clean-code.md:
// calculo de dominio nao vive no componente/lib do front).
export function validarRecebimento(valor: string, contaDestinoId: string): string | null {
  const erroValor = validarValorRecebimento(valor)
  if (erroValor) {
    return erroValor
  }

  if (contaDestinoId.length === 0) {
    return "Selecione a conta de destino do recebimento."
  }

  return null
}
