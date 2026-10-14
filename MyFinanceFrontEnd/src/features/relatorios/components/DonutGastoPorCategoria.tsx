import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { cn } from "@/shared/lib/utils"
import type { FatiaDonut } from "@/features/relatorios/lib/fatiasRelatorio"

// Circulo de raio 15,915 tem circunferencia 100: o percentual (0-100) vira
// comprimento de traco direto.
const RAIO = 15.915

type DonutGastoPorCategoriaProps = {
  fatias: FatiaDonut[]
  totalGasto: number
  // null => sem base de comparacao: a linha de variacao some.
  variacaoPercentual: number | null
  className?: string
}

const formatadorVariacao = new Intl.NumberFormat("pt-BR", {
  maximumFractionDigits: 1,
  signDisplay: "exceptZero",
})

// Apresentacao pura (mockup s_relatorio): donut de 220px com "Total gasto" no
// buraco e a variacao abaixo. Gasto maior que o mes anterior = negativo
// (coral); menor = positivo (verde). Percentuais vem do backend.
export function DonutGastoPorCategoria({
  fatias,
  totalGasto,
  variacaoPercentual,
  className,
}: DonutGastoPorCategoriaProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-4 rounded-2xl border-[0.5px] border-border bg-card p-7",
        className,
      )}
    >
      <div className="relative size-[220px] shrink-0">
        <svg viewBox="0 0 42 42" className="size-full -rotate-90" role="img" aria-label="Gasto por categoria">
          <circle cx="21" cy="21" r={RAIO} fill="none" strokeWidth="6" className="stroke-accent" />
          {fatias.map((fatia) => (
            <circle
              key={fatia.chave}
              cx="21"
              cy="21"
              r={RAIO}
              fill="none"
              strokeWidth="6"
              className={fatia.cor.traco}
              strokeDasharray={`${fatia.percentual} ${100 - fatia.percentual}`}
              strokeDashoffset={-fatia.inicio}
            />
          ))}
        </svg>
        <div className="absolute inset-0 m-auto flex size-[138px] flex-col items-center justify-center rounded-full bg-card">
          <span className="text-[13px] text-text-muted">Total gasto</span>
          <span className="mt-1 text-xl font-medium whitespace-nowrap text-text-primary">
            {formatarMoeda(totalGasto)}
          </span>
        </div>
      </div>

      {variacaoPercentual !== null ? (
        <div
          className={cn(
            "text-[13px]",
            variacaoPercentual > 0 && "text-negativo",
            variacaoPercentual < 0 && "text-positivo",
            variacaoPercentual === 0 && "text-text-muted",
          )}
        >
          {formatadorVariacao.format(variacaoPercentual)}% vs mes anterior
        </div>
      ) : null}
    </div>
  )
}
