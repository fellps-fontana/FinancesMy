// Validacao pura do formulario de AssinaturaCartao. Mesmo espirito de
// features/contas-fixas/lib/validarContaFixa.ts, mas sem periodicidade
// (assinatura de cartao e sempre mensal) e com diaReferencia no lugar de
// diaVencimento (AssinaturaCartaoService.ValidarDadosEntrada, backend:
// 1-31, mesmo range).

const DIA_REFERENCIA_MIN = 1
const DIA_REFERENCIA_MAX = 31

function validarValor(valor: string): string | null {
  const valorNormalizado = valor.trim().replace(",", ".")

  if (valorNormalizado.length === 0) {
    return "Informe o valor."
  }

  const valorNumerico = Number(valorNormalizado)

  if (Number.isNaN(valorNumerico)) {
    return "Informe um valor valido."
  }

  if (valorNumerico <= 0) {
    return "O valor deve ser maior que zero."
  }

  return null
}

export function converterValorParaNumero(valorBruto: string): number {
  return Number(valorBruto.trim().replace(",", "."))
}

function validarDiaReferencia(diaReferencia: string): string | null {
  if (diaReferencia.trim().length === 0) {
    return "Informe o dia de referencia."
  }

  const dia = Number(diaReferencia)

  if (!Number.isInteger(dia) || dia < DIA_REFERENCIA_MIN || dia > DIA_REFERENCIA_MAX) {
    return `O dia de referencia deve ser um numero entre ${DIA_REFERENCIA_MIN} e ${DIA_REFERENCIA_MAX}.`
  }

  return null
}

export function converterDiaReferenciaParaNumero(diaReferenciaBruto: string): number {
  return Number(diaReferenciaBruto)
}

// CRIAR e EDITAR compartilham o mesmo conjunto de campos obrigatorios
// (descricao, valor, diaReferencia) - diferente de ContaFixa, aqui
// EditarAssinaturaCartaoRequest tambem exige Descricao (DTOs/AssinaturaCartao/
// EditarAssinaturaCartaoRequest.cs), entao nao ha uma validacao "reduzida"
// separada para edicao.
export function validarAssinaturaCartao(
  descricao: string,
  valor: string,
  diaReferencia: string,
): string | null {
  if (descricao.trim().length === 0) {
    return "Informe uma descricao."
  }

  const erroValor = validarValor(valor)
  if (erroValor) {
    return erroValor
  }

  return validarDiaReferencia(diaReferencia)
}
