import type { AtivoResponse, TipoAtivo } from "@/features/investimentos/types"

export type FiltroTipoAtivo = "Todos" | TipoAtivo

// Filtra a lista de ativos pelo filtro Todos/Renda fixa/Renda variavel
// (mockup "11 Investimentos"). Pura e testavel isolada - nenhum estado de UI
// aqui, so decide quais itens aparecem na lista.
export function filtrarAtivosPorTipo(
  ativos: AtivoResponse[],
  filtro: FiltroTipoAtivo,
): AtivoResponse[] {
  if (filtro === "Todos") {
    return ativos
  }

  return ativos.filter((ativo) => ativo.tipo === filtro)
}
