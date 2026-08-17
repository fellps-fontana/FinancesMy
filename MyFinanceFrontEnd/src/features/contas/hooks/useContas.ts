import { useQuery } from "@tanstack/react-query"
import { listarContasBanco, listarContasInvestimento } from "@/features/contas/api"
import { contasKeys } from "@/features/contas/query-keys"
import type { ContaResponse } from "@/features/contas/types"

// Nao ha endpoint que liste contas de TODOS os tipos combinados (o back so
// aceita um `?tipo=` por chamada, ver Controllers/ContasController.cs). Busca
// banco + investimento em paralelo e combina num unico array - mesmo padrao
// ja usado por useContasParaSelecao (features/contas-receber). CARTAO fica
// de fora: tem pagina propria em /cartao e e linha de credito, nao
// patrimonio (ver ESCOPO desta tela).
async function buscarContasBancoEInvestimento(): Promise<ContaResponse[]> {
  const [contasBanco, contasInvestimento] = await Promise.all([
    listarContasBanco(),
    listarContasInvestimento(),
  ])

  return [...contasBanco, ...contasInvestimento]
}

// Estado de servidor desta tela (ver clean-code.md "Organizacao (React)":
// estado de servidor separado de estado de UI, via camada de dados
// dedicada). ContasPage.tsx so consome `data`/`isLoading`/`error` - nenhum
// fetch mora no componente.
export function useContas() {
  return useQuery({
    queryKey: contasKeys.lista(),
    queryFn: buscarContasBancoEInvestimento,
  })
}
