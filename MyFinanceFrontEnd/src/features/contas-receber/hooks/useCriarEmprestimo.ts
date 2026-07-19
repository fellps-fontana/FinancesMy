import { useMutation, useQueryClient } from "@tanstack/react-query"
import { criarEmprestimo } from "@/features/contas-receber/api"
import { contasReceberKeys } from "@/features/contas-receber/query-keys"
import type { CriarEmprestimoRequest } from "@/features/contas-receber/types"

// Emprestimo tambem pode entrar na projecao do mes corrente (item 9) e, alem
// disso, debita a conta de origem via transferencia de perna unica (item 13)
// - mesma invalidacao de useCriarRecebivel.
export function useCriarEmprestimo() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CriarEmprestimoRequest) => criarEmprestimo(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.lista() })
      queryClient.invalidateQueries({ queryKey: [...contasReceberKeys.all, "totalEsperadoMes"] })
    },
  })
}
