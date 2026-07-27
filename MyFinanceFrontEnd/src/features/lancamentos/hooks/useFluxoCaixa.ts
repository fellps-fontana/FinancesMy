import { useQuery } from "@tanstack/react-query"
import { listarFluxoCaixa } from "@/features/lancamentos/api"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"

// Fluxo de caixa (CAIXA) de uma conta - mesma visao do regra-de-negocio.md
// item 12 ("Lancamento geral / fluxo de caixa"): mostra o pagamento da
// fatura como saida real e nao lista as compras individuais do cartao.
export function useFluxoCaixa(contaId: string) {
  return useQuery({
    queryKey: lancamentosKeys.fluxoCaixa(contaId),
    queryFn: () => listarFluxoCaixa(contaId),
  })
}
