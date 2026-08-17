// Validacao pura do valor de saldo digitado pelo usuario - usada na criacao
// de conta (features/contas/components/FormNovaConta.tsx, saldoInicial), na
// edicao de saldo_manual de conta ja existente
// (features/contas/components/ContaCard.tsx, novoSaldo) e no modulo de
// investimento (conta de investimento simples, mesmo campo saldo_manual).
// Promovida pra shared/lib por directiva explicita da correcao do style
// (Bloco G/Contas): a mesma validacao vivia duplicada e DIVERGENTE em
// features/contas e features/investimentos - ambas com o mesmo campo de
// dominio (Conta.saldo_manual), entao a fonte unica fica aqui em vez de uma
// feature importar da outra.
//
// regra-de-negocio.md item 10: saldo_manual PODE ser negativo em toda conta
// manual (decisao confirmada pelo usuario) - o backend ja aceita e testa
// saldo negativo (ContasControllerTests.cs::
// CriarContaInvestimento_ComSaldoNegativo_Retorna201). Esta validacao so
// garante um numero valido preenchido - nunca rejeita por sinal.
export function validarSaldo(valorBruto: string): string | null {
  const valorNormalizado = valorBruto.trim().replace(",", ".")

  if (valorNormalizado.length === 0) {
    return "Informe o saldo."
  }

  const saldo = Number(valorNormalizado)

  if (Number.isNaN(saldo)) {
    return "Informe um saldo valido."
  }

  return null
}

// Conversao pareada com validarSaldo - so deve ser chamada depois que
// validarSaldo retornou null (valor ja confirmado como numero valido,
// positivo ou negativo).
export function converterSaldoParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}
