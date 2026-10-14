import { ChevronLeft, ChevronRight } from "lucide-react"
import { formatarRotuloMes } from "@/features/lancamentos/lib/filtrarPeriodo"

type NavegadorMesProps = {
  mesReferencia: string
  onMesAnterior: () => void
  onProximoMes: () => void
}

// Apresentacao pura: recebe o mes ("yyyy-MM") e dispara os callbacks. Mobile:
// barra cheia com setas nas pontas (mockup mobile); desktop: pilula compacta
// do mockup (9px, 13px) com as mesmas setas.
export function NavegadorMes({ mesReferencia, onMesAnterior, onProximoMes }: NavegadorMesProps) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-xl border-[0.5px] border-border bg-card px-4 py-3 md:w-fit md:rounded-[9px] md:px-3.5 md:py-2">
      <button
        type="button"
        aria-label="Mes anterior"
        onClick={onMesAnterior}
        className="flex size-6 shrink-0 items-center justify-center text-text-muted transition-colors hover:text-text-body"
      >
        <ChevronLeft className="size-4" strokeWidth={1.8} />
      </button>
      <span className="text-sm text-text-body md:min-w-24 md:text-center md:text-[13px]">
        {formatarRotuloMes(mesReferencia)}
      </span>
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
