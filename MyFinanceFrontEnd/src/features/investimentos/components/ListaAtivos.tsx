import { useAtivosDaConta } from "@/features/investimentos/hooks/useAtivosDaConta"
import { calcularValorAtivo } from "@/features/investimentos/lib/calcularValorAtivo"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import type { AtivoResponse } from "@/features/investimentos/types"

// Quantidade nao e valor monetario (formatarMoeda nao serve aqui), mas ainda
// e dado numerico exibido na tela - precisa de locale do projeto (pt-BR), nao
// String(numero) cru. Fracoes de ativo (ETF fracionario) usam ate 8 casas.
const formatadorQuantidade = new Intl.NumberFormat("pt-BR", {
  minimumFractionDigits: 0,
  maximumFractionDigits: 8,
})

type ListaAtivosProps = {
  contaId: string
}

// Container leve: busca os ativos da conta (estado de servidor via
// useAtivosDaConta) e delega a apresentacao pura para AtivoLinha - ver
// clean-code.md "Organizacao (React)" (estado de servidor separado da
// apresentacao, calculo de dominio fora do componente).
export function ListaAtivos({ contaId }: ListaAtivosProps) {
  const { data: ativos, isLoading, error } = useAtivosDaConta(contaId)

  // Log com contexto (qual conta falhou) antes da mensagem generica ao
  // usuario - mesmo padrao de ListaContasInvestimento.tsx (ver clean-code.md
  // "Tratamento de erro").
  if (error) {
    console.error(`Falha ao carregar ativos da conta de investimento - contaId=${contaId}`, error)
  }

  if (error) {
    return (
      <Alert variant="destructive">
        <AlertTitle>Nao foi possivel carregar os ativos</AlertTitle>
        <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
      </Alert>
    )
  }

  if (isLoading) {
    return <p className="text-[13px] text-muted-foreground">Carregando ativos...</p>
  }

  if (!ativos || ativos.length === 0) {
    return (
      <p className="text-[13px] text-muted-foreground">
        Nenhum ativo registrado nesta carteira ainda.
      </p>
    )
  }

  return (
    <ul className="flex flex-col gap-2">
      {ativos.map((ativo) => (
        <AtivoLinha key={ativo.id} ativo={ativo} />
      ))}
    </ul>
  )
}

type AtivoLinhaProps = {
  ativo: AtivoResponse
}

// Apresentacao pura (burra) de um ativo: so exibe dado ja pronto. O valor da
// posicao vem de calcularValorAtivo - nenhum calculo de dominio mora aqui
// (regra-de-negocio.md item 8.1/8.4).
function AtivoLinha({ ativo }: AtivoLinhaProps) {
  const valorAtivo = calcularValorAtivo(ativo)

  return (
    <li className="flex flex-col gap-1.5 rounded-lg border border-border bg-secondary px-3 py-2.5">
      <div className="flex items-center justify-between gap-2">
        <div className="flex flex-col">
          <span className="text-sm font-medium text-secondary-foreground">{ativo.ticker}</span>
          {ativo.nome && (
            <span className="text-[12px] text-muted-foreground">{ativo.nome}</span>
          )}
        </div>
        <span className="text-sm font-medium text-secondary-foreground">
          {formatarMoeda(valorAtivo)}
        </span>
      </div>
      <div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1 text-[12px] text-muted-foreground">
        <span>{formatadorQuantidade.format(ativo.quantidade)} un.</span>
        <span>Preco medio {formatarMoeda(ativo.precoMedio)}</span>
        <span>Preco atual {formatarMoeda(ativo.precoAtual)}</span>
      </div>
    </li>
  )
}
