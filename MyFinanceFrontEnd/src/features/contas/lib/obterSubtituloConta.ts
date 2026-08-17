import type { ContaResponse } from "@/features/contas/types"

const SUBTITULO_POR_SUBTIPO: Record<NonNullable<ContaResponse["subtipo"]>, string> = {
  Corrente: "Conta corrente",
  Poupanca: "Poupança",
  DinheiroFisico: "Dinheiro físico",
}

// Funcao pura e testavel (clean-code.md "Organizacao (React)": calculo de
// exibicao nao vive no componente). Investimento (saldo simples, regra-de-
// negocio.md item 8) nao carrega Subtipo - so Conta tipo Banco tem essa
// classificacao (item 10) - por isso o tipo decide antes do subtipo.
// Conta Banco sem subtipo cadastrado (registro legado) cai no rotulo
// generico em vez de mostrar campo vazio/null cru na tela.
export function obterSubtituloConta(
  conta: Pick<ContaResponse, "tipo" | "subtipo">,
): string {
  if (conta.tipo === "Investimento") {
    return "Investimento"
  }

  if (conta.subtipo) {
    return SUBTITULO_POR_SUBTIPO[conta.subtipo]
  }

  return "Conta bancária"
}
