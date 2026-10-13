export const relatoriosKeys = {
  all: ["relatorios"] as const,
  gastoPorCategoria: (ano: number, mes: number) =>
    [...relatoriosKeys.all, "gastoPorCategoria", ano, mes] as const,
  // categoriaId null = "Sem categoria"
  categoriaDetalhe: (ano: number, mes: number, categoriaId: string | null) =>
    [...relatoriosKeys.all, "categoriaDetalhe", ano, mes, categoriaId] as const,
}
