import { useQuery } from "@tanstack/react-query"
import { listarRecebiveisRecorrentes } from "@/features/recebiveis-recorrentes/api"
import { recebiveisRecorrentesKeys } from "@/features/recebiveis-recorrentes/query-keys"

// Estado de servidor isolado do componente (clean-code.md "Organizacao
// (React)"). Sem filtro: a tela lista moldes ativos e inativos juntos,
// distinguindo pelo badge de status (item 15).
export function useRecebiveisRecorrentes(ativa?: boolean) {
  return useQuery({
    queryKey: recebiveisRecorrentesKeys.lista(ativa),
    queryFn: () => listarRecebiveisRecorrentes(ativa),
  })
}
