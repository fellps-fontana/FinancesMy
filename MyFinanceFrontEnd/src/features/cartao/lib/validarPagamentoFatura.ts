// Validacao pura do formulario de pagamento de fatura (regra de negocio item
// 12, "Pagamento x fatura"): o pagamento pode ser PARCIAL (valor menor que o
// saldo pendente) mas nunca <= 0 nem maior que o pendente. O backend
// (PagamentoFaturaService.PagarFaturaAsync) e quem de fato impede overpayment -
// esta validacao so evita uma ida ao servidor com um valor obviamente invalido.
export function validarPagamentoFatura(
  contaOrigemId: string,
  valor: string,
  data: string,
  valorPendente: number,
): string | null {
  if (contaOrigemId.trim().length === 0) {
    return "Informe a conta de origem do pagamento."
  }

  const valorNumerico = Number(valor.trim().replace(",", "."))
  if (Number.isNaN(valorNumerico) || valorNumerico <= 0) {
    return "Informe um valor valido, maior que zero."
  }

  if (valorNumerico > valorPendente) {
    return "O valor nao pode ser maior que o saldo pendente da fatura."
  }

  if (data.trim().length === 0) {
    return "Informe a data do pagamento."
  }

  return null
}
