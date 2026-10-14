import { ArrowLeftRight } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { formatarDiaMes } from "@/shared/lib/datas"
import { GRADE_COLUNAS_LANCAMENTO } from "@/features/lancamentos/components/LancamentoItem"
import { useContasParaExibicaoTransferencia } from "@/features/lancamentos/hooks/useContasParaExibicaoTransferencia"
import { montarDescricaoTransferencia } from "@/features/lancamentos/lib/montarDescricaoTransferencia"
import type { TransferenciaFluxoCaixa } from "@/features/lancamentos/types"

type TransferenciaFluxoCaixaItemProps = {
  transferencia: TransferenciaFluxoCaixa
}

// Linha de transferencia na mesma grade da tabela (apresentacao). Titulo e
// subtitulo vem de montarDescricaoTransferencia (lib). Valor sem sinal nem
// cor de entrada/saida: transferencia nao e gasto nem receita (itens 2, 3 e
// 12). Sem acoes: a regra nao define edicao/exclusao de transferencia.
export function TransferenciaFluxoCaixaItem({ transferencia }: TransferenciaFluxoCaixaItemProps) {
  const { data: contas } = useContasParaExibicaoTransferencia()
  const { titulo, subtitulo } = montarDescricaoTransferencia(transferencia, contas)

  return (
    <li className="md:border-b-[0.5px] md:border-border md:last:border-b-0 py-1.5 md:px-[22px] md:py-3.5">
      <div className={cn("flex items-center gap-3 md:grid", GRADE_COLUNAS_LANCAMENTO)}>
        <div className="flex min-w-0 flex-1 items-center gap-3">
          <div className="flex size-[34px] shrink-0 items-center justify-center rounded-[10px] bg-accent-deep">
            <ArrowLeftRight className="size-4 text-accent-soft" strokeWidth={1.6} aria-hidden="true" />
          </div>
          <div className="flex min-w-0 flex-col">
            <span className="truncate text-sm text-text-body">{titulo}</span>
            {subtitulo && <span className="truncate text-xs text-text-faint md:hidden">{subtitulo}</span>}
          </div>
        </div>

        <span className="hidden truncate text-[13px] text-text-muted md:block">
          {subtitulo ?? "Transferencia"}
        </span>
        <span className="hidden text-[13px] text-text-muted md:block">
          {formatarDiaMes(transferencia.data)}
        </span>
        <span className="text-sm text-text-primary md:text-right">
          {formatarMoeda(transferencia.valor)}
        </span>
        <span className="hidden md:block" />
      </div>
    </li>
  )
}
