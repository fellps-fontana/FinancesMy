import { Badge } from "@/shared/ui/badge"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import type { LancamentoRelatorio } from "@/features/relatorios/types"

const MESES = [
  "janeiro", "fevereiro", "marco", "abril", "maio", "junho",
  "julho", "agosto", "setembro", "outubro", "novembro", "dezembro",
]

// "yyyy-MM-dd" -> "2 de julho" (sem Date, evita deslocamento de fuso).
function formatarDia(data: string): string {
  const [, mes, dia] = data.split("-").map(Number)
  return `${dia} de ${MESES[mes - 1]}`
}

export function LancamentoRelatorioItem({ lancamento }: { lancamento: LancamentoRelatorio }) {
  const titulo = lancamento.descricao?.trim() || lancamento.subcategoriaNome || "Sem descricao"
  return (
    <li className="flex items-center gap-3 border-b-[0.5px] border-border p-3.5 last:border-b-0">
      <div className="min-w-0 flex-1">
        <div className="truncate text-[14px] text-foreground">{titulo}</div>
        <div className="mt-[3px] flex flex-wrap items-center gap-1.5 text-[12px] text-text-faint">
          <span>{formatarDia(lancamento.data)} ·</span>
          {lancamento.contaTipo === "CARTAO" ? <Badge variant="parcela">Cartao</Badge> : null}
          <span>{lancamento.contaNome}</span>
          {lancamento.subcategoriaNome && lancamento.descricao ? (
            <span>· {lancamento.subcategoriaNome}</span>
          ) : null}
        </div>
      </div>
      <span className="text-[14px] text-text-primary">{formatarMoeda(lancamento.valor)}</span>
    </li>
  )
}
