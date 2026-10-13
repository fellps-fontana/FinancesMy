import { Link } from "react-router-dom"
import { Card, CardContent } from "@/shared/ui/card"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { cn } from "@/shared/lib/utils"
import { useContas } from "@/features/contas/hooks/useContas"
import { calcularPatrimonioTotal } from "@/features/contas/lib/calcularPatrimonioTotal"
import type { ContaResponse } from "@/features/contas/types"

// Dashboard mostra so contas BANCO (corrente/poupanca/dinheiro fisico, item
// 10): investimento tem tela propria (/investimentos) e cartao e linha de
// credito, nao saldo. Filtro de selecao, nao calculo de dominio - `saldo` de
// cada conta ja chega resolvido do backend (regra-de-negocio.md item 10).
function selecionarContasBanco(contas: ContaResponse[]): ContaResponse[] {
  return contas.filter((conta) => conta.tipo === "Banco")
}

type CardSaldoTotalProps = {
  className?: string
}

// Card "Saldo total" (mockups 02 Dashboard): agregacao de exibicao dos saldos
// ja prontos do backend via `calcularPatrimonioTotal` (mesma util da tela
// /contas, lib testavel) - o componente so formata.
export function CardSaldoTotal({ className }: CardSaldoTotalProps) {
  const { data, isLoading, isError } = useContas()

  return (
    <Card
      className={cn(
        "gap-0 rounded-2xl py-5 [--card-spacing:--spacing(5)] md:py-[22px] md:[--card-spacing:22px]",
        className,
      )}
    >
      <CardContent className="flex flex-col gap-2">
        <span className="text-[13px] text-text-muted">Saldo total</span>

        {isLoading && <span className="text-sm text-text-muted">Carregando saldo...</span>}
        {isError && <span className="text-sm text-negativo">Nao foi possivel carregar o saldo.</span>}
        {!isLoading && !isError && data !== undefined && (
          <span className="text-[28px] font-medium text-text-primary">
            {formatarMoeda(calcularPatrimonioTotal(selecionarContasBanco(data)))}
          </span>
        )}
      </CardContent>
    </Card>
  )
}

type ContasResumoProps = {
  className?: string
}

// Card "Contas" (mockup desktop, coluna direita): saldo de cada conta banco,
// clique leva a /contas. Apresentacao pura sobre dados do hook existente.
export function ContasResumo({ className }: ContasResumoProps) {
  const { data, isLoading, isError } = useContas()
  const contas = data === undefined ? [] : selecionarContasBanco(data)

  return (
    <Card className={cn("rounded-2xl py-5 [--card-spacing:--spacing(5.5)]", className)}>
      <Link
        to="/contas"
        className="flex flex-col gap-3.5 rounded-2xl px-(--card-spacing) transition-colors hover:bg-muted/40 focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
      >
        <span className="text-[15px] text-text-primary">Contas</span>

        {isLoading && <p className="text-sm text-text-muted">Carregando contas...</p>}
        {isError && <p className="text-sm text-negativo">Nao foi possivel carregar as contas.</p>}
        {!isLoading && !isError && contas.length === 0 && (
          <p className="text-sm text-text-muted">Nenhuma conta cadastrada.</p>
        )}

        {contas.length > 0 && (
          <ul className="flex flex-col gap-3">
            {contas.map((conta) => (
              <li key={conta.id} className="flex items-center justify-between gap-3">
                <span className="truncate text-sm text-text-body">{conta.nome}</span>
                <span className="shrink-0 text-[13px] text-text-muted">
                  {formatarMoeda(conta.saldo)}
                </span>
              </li>
            ))}
          </ul>
        )}
      </Link>
    </Card>
  )
}
