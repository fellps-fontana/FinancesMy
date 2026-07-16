import { Button } from "@/shared/ui/button"
import type { FiltroTipoAtivo as FiltroTipoAtivoValue } from "@/features/investimentos/lib/filtrarAtivosPorTipo"

type FiltroTipoAtivoProps = {
  filtro: FiltroTipoAtivoValue
  onFiltroChange: (filtro: FiltroTipoAtivoValue) => void
}

const OPCOES: { valor: FiltroTipoAtivoValue; label: string }[] = [
  { valor: "Todos", label: "Todos" },
  { valor: "RendaFixa", label: "Renda fixa" },
  { valor: "RendaVariavel", label: "Renda variavel" },
]

// Componente de apresentacao (burro): filtro Todos/Renda fixa/Renda variavel
// do mockup "11 Investimentos". A filtragem em si (lib/filtrarAtivosPorTipo)
// e a definicao de qual opcao esta ativa moram no container.
export function FiltroTipoAtivo({ filtro, onFiltroChange }: FiltroTipoAtivoProps) {
  return (
    <div className="flex flex-wrap gap-2" role="group" aria-label="Filtrar ativos por tipo">
      {OPCOES.map((opcao) => (
        <Button
          key={opcao.valor}
          type="button"
          size="sm"
          variant={filtro === opcao.valor ? "default" : "outline"}
          aria-pressed={filtro === opcao.valor}
          onClick={() => onFiltroChange(opcao.valor)}
        >
          {opcao.label}
        </Button>
      ))}
    </div>
  )
}
