import { formatarMoeda } from "@/shared/lib/formatarMoeda"

export type ResumoParcelamento = {
  texto: string
  valido: boolean
}

const TEXTO_INVALIDO = "Informe o valor e 2 ou mais parcelas"

// Previa visual apenas: o backend e a fonte de verdade do valor de cada
// parcela (item 17: valor_total / N, resto na ultima). Trabalha em centavos
// para o resto nao vazar ponto flutuante.
export function montarResumoParcelamento(
  valorTotal: number | null,
  quantidadeParcelas: number,
): ResumoParcelamento {
  const parcelasValidas = Number.isInteger(quantidadeParcelas) && quantidadeParcelas >= 2
  if (valorTotal === null || valorTotal <= 0 || !parcelasValidas) {
    return { texto: TEXTO_INVALIDO, valido: false }
  }

  const totalCentavos = Math.round(valorTotal * 100)
  const baseCentavos = Math.floor(totalCentavos / quantidadeParcelas)
  const ultimaCentavos = totalCentavos - baseCentavos * (quantidadeParcelas - 1)

  if (ultimaCentavos === baseCentavos) {
    return { texto: `${quantidadeParcelas}x de ${formatarMoeda(baseCentavos / 100)}`, valido: true }
  }

  return {
    texto: `${quantidadeParcelas - 1}x de ${formatarMoeda(baseCentavos / 100)} + 1x de ${formatarMoeda(ultimaCentavos / 100)}`,
    valido: true,
  }
}
