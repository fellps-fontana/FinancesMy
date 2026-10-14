import type { ContaResponse } from "@/features/contas/types"

// Patrimonio total = saldos das contas Banco + totalInvestido do backend
// (GET /api/contas/investimentos/total, regra item 8). Contas de investimento
// e ativos NAO sao somados aqui: o total de investimentos ja os consolida, e
// somar de novo contaria o mesmo dinheiro duas vezes. Cartao fica fora
// (linha de credito, nao patrimonio). `saldo` ja vem resolvido pelo backend
// (regra item 10); aqui e so agregacao de exibicao. Funcao pura e testavel.
export function calcularPatrimonioTotal(contas: ContaResponse[], totalInvestido = 0): number {
  const saldoBancos = contas
    .filter((conta) => conta.tipo === "Banco")
    .reduce((total, conta) => total + conta.saldo, 0)

  return saldoBancos + totalInvestido
}
