import { Moon, Sun } from "lucide-react"
import { Button } from "@/shared/ui/button"
import { useTheme } from "@/shared/hooks/useTheme"

// Alterna entre tema claro e escuro (identidade-visual.md, "Mecanismo de
// alternancia"). So alterna light/dark - escolha manual sobrescreve a
// preferencia do SO (useTheme.ts), por isso o toggle nunca volta pra
// "system" sozinho depois do primeiro clique.
export function ThemeToggle() {
  const { resolvedTheme, toggleTheme } = useTheme()
  const isDark = resolvedTheme === "dark"
  const label = isDark ? "Mudar para tema claro" : "Mudar para tema escuro"

  return (
    <Button variant="outline" size="icon-sm" onClick={toggleTheme} aria-label={label} title={label}>
      {isDark ? <Sun className="size-3.5" strokeWidth={1.6} /> : <Moon className="size-3.5" strokeWidth={1.6} />}
    </Button>
  )
}
