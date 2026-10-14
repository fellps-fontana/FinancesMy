import { cn } from "@/shared/lib/utils"
import { AtivoLinha, GRADE_COLUNAS_ATIVO } from "@/features/investimentos/components/AtivoLinha"
import type { AtivoResponse } from "@/features/investimentos/types"

type TabelaAtivosProps = {
  ativos: AtivoResponse[]
  /** Mensagem quando a lista filtrada esta vazia. */
  mensagemVazia: string
  onVincularConta: (ativo: AtivoResponse) => void
  onAbrirAcoes: (ativo: AtivoResponse) => void
  className?: string
}

// Tabela de ativos no desktop (card com cabecalho) e lista de cards no
// mobile, como em s_investimentos.
export function TabelaAtivos({
  ativos,
  mensagemVazia,
  onVincularConta,
  onAbrirAcoes,
  className,
}: TabelaAtivosProps) {
  if (ativos.length === 0) {
    return <p className="text-sm text-text-muted">{mensagemVazia}</p>
  }

  return (
    <div
      className={cn(
        "min-w-0 md:overflow-hidden md:rounded-2xl md:border-[0.5px] md:border-border md:bg-card",
        className,
      )}
    >
      <div
        className={cn(
          "hidden border-b-[0.5px] border-border px-[22px] py-3.5 text-xs text-text-muted md:grid",
          GRADE_COLUNAS_ATIVO,
        )}
      >
        <span>Ativo</span>
        <span>Tipo</span>
        <span>Conta</span>
        <span className="text-right">Valor</span>
        <span className="text-right">Evolucao</span>
        <span />
      </div>
      <ul className="flex flex-col gap-2.5 md:block">
        {ativos.map((ativo) => (
          <AtivoLinha
            key={ativo.id}
            ativo={ativo}
            onVincularConta={onVincularConta}
            onAbrirAcoes={onAbrirAcoes}
          />
        ))}
      </ul>
    </div>
  )
}
