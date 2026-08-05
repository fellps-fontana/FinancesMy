import { validarValorPositivo } from "@/features/investimentos/lib/validarValorPositivo"

// Validacao pura do formulario "Novo ativo" (regra-de-negocio.md item 8.1:
// cadastrar um Ativo E, na pratica, registrar o primeiro aporte dele - o
// formulario pede nome, tipo, instituicao, quantidade, preco unitario e data
// da compra, nunca valor investido direto). Testavel isoladamente do
// componente - ver clean-code.md "Organizacao (React)". O tipo
// (RendaFixa/RendaVariavel) nao precisa de validacao aqui: o formulario so
// permite os dois valores validos via toggle.
export function validarNovoAtivo(
  nome: string,
  instituicao: string,
  quantidade: string,
  precoUnitario: string,
  dataCompra: string,
): string | null {
  if (nome.trim().length === 0) {
    return "Informe o nome do ativo."
  }

  if (instituicao.trim().length === 0) {
    return "Informe a instituicao."
  }

  const erroQuantidade = validarValorPositivo(quantidade, "Informe a quantidade.")
  if (erroQuantidade) {
    return erroQuantidade
  }

  const erroPrecoUnitario = validarValorPositivo(precoUnitario, "Informe o preco unitario.")
  if (erroPrecoUnitario) {
    return erroPrecoUnitario
  }

  if (dataCompra.trim().length === 0) {
    return "Informe a data da compra."
  }

  return null
}
