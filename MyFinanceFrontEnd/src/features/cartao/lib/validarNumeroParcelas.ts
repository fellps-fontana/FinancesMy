// Validacao/parse puro do campo opcional de parcelas do formulario de compra
// (regra de negocio item 12, subsecao "Parcelamento"). Em branco = compra a
// vista (numeroParcelas undefined). Preenchido, precisa ser um inteiro >= 2 -
// o proprio backend tambem valida isso, mas a UI evita a chamada
// desnecessaria.
export type ResultadoValidacaoParcelas = {
  erro: string | null
  numeroParcelas: number | undefined
}

export function validarNumeroParcelas(numeroParcelasTexto: string): ResultadoValidacaoParcelas {
  const parcelasTexto = numeroParcelasTexto.trim()
  if (parcelasTexto.length === 0) {
    return { erro: null, numeroParcelas: undefined }
  }

  const parcelasNumero = Number(parcelasTexto)
  if (!Number.isInteger(parcelasNumero) || parcelasNumero < 2) {
    return {
      erro: "Numero de parcelas deve ser um numero inteiro maior ou igual a 2.",
      numeroParcelas: undefined,
    }
  }

  return { erro: null, numeroParcelas: parcelasNumero }
}
