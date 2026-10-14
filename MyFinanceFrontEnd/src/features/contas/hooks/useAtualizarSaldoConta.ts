import { useMutation, useQueryClient } from "@tanstack/react-query"
import { atualizarSaldoConta, tipificarErroConta } from "@/features/contas/api"
import { contasKeys } from "@/features/contas/query-keys"
import { investimentosKeys } from "@/features/investimentos/query-keys"
import type { AtualizarSaldoRequest } from "@/features/contas/types"

type AtualizarSaldoVariables = {
  id: string
  request: AtualizarSaldoRequest
}

// Mutation de edicao de saldo_manual (regra-de-negocio.md item 10). Invalida
// a lista combinada banco+investimento desta feature - o patrimonio total
// (lib/calcularPatrimonioTotal.ts) e derivado dela em ContasPage.tsx, entao
// recalcula sozinho apos a invalidacao, sem logica de dominio no componente.
export function useAtualizarSaldoConta() {
  const queryClient = useQueryClient()

  return useMutation({
    // 422 { erro }: saldo rejeitado pelo backend.
    mutationFn: ({ id, request }: AtualizarSaldoVariables) =>
      atualizarSaldoConta(id, request).catch((erro: unknown) => {
        throw tipificarErroConta(erro, "SALDO_INVALIDO", 422)
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contasKeys.lista() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.total() })
    },
  })
}
