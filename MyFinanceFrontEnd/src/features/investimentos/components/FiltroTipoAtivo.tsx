import { Chip } from "@/shared/ui/chip"
import type { FiltroTipoAtivo as FiltroTipoAtivoValue } from "@/features/investimentos/lib/filtrarAtivosPorTipo"

type FiltroTipoAtivoProps = {
  filtro: FiltroTipoAtivoValue
  onFiltroChange: (filtro: FiltroTipoAtivoValue) => void
}

const OPCOES: { valor: FiltroTipoAtivoValue; label: string }[] = [
  { valor: "Todos", label: "Todos" },
  { valor: "RendaFixa", label: "Renda fixa" },
  { valor: "RendaVariavel", label: "Variavel" },
]

// Apresentacao pura: chips Todos / Renda fixa / Variavel (mockup). A
// filtragem em si (lib/filtrarAtivosPorTipo) mora no container.
export function FiltroTipoAtivo({ filtro, onFiltroChange }: FiltroTipoAtivoProps) {
  return (
    <div className="flex flex-wrap gap-2" role="group" aria-label="Filtrar ativos por tipo">
      {OPCOES.map((opcao) => (
        <Chip
          key={opcao.valor}
          selected={filtro === opcao.valor}
          onClick={() => onFiltroChange(opcao.valor)}
          className="md:px-3.5 md:py-2 md:text-[13px]"
        >
          {opcao.label}
        </Chip>
      ))}
    </div>
  )
}
