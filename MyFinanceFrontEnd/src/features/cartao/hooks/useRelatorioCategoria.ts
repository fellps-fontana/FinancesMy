import { useQuery } from "@tanstack/react-query"
import { obterRelatorioCategoria } from "@/features/cartao/api"
import { cartaoKeys } from "@/features/cartao/query-keys"

// GAP CONHECIDO: o backend nao tem endpoint de relatorio ainda (ver
// api.ts/obterRelatorioCategoria) - esta query fica em erro ate ele existir;
// a tela trata isError mostrando o aviso, sem dado inventado.
export function useRelatorioCategoria(mes: string, contaId?: string) {
  return useQuery({
    queryKey: cartaoKeys.relatorioCategoria(mes, contaId),
    queryFn: () => obterRelatorioCategoria(mes, contaId),
  })
}
