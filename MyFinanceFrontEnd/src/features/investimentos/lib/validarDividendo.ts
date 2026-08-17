import { validarValorPositivo } from "@/features/investimentos/lib/validarValorPositivo"

// Validacao pura do formulario "Registrar dividendo" (regra-de-negocio.md
// item 8.4: DIVIDENDO e sempre cadastro MANUAL do usuario, com `valor` > 0 e
// `data`, vinculado a um Ativo especifico). Tipo/origem NAO sao campos do
// formulario - o back decide DIVIDENDO/MANUAL sempre (ver briefing desta
// tarefa) - por isso so ha os dois campos abaixo para validar. Testavel
// isolada do componente - clean-code.md "Organizacao (React)".
export function validarDividendo(valor: string, data: string): string | null {
  const erroValor = validarValorPositivo(valor, "Informe o valor do dividendo.")
  if (erroValor) {
    return erroValor
  }

  if (data.trim().length === 0) {
    return "Informe a data do dividendo."
  }

  return null
}
