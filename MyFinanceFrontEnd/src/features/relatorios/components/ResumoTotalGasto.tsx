import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { cn } from "@/shared/lib/utils"

type Props = {
  totalGasto: number
  // null => sem base de comparacao: a linha de variacao some.
  variacaoPercentual: number | null
}

const formatadorVariacao = new Intl.NumberFormat("pt-BR", {
  maximumFractionDigits: 1,
  signDisplay: "exceptZero",
})

// Gasto maior que o mes anterior = negativo; menor = positivo (cor de dominio).
export function ResumoTotalGasto({ totalGasto, variacaoPercentual }: Props) {
  return (
    <div className="flex flex-col gap-1 rounded-2xl border-[0.5px] border-border bg-card p-5">
      <span className="text-[13px] text-muted-foreground">Total gasto</span>
      <span className="text-[28px] leading-tight font-medium text-text-primary">
        {formatarMoeda(totalGasto)}
      </span>
      {variacaoPercentual !== null ? (
        <span
          className={cn(
            "text-[13px]",
            variacaoPercentual > 0 && "text-negativo",
            variacaoPercentual < 0 && "text-positivo",
            variacaoPercentual === 0 && "text-muted-foreground",
          )}
        >
          {formatadorVariacao.format(variacaoPercentual)}% vs mes anterior
        </span>
      ) : null}
    </div>
  )
}
