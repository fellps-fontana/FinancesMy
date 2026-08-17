import type { ContaResponse } from "@/features/contas/types"

// Patrimonio total desta tela = soma do saldo de todas as contas manuais
// exibidas (Banco + Investimento; Cartao fica fora, ver ESCOPO da tela -
// cartao e linha de credito, nao patrimonio). `conta.saldo` ja vem resolvido
// pelo backend (ContaResponse.FromConta: SaldoManual ?? 0 - regra-de-
// negocio.md item 10), entao a soma aqui e so agregacao de exibicao, nao
// recalculo de regra de dominio. Funcao pura e testavel (clean-code.md).
export function calcularPatrimonioTotal(contas: ContaResponse[]): number {
  return contas.reduce((total, conta) => total + conta.saldo, 0)
}
