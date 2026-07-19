// Validacao pura de um valor monetario que precisa ser ESTRITAMENTE positivo
// (> 0) - regra usada tanto para valorInvestido na criacao de ativo quanto
// para valorAtual na edicao (o back retorna 400 se <= 0 nos dois casos, ver
// Controllers/AtivosController.cs). Difere de validarSaldo (que aceita 0),
// por isso vive em arquivo proprio em vez de reaproveitar aquela funcao.
export function validarValorPositivo(valorBruto: string, mensagemVazio: string): string | null {
  const valorNormalizado = valorBruto.trim().replace(",", ".")

  if (valorNormalizado.length === 0) {
    return mensagemVazio
  }

  const valor = Number(valorNormalizado)

  if (Number.isNaN(valor)) {
    return "Informe um valor valido."
  }

  if (valor <= 0) {
    return "O valor deve ser maior que zero."
  }

  return null
}

// Conversao pareada com validarValorPositivo - so deve ser chamada depois que
// validarValorPositivo retornou null (valor ja confirmado como numero valido).
export function converterValorParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}
