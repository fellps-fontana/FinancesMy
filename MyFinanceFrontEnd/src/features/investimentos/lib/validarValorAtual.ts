import { validarValorPositivo } from "@/features/investimentos/lib/validarValorPositivo"

// Validacao pura do campo "Valor atual" na edicao de um ativo
// (regra-de-negocio.md item 8.1: valorAtual e 100% manual, editado pelo
// usuario). Reaproveita a mesma regra de "estritamente positivo" da criacao.
export function validarValorAtual(valorAtual: string): string | null {
  return validarValorPositivo(valorAtual, "Informe o valor atual.")
}
