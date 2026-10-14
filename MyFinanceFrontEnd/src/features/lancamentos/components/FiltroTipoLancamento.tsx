import { Chip } from "@/shared/ui/chip"
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

// Chips Todos/Entradas/Saidas (mockup s_lancamentos): apresentacao pura. A
// filtragem em si vive em lib/filtrarPeriodo.ts. Ativo: accent-deep/accent-soft;
// inativo: superficie com borda, texto muted (como no mockup). Desktop usa
// 13px e padding maior que o mobile (12px).
export function FiltroTipoLancamento({ valor, onChange }: FiltroTipoLancamentoProps) {
  return (
    <div className="flex flex-wrap gap-2" role="group" aria-label="Filtro por tipo de lancamento">
      {OPCOES.map((opcao) => {
        const ativo = opcao.valor === valor
        return (
          <Chip
            key={opcao.valor}
            selected={ativo}
            onClick={() => onChange(opcao.valor)}
            className={cn(
              "md:px-3.5 md:py-2 md:text-[13px]",
              !ativo && "border-[0.5px] border-border bg-card",
            )}
          >
            {opcao.label}
          </Chip>
        )
      })}
    </div>
  )
}
