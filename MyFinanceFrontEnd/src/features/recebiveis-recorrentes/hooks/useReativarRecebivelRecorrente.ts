import { useMutation, useQueryClient } from "@tanstack/react-query"
import { reativarRecebivelRecorrente } from "@/features/recebiveis-recorrentes/api"
import { recebiveisRecorrentesKeys } from "@/features/recebiveis-recorrentes/query-keys"
// Ver useCriarRecebivelRecorrente para o motivo do import cross-feature das
// chaves de contas-receber.
import { contasReceberKeys } from "@/features/contas-receber/query-keys"

// Reativar volta a materializar as ocorrencias do molde (item 15) - novas
// linhas entram na lista de contas a receber e no total esperado do mes.
export function useReativarRecebivelRecorrente() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => reativarRecebivelRecorrente(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: recebiveisRecorrentesKeys.lista() })
      queryClient.invalidateQueries({ queryKey: recebiveisRecorrentesKeys.porId(id) })
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.lista() })
      queryClient.invalidateQueries({ queryKey: [...contasReceberKeys.all, "totalEsperadoMes"] })
    },
  })
}
