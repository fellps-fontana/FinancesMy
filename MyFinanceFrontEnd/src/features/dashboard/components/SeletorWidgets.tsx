import { cn } from "@/shared/lib/utils"
import { Card, CardContent } from "@/shared/ui/card"
import { CATALOGO_WIDGETS, type PreferenciaWidgets, type WidgetId } from "@/features/dashboard/lib/preferenciaWidgets"

type SeletorWidgetsProps = {
  preferencia: PreferenciaWidgets
  onAlternarWidget: (id: WidgetId) => void
  className?: string
}

/**
 * Componente de apresentacao (burro): lista todo o CATALOGO_WIDGETS do
 * Dashboard com um switch liga/desliga por item. Nao le nem escreve
 * localStorage aqui -- so recebe `preferencia` (estado atual) e dispara
 * `onAlternarWidget` (callback do container). Estado e persistencia moram em
 * DashboardPage.tsx + lib/preferenciaWidgets.ts (clean-code.md "Organizacao
 * (React)": apresentacao separada de logica/estado).
 *
 * Sem componente de Switch pronto em shared/ui/ (so button/card/input/label/
 * alert hoje) e fora do escopo desta task adicionar um -- o toggle e um
 * <button role="switch"> autocontido, mesmo padrao acessivel de switch (ARIA
 * Authoring Practices), estilizado 100% com os tokens de
 * identidade-visual.md: bg-primary = roxo de acao/destaque (ligado, mesmo
 * token do botao primario) e bg-accent = "bg-surface-alt" da identidade
 * visual (desligado -- neste design system --accent do shadcn foi mapeado
 * pro hex de bg-surface-alt, ver index.css/@theme; --primary carrega o roxo
 * real da identidade). Sem cor crua fora de token em nenhum estado.
 */
export function SeletorWidgets({ preferencia, onAlternarWidget, className }: SeletorWidgetsProps) {
  return (
    <Card className={className}>
      <CardContent className="flex flex-col gap-3">
        <span className="text-[13px] text-text-muted">Widgets do dashboard</span>

        <ul className="flex flex-col gap-3">
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
      </CardContent>
    </Card>
  )
}
