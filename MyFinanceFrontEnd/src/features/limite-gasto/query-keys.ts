// Chaves centralizadas para evitar string magica espalhada nos hooks.
// `all` cobre lista E gasto-vs-limite de proposito: definir/remover um
// limite muda o valor_limite, que muda TODO calculo de gasto-vs-limite em
// cache (percentualUtilizado, estourado) - invalidar `all` inteiro apos
// mutation garante que nenhuma tela fique mostrando comparativo desatualizado
// (regra-de-negocio.md item 14).
export const limiteGastoKeys = {
  all: ["limiteGasto"] as const,
  lista: () => [...limiteGastoKeys.all, "lista"] as const,
  gastoVsLimiteTodas: (ano: number, mes: number) =>
    [...limiteGastoKeys.all, "gastoVsLimite", ano, mes] as const,
  gastoVsLimitePorCategoria: (categoriaId: string, ano: number, mes: number) =>
    [...limiteGastoKeys.all, "gastoVsLimite", categoriaId, ano, mes] as const,
}
