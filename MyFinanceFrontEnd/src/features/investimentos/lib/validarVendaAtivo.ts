// Validacao pura do formulario de registrar venda de ativo (regra-de-
// negocio.md item 8.3: venda reduz a quantidade do ativo, ou desativa o
// ativo quando a quantidade chega a zero - e so registro interno da
// carteira, sem lancamento em outra conta). A validacao que realmente conta
// contra a posicao real e feita no backend (QuantidadeVendaInvalidaException,
// 422) - aqui e so UX, para o usuario nao digitar mais do que tem antes de
// enviar. Mesmo espirito de validarCompraAtivo.ts - testavel isoladamente do
// componente, ver clean-code.md "Organizacao (React)".
export function validarVendaAtivo(
  quantidade: string,
  precoUnitario: string,
  data: string,
  quantidadeDisponivel: number,
): string | null {
  const quantidadeNumero = converterVendaParaNumero(quantidade)
  if (quantidade.trim().length === 0 || Number.isNaN(quantidadeNumero)) {
    return "Informe uma quantidade valida."
  }
  if (quantidadeNumero <= 0) {
    return "A quantidade deve ser maior que zero."
  }
  if (quantidadeNumero > quantidadeDisponivel) {
    return "A quantidade nao pode ser maior que a que voce possui nesta posicao."
  }

  const precoNumero = converterVendaParaNumero(precoUnitario)
  if (precoUnitario.trim().length === 0 || Number.isNaN(precoNumero)) {
    return "Informe um preco valido."
  }
  if (precoNumero <= 0) {
    return "O preco deve ser maior que zero."
  }

  if (data.trim().length === 0) {
    return "Informe a data da venda."
  }

  return null
}

// Conversao pareada com validarVendaAtivo - mesma normalizacao usada em
// validarCompraAtivo.ts/validarSaldo.ts (permite virgula como separador
// decimal). So deve ser chamada depois que validarVendaAtivo retornou null.
export function converterVendaParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}
