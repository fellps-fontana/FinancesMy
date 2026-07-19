import { useMutation, useQueryClient } from "@tanstack/react-query"
import { registrarRecebimento } from "@/features/contas-receber/api"
import { contasReceberKeys } from "@/features/contas-receber/query-keys"
import type { RegistrarRecebimentoRequest } from "@/features/contas-receber/types"

type RegistrarRecebimentoVariables = {
  contaReceberId: string
  request: RegistrarRecebimentoRequest
}

// Recebimento muda o saldo_pendente (e o status derivado dele, item 13) da
// conta receber, o que tambem muda o total esperado do mes (item 9) - por
// isso invalida a lista, o registro pontual e o total esperado.
export function useRegistrarRecebimento() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ contaReceberId, request }: RegistrarRecebimentoVariables) =>
      registrarRecebimento(contaReceberId, request),
    onSuccess: (_data, { contaReceberId }) => {
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.lista() })
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.porId(contaReceberId) })
      queryClient.invalidateQueries({ queryKey: [...contasReceberKeys.all, "totalEsperadoMes"] })
    },
  })
}
