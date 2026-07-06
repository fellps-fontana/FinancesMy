import { validarSaldo } from "@/features/investimentos/lib/validarSaldo"

// Validacao pura do formulario de criacao de conta de investimento manual
// (ver regra-de-negocio.md secao 8: cofrinho/XP/carteira de acoes entram como
// conta manual, origem sempre implicita). Testavel isoladamente do componente
// - ver clean-code.md "Organizacao (React)".
export function validarNovaConta(nome: string, saldoInicial: string): string | null {
  if (nome.trim().length === 0) {
    return "Informe um nome para a conta."
  }

  return validarSaldo(saldoInicial)
}
