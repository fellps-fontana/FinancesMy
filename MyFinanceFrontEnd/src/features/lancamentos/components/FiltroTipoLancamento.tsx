import { cn } from "@/shared/lib/utils"
import type { FiltroTipoLancamento as ValorFiltroTipoLancamento } from "@/features/lancamentos/lib/filtrarPeriodo"

type FiltroTipoLancamentoProps = {
  valor: ValorFiltroTipoLancamento
  onChange: (valor: ValorFiltroTipoLancamento) => void
}

const OPCOES: { valor: ValorFiltroTipoLancamento; label: string }[] = [
  { valor: "TODOS", label: "Todos" },
  { valor: "ENTRADAS", label: "Entradas" },
  { valor: "SAIDAS", label: "Saidas" },
]

// Componente de apresentacao puro (chips Todos/Entradas/Saidas do mockup "04
// Lancamentos"): so exibe as 3 opcoes e delega a escolha pro pai via
// onChange. A filtragem em si (filtrarPorTipo) vive em lib/filtrarPeriodo.ts
// - este componente nao sabe o que cada opcao faz sobre a lista, so
// representa o estado ja resolvido pelo pai. Chip ativo usa accent-deep/
// accent-soft (identidade-visual.md: "roxo = acao/categoria", mesmo par de
// tokens do chip "Todos" selecionado no mockup).
export function FiltroTipoLancamento({ valor, onChange }: FiltroTipoLancamentoProps) {
  return (
    <div className="flex flex-wrap gap-2" role="group" aria-label="Filtro por tipo de lancamento">
      {OPCOES.map((opcao) => {
        const ativo = opcao.valor === valor
        return (
          <button
            key={opcao.valor}
            type="button"
            aria-pressed={ativo}
            onClick={() => onChange(opcao.valor)}
            className={cn(
              "rounded-full px-3.5 py-1.5 text-[13px] font-medium transition-colors",
              ativo
                ? "bg-accent-deep text-accent-soft"
                : "border border-border bg-card text-text-muted hover:text-text-body",
            )}
          >
            {opcao.label}
          </button>
        )
      })}
    </div>
  )
}
