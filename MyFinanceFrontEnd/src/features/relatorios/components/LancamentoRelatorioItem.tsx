import { cn } from "@/shared/lib/utils"
import { Badge } from "@/shared/ui/badge"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import type { LancamentoRelatorio } from "@/features/relatorios/types"

// Grade da tabela desktop (Descricao / Data / Conta / Valor); o cabecalho usa
// a mesma string para as colunas alinharem.
export const GRADE_COLUNAS_LANCAMENTO_RELATORIO =
  "md:grid-cols-[minmax(0,2fr)_minmax(0,1fr)_minmax(0,1.2fr)_minmax(0,1fr)]"

const MESES = [
  "janeiro", "fevereiro", "marco", "abril", "maio", "junho",
  "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
]

// "yyyy-MM-dd" -> "2 de julho" (sem Date, evita deslocamento de fuso).
function formatarDia(data: string): string {
  const [, mes, dia] = data.split("-").map(Number)
  return `${dia} de ${MESES[mes - 1]}`
}

// Linha de tabela no desktop; lista (descricao + meta + valor) no mobile.
export function LancamentoRelatorioItem({ lancamento }: { lancamento: LancamentoRelatorio }) {
  const titulo = lancamento.descricao?.trim() || lancamento.subcategoriaNome || "Sem descricao"
  const detalhe = lancamento.subcategoriaNome && lancamento.descricao ? lancamento.subcategoriaNome : null

  return (
    <li
      className={cn(
        "flex items-center gap-3 border-b-[0.5px] border-border p-3.5 last:border-b-0",
        "md:grid md:gap-0 md:px-[22px] md:py-3.5",
        GRADE_COLUNAS_LANCAMENTO_RELATORIO,
      )}
    >
      <div className="min-w-0 flex-1">
        <div className="truncate text-sm text-text-body">{titulo}</div>
        {detalhe ? <div className="hidden truncate text-xs text-text-faint md:block">{detalhe}</div> : null}
        <div className="mt-[3px] flex flex-wrap items-center gap-1.5 text-xs text-text-faint md:hidden">
          <span>{formatarDia(lancamento.data)} ·</span>
          {lancamento.contaTipo === "CARTAO" ? <Badge variant="parcela">Cartao</Badge> : null}
          <span>{lancamento.contaNome}</span>
          {detalhe ? <span>· {detalhe}</span> : null}
        </div>
      </div>

      <span className="hidden text-[13px] whitespace-nowrap text-text-muted md:block">
        {formatarDia(lancamento.data)}
      </span>

      <div className="hidden min-w-0 items-center gap-1.5 text-[13px] text-text-muted md:flex">
        {lancamento.contaTipo === "CARTAO" ? <Badge variant="parcela">Cartao</Badge> : null}
        <span className="truncate">{lancamento.contaNome}</span>
      </div>

      <span className="text-sm whitespace-nowrap text-text-primary md:text-right">
        {formatarMoeda(lancamento.valor)}
      </span>
    </li>
  )
}
