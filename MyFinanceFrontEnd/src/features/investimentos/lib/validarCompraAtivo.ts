// Validacao pura do formulario de registrar compra de ativo (regra-de-
// negocio.md secao 8: ticker, quantidade e preco sao os dados minimos que o
// back precisa para criar ou incrementar um ativo na carteira). Testavel
// isoladamente do componente - ver clean-code.md "Organizacao (React)".
export function validarCompraAtivo(
  ticker: string,
  quantidade: string,
  precoUnitario: string,
  data: string,
): string | null {
  if (ticker.trim().length === 0) {
    return "Informe o ticker do ativo."
  }

  const quantidadeNumero = converterCompraParaNumero(quantidade)
  if (quantidade.trim().length === 0 || Number.isNaN(quantidadeNumero)) {
    return "Informe uma quantidade valida."
  }
  if (quantidadeNumero <= 0) {
    return "A quantidade deve ser maior que zero."
  }

  const precoNumero = converterCompraParaNumero(precoUnitario)
  if (precoUnitario.trim().length === 0 || Number.isNaN(precoNumero)) {
    return "Informe um preco valido."
  }
  if (precoNumero <= 0) {
    return "O preco deve ser maior que zero."
  }

  if (data.trim().length === 0) {
    return "Informe a data da compra."
  }

  return null
}

// Conversao pareada com validarCompraAtivo - mesma normalizacao usada em
// validarSaldo.ts (permite virgula como separador decimal). So deve ser
// chamada depois que validarCompraAtivo retornou null.
export function converterCompraParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}
