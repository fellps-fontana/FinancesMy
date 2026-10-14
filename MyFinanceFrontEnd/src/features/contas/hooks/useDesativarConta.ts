import { useMutation, useQueryClient } from "@tanstack/react-query"
import { desativarConta, tipificarErroConta } from "@/features/contas/api"
import { contasKeys } from "@/features/contas/query-keys"
import { investimentosKeys } from "@/features/investimentos/query-keys"

// Mutation de desativacao de conta (soft-delete, regra-de-negocio.md item
// 8.3, mesmo principio aplicado a Conta). Invalida a lista desta feature -
// a conta desativada some da tela assim que o cache e revalidado.
export function useDesativarConta() {
  const queryClient = useQueryClient()

  return useMutation({
    // 409 { erro }: a conta tem ativo ativo vinculado e nao pode ser desativada.
    mutationFn: (id: string) =>
      desativarConta(id).catch((erro: unknown) => {
        throw tipificarErroConta(erro, "CONTA_COM_ATIVO_ATIVO", 409)
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contasKeys.lista() })
      queryClient.invalidateQueries({ queryKey: investimentosKeys.total() })
    },
  })
}
