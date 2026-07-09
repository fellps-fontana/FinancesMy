// Validacao pura do formulario de criacao da conta CARTAO (regra de negocio
// item 12). Dia de fechamento/vencimento sao obrigatorios e devem estar
// entre 1 e 31 - mesma faixa validada no backend (ContaService.ValidarCartao),
// checada aqui so pra dar feedback sem ida ao servidor.
export function validarNovaContaCartao(
  nome: string,
  diaFechamento: string,
  diaVencimento: string,
): string | null {
  if (nome.trim().length === 0) {
    return "Informe um nome para o cartao."
  }

  const fechamento = Number(diaFechamento)
  if (!Number.isInteger(fechamento) || fechamento < 1 || fechamento > 31) {
    return "Dia de fechamento deve ser um numero entre 1 e 31."
  }

  const vencimento = Number(diaVencimento)
  if (!Number.isInteger(vencimento) || vencimento < 1 || vencimento > 31) {
    return "Dia de vencimento deve ser um numero entre 1 e 31."
  }

  return null
}
