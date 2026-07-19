// Validacao pura do formulario de registro de Contas a Receber (recebivel e
// emprestimo, regra-de-negocio.md item 13). Testavel isoladamente do
// componente - ver clean-code.md "Organizacao (React)".
//
// Nao reaproveita validarSaldo de features/investimentos: aquela funcao
// aceita saldo zero (saldo manual de conta pode nascer zerado). Aqui
// valorTotal precisa ser estritamente positivo - um recebivel ou emprestimo
// de valor zero nao tem sentido de dominio (item 13: e o valor esperado a
// entrar, base do calculo de saldo_pendente). Por isso a conversao numerica
// e replicada localmente em vez de importada cross-feature so por essa
// diferenca de regra.
function validarValorTotal(valorTotal: string): string | null {
  const valorNormalizado = valorTotal.trim().replace(",", ".")

  if (valorNormalizado.length === 0) {
    return "Informe o valor total."
  }

  const valor = Number(valorNormalizado)

  if (Number.isNaN(valor)) {
    return "Informe um valor total valido."
  }

  if (valor <= 0) {
    return "O valor total deve ser maior que zero."
  }

  return null
}

// Conversao pareada com validarValorTotal - so deve ser chamada depois que a
// validacao correspondente retornou null (valor ja confirmado como numero
// positivo valido).
export function converterValorTotalParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}

// RECEBIVEL: expectativa solta, sem vinculo com conta/origem no sistema
// (item 13) - so precisa de descricao e valor.
export function validarRecebivel(descricao: string, valorTotal: string): string | null {
  if (descricao.trim().length === 0) {
    return "Informe uma descricao."
  }

  return validarValorTotal(valorTotal)
}

// EMPRESTIMO: alem de descricao e valor, exige `pessoa` (texto livre, sem
// cadastro proprio - item 13) e a conta de origem de onde o valor emprestado
// sai via transferencia de perna unica (item 13).
export function validarEmprestimo(
  descricao: string,
  pessoa: string,
  valorTotal: string,
  contaOrigemId: string,
): string | null {
  const erroRecebivel = validarRecebivel(descricao, valorTotal)
  if (erroRecebivel) {
    return erroRecebivel
  }

  if (pessoa.trim().length === 0) {
    return "Informe a pessoa que recebeu o emprestimo."
  }

  if (contaOrigemId.length === 0) {
    return "Selecione a conta de origem do emprestimo."
  }

  return null
}
