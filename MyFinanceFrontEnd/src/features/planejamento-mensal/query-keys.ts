// `all` permite invalidar todos os meses de uma vez; `plano` e a chave de um
// mes especifico.
export const planejamentoMensalKeys = {
  all: ["planejamentoMensal"] as const,
  plano: (ano: number, mes: number) =>
    [...planejamentoMensalKeys.all, "plano", ano, mes] as const,
}
