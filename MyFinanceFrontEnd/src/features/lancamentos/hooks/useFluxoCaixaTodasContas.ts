import { useQuery } from "@tanstack/react-query"
import { listarFluxoCaixaTodasContas } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"

// Fluxo de caixa (CAIXA) agregado de TODAS as contas - mesma visao do
// regra-de-negocio.md item 12 ("Lancamento geral / fluxo de caixa"), agora
// sem exigir selecao previa de conta (LancamentosPage.tsx abre ja mostrando
// os itens). Cada linha ja chega classificada como LANCAMENTO ou
// TRANSFERENCIA (types.ts, FluxoCaixaItem) - o front nao decide o tipo.
export function useFluxoCaixaTodasContas() {
  return useQuery({
    queryKey: lancamentosKeys.fluxoCaixaTodasContas(),
    queryFn: listarFluxoCaixaTodasContas,
  })
}
