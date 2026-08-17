import type { SubtipoConta, TipoConta, TipoContaFormulario } from "@/features/contas/types"

// Opcoes do dropdown "Tipo de conta" do formulario de nova conta
// (regra-de-negocio.md item 10): Conta tipo Banco tem Subtipo (Corrente/
// Poupanca/DinheiroFisico); Conta tipo Investimento nunca tem Subtipo (fica
// null no payload, ver DTOs/Conta/CriarContaRequest.cs e ContaService.
// CriarContaAsync). Mapeamento puro e testavel - o componente de formulario
// so exibe a selecao, nao decide o par (tipo, subtipo) enviado ao back
// (clean-code.md "Organizacao (React)": calculo/mapeamento de dominio nao
// vive no componente).
const MAPA_TIPO_FORMULARIO: Record<
  TipoContaFormulario,
  { tipo: TipoConta; subtipo: SubtipoConta | undefined }
> = {
  CONTA_CORRENTE: { tipo: "Banco", subtipo: "Corrente" },
  POUPANCA: { tipo: "Banco", subtipo: "Poupanca" },
  DINHEIRO_FISICO: { tipo: "Banco", subtipo: "DinheiroFisico" },
  INVESTIMENTO: { tipo: "Investimento", subtipo: undefined },
}

export function obterTipoESubtipoConta(
  selecao: TipoContaFormulario,
): { tipo: TipoConta; subtipo: SubtipoConta | undefined } {
  return MAPA_TIPO_FORMULARIO[selecao]
}
