import { useMutation, useQueryClient } from "@tanstack/react-query"
import { editarRecebivelRecorrente } from "@/features/recebiveis-recorrentes/api"
import { recebiveisRecorrentesKeys } from "@/features/recebiveis-recorrentes/query-keys"
// Ver useCriarRecebivelRecorrente para o motivo do import cross-feature das
// chaves de contas-receber.
import { contasReceberKeys } from "@/features/contas-receber/query-keys"
import type { EditarRecebivelRecorrenteRequest } from "@/features/recebiveis-recorrentes/types"

type EditarRecebivelRecorrenteVariables = {
  id: string
  request: EditarRecebivelRecorrenteRequest
}

// Editar valor/categoria propaga para as ocorrencias PENDENTE; mudar a
// ancora (periodicidade/dia/mes/dia-da-semana) regenera o conjunto de
// ocorrencias (item 15). Nos dois casos a lista de contas a receber e o
// total esperado do mes mudam - por isso a invalidacao cruzada.
export function useEditarRecebivelRecorrente() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: EditarRecebivelRecorrenteVariables) =>
      editarRecebivelRecorrente(id, request),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: recebiveisRecorrentesKeys.lista() })
      queryClient.invalidateQueries({ queryKey: recebiveisRecorrentesKeys.porId(id) })
      queryClient.invalidateQueries({ queryKey: contasReceberKeys.lista() })
      queryClient.invalidateQueries({ queryKey: [...contasReceberKeys.all, "totalEsperadoMes"] })
    },
  })
}
