import type { LancamentoResponse } from "../types"

// Recorte de mes calendario (regra-de-negocio.md item 9) aplicado client-side,
// ja que o endpoint de fluxo-caixa nao aceita periodo como query param. `mes`
// e 1-indexed (Janeiro = 1), mesma convencao usada no resto do dominio (ver
// `mesAtual` em DashboardPage.tsx / ComparativoLimiteGastoPage.tsx).
// `data` chega como DateOnly do backend (string "yyyy-MM-dd") - parse manual
// dos componentes (em vez de `new Date(iso)`) evita o deslocamento de fuso
// que `Date` aplicaria ao interpretar a string como UTC meia-noite, mesmo
// cuidado ja tomado em `formatarData.ts`.
export function filtrarLancamentosDoMes(
  lancamentos: LancamentoResponse[],
  ano: number,
  mes: number,
): LancamentoResponse[] {
  return lancamentos.filter((lancamento) => {
    const [anoLancamento, mesLancamento] = lancamento.data.split("-").map(Number)
    return anoLancamento === ano && mesLancamento === mes
  })
}
