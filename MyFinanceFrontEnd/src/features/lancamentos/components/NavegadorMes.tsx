import { ChevronLeft, ChevronRight } from "lucide-react"
import { formatarRotuloMes } from "@/features/lancamentos/lib/filtrarPeriodo"

type NavegadorMesProps = {
  mesReferencia: string
  onMesAnterior: () => void
  onProximoMes: () => void
}

// Componente de apresentacao (burro): so recebe o mes referencia ja pronto
// ("yyyy-MM") e dispara os callbacks de navegacao - nenhuma logica de soma de
// mes mora aqui (isso e `somarMeses`, lib/filtrarPeriodo.ts). Rotulo
// formatado por `formatarRotuloMes` (mesma lib), nao calculado inline no JSX
// (clean-code.md, "Organizacao (React)": calculo nao vive no componente).
export function NavegadorMes({ mesReferencia, onMesAnterior, onProximoMes }: NavegadorMesProps) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-xl border border-border bg-card px-4 py-3 md:w-fit md:px-3.5 md:py-2.5">
      <button
        type="button"
        aria-label="Mes anterior"
        onClick={onMesAnterior}
        className="flex size-6 shrink-0 items-center justify-center text-text-muted transition-colors hover:text-text-body"
      >
        <ChevronLeft className="size-4" strokeWidth={1.8} />
      </button>
      <span className="text-[13px] text-text-body">{formatarRotuloMes(mesReferencia)}</span>
      <button
        type="button"
        aria-label="Proximo mes"
        onClick={onProximoMes}
        className="flex size-6 shrink-0 items-center justify-center text-text-muted transition-colors hover:text-text-body"
      >
        <ChevronRight className="size-4" strokeWidth={1.8} />
      </button>
    </div>
  )
}
