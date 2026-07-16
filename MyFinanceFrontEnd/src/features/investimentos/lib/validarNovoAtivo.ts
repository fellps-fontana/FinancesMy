import { validarValorPositivo } from "@/features/investimentos/lib/validarValorPositivo"

// Validacao pura do formulario "Novo ativo" (regra-de-negocio.md item 8: nome,
// tipo, instituicao, valor investido e data da compra sao os campos do
// cadastro). Testavel isoladamente do componente - ver clean-code.md
// "Organizacao (React)". O tipo (RendaFixa/RendaVariavel) nao precisa de
// validacao aqui: o formulario so permite os dois valores validos via toggle.
export function validarNovoAtivo(
  nome: string,
  instituicao: string,
  valorInvestido: string,
  dataCompra: string,
): string | null {
  if (nome.trim().length === 0) {
    return "Informe o nome do ativo."
  }

  if (instituicao.trim().length === 0) {
    return "Informe a instituicao."
  }

  const erroValor = validarValorPositivo(valorInvestido, "Informe o valor investido.")
  if (erroValor) {
    return erroValor
  }

  if (dataCompra.trim().length === 0) {
    return "Informe a data da compra."
  }

  return null
}
