import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/shared/api/client"
import { lancamentosKeys } from "@/features/lancamentos/query-keys"
import type { ContaParaExibicao } from "@/features/lancamentos/types"

// Diferente de useContasParaSelecao (features/contas-receber/hooks), que
// exclui cartao de proposito (cartao nao e origem/destino de emprestimo ou
// recebimento) - aqui o cartao PRECISA aparecer, porque pagamento de fatura
// (regra-de-negocio.md item 12) tem destino = conta CARTAO, e a UI de
// transferencia (TransferenciaFluxoCaixaItem.tsx) precisa resolver esse nome
// tambem. Busca banco + investimento + cartao em paralelo (mesmo padrao dos
// demais hooks de selecao combinada do projeto, ja que GET /api/contas so
// aceita um `?tipo=` por chamada).
async function buscarContasParaExibicaoTransferencia(): Promise<ContaParaExibicao[]> {
  const [contasBanco, contasInvestimento, contasCartao] = await Promise.all([
    apiClient.get<ContaParaExibicao[]>("/api/contas?tipo=banco"),
    apiClient.get<ContaParaExibicao[]>("/api/contas?tipo=investimento"),
    apiClient.get<ContaParaExibicao[]>("/api/contas?tipo=cartao"),
  ])

  return [...contasBanco, ...contasInvestimento, ...contasCartao]
}

export function useContasParaExibicaoTransferencia() {
  return useQuery({
    queryKey: lancamentosKeys.contasParaExibicaoTransferencia(),
    queryFn: buscarContasParaExibicaoTransferencia,
  })
}
