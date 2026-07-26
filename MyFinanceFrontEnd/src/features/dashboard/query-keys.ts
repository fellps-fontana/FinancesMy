// Chave centralizada para evitar string magica espalhada nos hooks da
// feature dashboard, mesmo padrao de contasReceberKeys.
export const dashboardKeys = {
  all: ["dashboard"] as const,
  projecaoMes: (ano: number, mes: number) =>
    [...dashboardKeys.all, "projecaoMes", ano, mes] as const,
}
