import { converterValorParaNumero } from "@/features/investimentos/lib/validarValorPositivo"

// Total do primeiro aporte = quantidade x preco unitario (regra 8.1). So para
// previa no formulario: o backend recalcula e e a fonte da verdade. Retorna
// null enquanto algum dos campos nao for um numero positivo.
export function calcularTotalNovoAtivo(quantidade: string, precoUnitario: string): number | null {
  if (quantidade.trim().length === 0 || precoUnitario.trim().length === 0) return null

  const qtd = converterValorParaNumero(quantidade)
  const preco = converterValorParaNumero(precoUnitario)
  if (Number.isNaN(qtd) || Number.isNaN(preco) || qtd <= 0 || preco <= 0) return null

  return qtd * preco
}
