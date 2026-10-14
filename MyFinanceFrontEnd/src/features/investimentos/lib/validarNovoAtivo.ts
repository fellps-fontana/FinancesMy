import { validarValorPositivo } from "@/features/investimentos/lib/validarValorPositivo"

// Validacao pura do formulario "Novo ativo" (regra-de-negocio.md item 8.1:
// cadastrar um Ativo E registrar o primeiro aporte - quantidade x preco
// unitario, nunca valor investido direto; item 8.5: conta de investimento
// obrigatoria). O tipo nao precisa de validacao: o toggle so permite os dois
// valores validos.
export function validarNovoAtivo(
  nome: string,
  contaId: string,
  quantidade: string,
  precoUnitario: string,
  dataCompra: string,
): string | null {
  if (nome.trim().length === 0) {
    return "Informe o nome do ativo."
  }

  if (contaId.trim().length === 0) {
    return "Selecione a conta de investimento."
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
