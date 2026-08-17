import { ArrowLeftRight } from "lucide-react"
import { Card, CardContent } from "@/shared/ui/card"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarData } from "@/features/cartao/lib/formatarData"
import { useContasParaExibicaoTransferencia } from "@/features/lancamentos/hooks/useContasParaExibicaoTransferencia"
import { montarDescricaoTransferencia } from "@/features/lancamentos/lib/montarDescricaoTransferencia"
import type { TransferenciaFluxoCaixa } from "@/features/lancamentos/types"

type TransferenciaFluxoCaixaItemProps = {
  transferencia: TransferenciaFluxoCaixa
}

// Componente de apresentacao (burro): a decisao de titulo/subtitulo
// (Pagamento de fatura / Emprestimo / "Conta A -> Conta B") vem inteira de
// montarDescricaoTransferencia (lib/, funcao pura testavel) - este componente
// so busca o lookup de contas (useContasParaExibicaoTransferencia, banco +
// investimento + cartao) e renderiza.
//
// Linha visualmente distinta de LancamentoItem (regra-de-negocio.md item 3:
// "no fluxo de caixa a transferencia aparece como uma unica linha logica"):
// icone de troca (ArrowLeftRight, nao a seta unica de entrada/saida de
// LancamentoItem) e valor SEM sinal +/- nem cor positivo/negativo -
// transferencia nao e gasto nem receita (item 2 CRITICA + item 3), entao nao
// herda a semantica de cor de entrada/saida da identidade visual. SEM acoes
// (editar/pagar/remover): a regra de negocio nao define nenhuma acao de
// edicao/exclusao sobre transferencia hoje.
export function TransferenciaFluxoCaixaItem({ transferencia }: TransferenciaFluxoCaixaItemProps) {
  const { data: contas } = useContasParaExibicaoTransferencia()
  const { titulo, subtitulo } = montarDescricaoTransferencia(transferencia, contas)

  return (
    <Card size="sm">
      <CardContent className="flex items-center gap-3">
        <div className="flex size-[34px] shrink-0 items-center justify-center rounded-lg bg-accent-deep">
          <ArrowLeftRight className="size-4 text-accent-soft" strokeWidth={1.6} aria-hidden="true" />
        </div>

        <div className="flex min-w-0 flex-1 flex-col">
          <span className="truncate text-[14px] text-text-body">{titulo}</span>
          {subtitulo && <span className="truncate text-[12px] text-text-faint">{subtitulo}</span>}
          <span className="text-[12px] text-text-faint">{formatarData(transferencia.data)}</span>
        </div>

        <span className="shrink-0 text-[14px] font-medium text-text-primary">
          {formatarMoeda(transferencia.valor)}
        </span>
      </CardContent>
    </Card>
  )
}
