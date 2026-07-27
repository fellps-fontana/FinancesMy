// Validacao/conversao de valor monetario compartilhada por FormLancamento.tsx
// e FormTransferencia.tsx (ambos tratam "valor" da mesma forma - numero
// positivo, aceitando virgula como separador decimal). Extraido pra evitar
// duplicacao entre os dois forms - mesmo espirito de
// contas-fixas/lib/validarContaFixa.ts (clean-code.md, "Evite duplicacao").

export function validarValor(valor: string): string | null {
  const valorNormalizado = valor.trim().replace(",", ".")

  if (valorNormalizado.length === 0) {
    return "Informe o valor."
  }

  const valorNumerico = Number(valorNormalizado)

  if (Number.isNaN(valorNumerico) || valorNumerico <= 0) {
    return "Informe um valor valido, maior que zero."
  }

  return null
}

// Conversao pareada com validarValor - so deve ser chamada depois que a
// validacao correspondente retornou null (valor ja confirmado como numero
// positivo valido).
export function converterValorParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}
