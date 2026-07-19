// Validacao pura do formulario de lancamento de compra no cartao (regra de
// negocio item 12). Compra e sempre um DEBIT (gasto) no cartao - por isso o
// valor precisa ser maior que zero, sem exigir sinal do usuario.
export function validarCompra(descricao: string, valor: string, data: string): string | null {
  if (descricao.trim().length === 0) {
    return "Informe uma descricao para a compra."
  }

  const valorNumerico = Number(valor.trim().replace(",", "."))
  if (Number.isNaN(valorNumerico) || valorNumerico <= 0) {
    return "Informe um valor valido, maior que zero."
  }

  if (data.trim().length === 0) {
    return "Informe a data da compra."
  }

  return null
}
