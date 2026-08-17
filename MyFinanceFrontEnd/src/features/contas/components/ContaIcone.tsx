import { cn } from "@/shared/lib/utils"
import { obterIconeConta } from "@/features/contas/lib/obterIconeConta"
import type { ContaResponse } from "@/features/contas/types"

type ContaIconeProps = {
  conta: Pick<ContaResponse, "icone" | "cor" | "subtipo" | "tipo">
}

// Componente de apresentacao puro (burro): quadrado arredondado 34px com
// icone dentro, conforme identidade-visual.md ("Icone de status/categoria").
// Sem `cor` cadastrada na conta, usa o par de tokens padrao (bg-accent-deep +
// text-accent-soft, ver mockup 03 Contas). Com `cor` cadastrada (TASK-127,
// hex livre escolhido pelo usuario no cadastro - nao e decoracao arbitraria
// da tela, e dado de dominio por conta), aplica como fundo do quadrado; o
// hex vem do backend por registro, entao a excecao ao "nunca cor crua fora
// dos tokens" e o proprio dado, nao um valor inventado no componente.
export function ContaIcone({ conta }: ContaIconeProps) {
  const Icone = obterIconeConta(conta)
  const temCorPersonalizada = Boolean(conta.cor)

  return (
    <div
      className={cn(
        "flex h-[34px] w-[34px] shrink-0 items-center justify-center rounded-[10px]",
        !temCorPersonalizada && "bg-accent-deep",
      )}
      style={temCorPersonalizada ? { backgroundColor: conta.cor! } : undefined}
    >
      <Icone
        className={cn("h-4 w-4", temCorPersonalizada ? "text-primary-foreground" : "text-accent-soft")}
        strokeWidth={1.6}
      />
    </div>
  )
}
