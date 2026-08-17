// Mascara de moeda do campo "Saldo inicial" (mockup "03 Contas", modal "Nova
// conta"): o input mostra sempre "R$ 0,00" formatado, nunca number cru
// (identidade-visual.md / stack.md "Valores formatados conforme locale do
// projeto"). Padrao "digita da direita pra esquerda" (mesmo comportamento de
// app bancario): cada tecla nova entra nos centavos e empurra o resto pra
// esquerda. Funcao pura e testavel - clean-code.md "Organizacao (React)":
// formatacao/calculo nao vive no componente.
//
// Sinal negativo (regra-de-negocio.md item 10: saldo_manual pode ser
// negativo em toda conta manual, decisao confirmada pelo usuario): a
// presenca de "-" em qualquer posicao do texto digitado liga o sinal
// negativo - o input e texto livre (sem type="number"), entao o usuario
// pode digitar "-" antes ou durante os digitos.
const FORMATADOR_MOEDA_BR = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
})

// Recebe o valor bruto do <input> (pode ja conter a mascara anterior, ex:
// usuario digitando em cima de "R$ 12,30") e devolve o texto ja formatado
// como moeda BR, preservando o sinal negativo se o usuario digitou "-". So
// os digitos (e o sinal) importam - qualquer simbolo de mascara anterior
// (R$, ponto, virgula) e descartado e reconstruido do zero a cada chamada,
// entao editar em qualquer posicao do texto sempre converge pro mesmo
// resultado formatado.
export function aplicarMascaraMoeda(valorDigitado: string): string {
  const negativo = valorDigitado.includes("-")
  const digitos = valorDigitado.replace(/\D/g, "")
  const valorEmCentavos = digitos ? Number(digitos) : 0
  const valorComSinal = negativo && valorEmCentavos !== 0 ? -valorEmCentavos : valorEmCentavos

  return FORMATADOR_MOEDA_BR.format(valorComSinal / 100)
}

// Complementar a aplicarMascaraMoeda: converte o texto ja mascarado de volta
// pro number que a API espera (ex: "-R$ 1.500,00" -> -1500). O form guarda o
// texto mascarado como estado de UI (o que aparece no input); o valor
// numerico so existe na hora de montar o payload de criacao da conta.
export function converterMascaraMoedaParaNumero(valorMascarado: string): number {
  const negativo = valorMascarado.includes("-")
  const digitos = valorMascarado.replace(/\D/g, "")
  const valor = digitos ? Number(digitos) / 100 : 0

  return negativo ? -valor : valor
}
