import { ChevronLeft, ChevronRight } from "lucide-react"
import { Button } from "@/shared/ui/button"

type NavegacaoMesProps = {
  rotulo: string
  podeAvancar?: boolean
  onAnterior: () => void
  onProximo: () => void
}

export function NavegacaoMes({
  rotulo,
  podeAvancar = true,
  onAnterior,
  onProximo,
}: NavegacaoMesProps) {
  return (
    <div className="flex items-center gap-2">
      <Button variant="outline" size="icon" onClick={onAnterior} aria-label="Mês anterior">
        <ChevronLeft />
      </Button>
      <span className="min-w-36 text-center text-sm text-text-primary" aria-live="polite">
        {rotulo}
      </span>
      <Button
        variant="outline"
        size="icon"
        onClick={onProximo}
        disabled={!podeAvancar}
        title={!podeAvancar ? "Limite de 12 meses futuros atingido" : undefined}
        aria-label="Próximo mês"
      >
        <ChevronRight />
      </Button>
    </div>
  )
}
