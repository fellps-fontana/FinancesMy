import { useMutation, useQueryClient } from "@tanstack/react-query"
import { desativarRecebivelRecorrente } from "@/features/recebiveis-recorrentes/api"
import { recebiveisRecorrentesKeys } from "@/features/recebiveis-recorrentes/query-keys"
// Ver useCriarRecebivelRecorrente para o motivo do import cross-feature das
// chaves de contas-receber.
import { contasReceberKeys } from "@/features/contas-receber/query-keys"

// Desativar remove as ocorrencias ainda PENDENTE do molde (item 15) - isso
// tira linhas da lista de contas a receber e reduz o total esperado do mes.
export function useDesativarRecebivelRecorrente() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => desativarRecebivelRecorrente(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: recebiveisRecorrentesKeys.lista() })
      queryClient.invalidateQueries({ queryKey: recebiveisRecorrentesKeys.porId(id) })
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.lista() })
      queryClient.invalidateQueries({ queryKey: [...contasReceberKeys.all, "totalEsperadoMes"] })
    },
  })
}
