import { useState } from "react"
import { ChevronDown } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { Card, CardContent } from "@/shared/ui/card"
import { CATALOGO_WIDGETS, type PreferenciaWidgets, type WidgetId } from "@/features/dashboard/lib/preferenciaWidgets"

type SeletorWidgetsProps = {
  preferencia: PreferenciaWidgets
  onAlternarWidget: (id: WidgetId) => void
  className?: string
}

const ID_LISTA = "seletor-widgets-lista"

/**
 * Componente de apresentacao (burro): lista todo o CATALOGO_WIDGETS do
 * Dashboard com um switch liga/desliga por item. Nao le nem escreve
 * localStorage aqui -- so recebe `preferencia` (estado atual) e dispara
 * `onAlternarWidget` (callback do container). Estado de PREFERENCIA e
 * persistencia moram em DashboardPage.tsx + lib/preferenciaWidgets.ts
 * (clean-code.md "Organizacao (React)": apresentacao separada de
 * logica/estado).
 *
 * Colapsavel, fechado por padrao (`expandido` e estado de UI puro deste
 * componente -- efemero, nao persiste entre cargas, ao contrario da
 * preferencia de widgets em si). So o cabecalho/trigger "Personalizar
 * widgets" fica visivel de inicio; clicar expande a lista de switches,
 * clicar de novo colapsa. Mora no fim da pagina (ver DashboardPage.tsx) por
 * ser configuracao, nao conteudo do dashboard.
 *
 * Sem componente de Switch/Collapsible pronto em shared/ui/ (so
 * button/card/input/label/alert hoje) e fora do escopo desta task adicionar
 * um -- o toggle e um <button role="switch"> autocontido, mesmo padrao
 * acessivel de switch (ARIA Authoring Practices), estilizado 100% com os
 * tokens de identidade-visual.md: bg-primary = roxo de acao/destaque
 * (ligado, mesmo token do botao primario) e bg-accent = "bg-surface-alt" da
 * identidade visual (desligado -- neste design system --accent do shadcn foi
 * mapeado pro hex de bg-surface-alt, ver index.css/@theme; --primary carrega
 * o roxo real da identidade). O trigger de expandir usa o mesmo
 * `aria-expanded`/`aria-controls` do padrao ARIA disclosure, com a transicao
 * feita via CSS puro (grid-template-rows 0fr -> 1fr + opacidade), sem
 * biblioteca extra e sem neon/gradiente (identidade-visual.md "Sem
 * brilho/neon/gradiente chamativo").
 */
export function SeletorWidgets({ preferencia, onAlternarWidget, className }: SeletorWidgetsProps) {
  const [expandido, setExpandido] = useState(false)

  return (
    <Card className={className}>
      <CardContent className="flex flex-col">
        <button
          type="button"
          aria-expanded={expandido}
          aria-controls={ID_LISTA}
          onClick={() => setExpandido((atual) => !atual)}
          className="flex items-center justify-between gap-3 text-[13px] text-text-muted focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50 rounded-md"
        >
          <span>Personalizar widgets</span>
          <ChevronDown
            aria-hidden="true"
            className={cn("size-4 shrink-0 transition-transform duration-200", expandido && "rotate-180")}
          />
        </button>

        <div
          id={ID_LISTA}
          className={cn(
            "grid overflow-hidden transition-all duration-200 ease-in-out",
            expandido ? "grid-rows-[1fr] pt-3 opacity-100" : "grid-rows-[0fr] pt-0 opacity-0",
          )}
        >
          <ul className="flex flex-col gap-3 overflow-hidden">
            {CATALOGO_WIDGETS.map((widget) => {
              const ativo = preferencia[widget.id]

              return (
                <li key={widget.id} className="flex items-center justify-between gap-3">
                  <span className="text-sm text-text-body">{widget.label}</span>

                  <button
                    type="button"
                    role="switch"
                    aria-checked={ativo}
                    aria-label={`Exibir widget "${widget.label}" no dashboard`}
                    onClick={() => onAlternarWidget(widget.id)}
                    className={cn(
                      "relative h-5 w-9 shrink-0 rounded-full transition-colors",
                      "focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50",
                      ativo ? "bg-primary" : "bg-accent",
                    )}
                  >
                    <span
                      aria-hidden="true"
                      className={cn(
                        "absolute top-0.5 left-0.5 size-4 rounded-full bg-primary-foreground transition-transform",
                        ativo ? "translate-x-[16px]" : "translate-x-0",
                      )}
                    />
                  </button>
                </li>
              )
            })}
          </ul>
        </div>
      </CardContent>
    </Card>
  )
}
