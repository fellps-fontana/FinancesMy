// Validacao pura do valor de saldo digitado pelo usuario - reutilizada tanto
// na criacao de conta (saldoInicial) quanto na edicao de saldo manual
// (novoSaldo). Ver regra-de-negocio.md secao 10: saldo de conta manual e
// definido pelo usuario, mas precisa continuar sendo um numero valido e nao
// negativo (nao existe saldo manual negativo neste dominio).
export function validarSaldo(valorBruto: string): string | null {
  const valorNormalizado = valorBruto.trim().replace(",", ".")

  if (valorNormalizado.length === 0) {
    return "Informe o saldo."
  }

  const saldo = Number(valorNormalizado)

  if (Number.isNaN(saldo)) {
    return "Informe um saldo valido."
  }

  if (saldo < 0) {
    return "O saldo nao pode ser negativo."
  }

  return null
}

// Conversao pareada com validarSaldo - so deve ser chamada depois que
// validarSaldo retornou null (valor ja confirmado como numero valido).
export function converterSaldoParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}
