import { useQuery } from "@tanstack/react-query"
import { apiClient } from "@/shared/api/client"
import { contasKeys } from "@/features/contas/query-keys"
import type { ContaResponse } from "@/features/contas/types"

// Nao ha endpoint que liste contas de TODOS os tipos combinados (o back so
// aceita um `?tipo=` por chamada). Busca banco + investimento em paralelo e
// combina os resultados num unico array. CARTAO fica de fora: cartao e linha
// de credito, nao fonte/destino de fundos para emprestimo ou recebimento
// (decisao pragmatica de UX, nao regra de negocio formal do item 13).
async function buscarContasParaSelecao(): Promise<ContaResponse[]> {
  const [contasBanco, contasInvestimento] = await Promise.all([
    apiClient.get<ContaResponse[]>("/api/contas?tipo=banco"),
    apiClient.get<ContaResponse[]>("/api/contas?tipo=investimento"),
  ])

  return [...contasBanco, ...contasInvestimento]
}

type UseContasParaSelecaoOptions = {
  // FormRegistrarContaReceber so precisa da lista quando tipo === "EMPRESTIMO"
  // (RECEBIVEL nao tem conta de origem, item 13); FormRegistrarRecebimento
  // sempre precisa (contaDestinoId e sempre obrigatorio). Default `true`
  // cobre o segundo caso sem exigir que todo chamador passe a opcao.
  enabled?: boolean
}

// Hook compartilhado pelos formularios que escolhem conta de origem/destino:
// FormRegistrarContaReceber (origem do emprestimo), FormRegistrarRecebimento
// (destino do recebimento), FormContaFixa (origem da conta fixa), FormLancamento
// e FormTransferencia - mesma chamada combinada, extraida pra nao duplicar a
// logica de buscarContasParaSelecao.
export function useContasParaSelecao(options: UseContasParaSelecaoOptions = {}) {
  const { enabled = true } = options

  return useQuery({
    queryKey: contasKeys.contasParaSelecao(),
    queryFn: buscarContasParaSelecao,
    enabled,
  })
}
