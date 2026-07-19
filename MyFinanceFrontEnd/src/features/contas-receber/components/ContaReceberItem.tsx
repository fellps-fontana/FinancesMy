import { Card, CardContent } from "@/shared/ui/card"
import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarData } from "@/features/cartao/lib/formatarData"
import type { ContaReceberResponse } from "@/features/contas-receber/types"

type StatusContaReceber = "PENDENTE" | "PARCIAL" | "RECEBIDO"
type TipoContaReceber = "RECEBIVEL" | "EMPRESTIMO"

// Mapeamento de cor por status (regra-de-negocio.md item 13, formula do
// saldo_pendente/estados). Segue o mesmo espirito de StatusFaturaBadge
// (identidade-visual.md: "pago -> positivo; pendente -> alerta"): PENDENTE e
// PARCIAL ainda aguardam recebimento (alerta), RECEBIDO e o estado final
// positivo. PARCIAL nao tem cor semantica reservada propria no documento de
// identidade - usamos --primary (o roxo real de acao/destaque do tema; o
// token shadcn "accent" NAO e o roxo neste projeto, e sim uma superficie
// neutra escura, ver src/index.css) para marcar "em andamento" sem inventar
// cor nova.
const CONFIG_POR_STATUS: Record<StatusContaReceber, { label: string; className: string }> = {
  PENDENTE: { label: "Pendente", className: "bg-alerta/15 text-alerta" },
  PARCIAL: { label: "Parcial", className: "bg-primary/15 text-primary" },
  RECEBIDO: { label: "Recebido", className: "bg-positivo/15 text-positivo" },
}

// Badge de tipo e neutro (sem semantica de estado) - mesmo tratamento dado a
// origem "manual" em identidade-visual.md ("manual -> neutro: text-muted
// sobre bg-surface-alt").
const LABEL_POR_TIPO: Record<TipoContaReceber, string> = {
  RECEBIVEL: "Recebivel",
  EMPRESTIMO: "Emprestimo",
}

type ContaReceberItemProps = {
  contaReceber: ContaReceberResponse
}

// Componente de apresentacao (puro): recebe a conta a receber com
// saldoPendente/status ja calculados pelo backend (regra-de-negocio.md item
// 13) e apenas exibe - nenhum calculo de saldo ou transicao de status mora
// aqui.
export function ContaReceberItem({ contaReceber }: ContaReceberItemProps) {
  const status = contaReceber.status as StatusContaReceber
  const statusConfig = CONFIG_POR_STATUS[status]

  const tipo = contaReceber.tipo as TipoContaReceber
  const tipoLabel = LABEL_POR_TIPO[tipo] ?? contaReceber.tipo

  const mostrarPessoa = tipo === "EMPRESTIMO" && Boolean(contaReceber.pessoa)
  const saldoPendenteEmAberto = status !== "RECEBIDO"

  return (
    <Card size="sm">
      <CardContent className="flex flex-col gap-2">
        <div className="flex items-start justify-between gap-2">
          <div className="flex flex-col gap-1">
            <span className="text-[19px] font-medium text-text-primary">{contaReceber.descricao}</span>
            <span className="inline-flex w-fit items-center rounded-[5px] bg-muted px-2 py-0.5 text-[12px] font-medium text-text-muted">
              {tipoLabel}
            </span>
          </div>

          {statusConfig && (
            <span
              className={cn(
                "inline-flex items-center rounded-[5px] px-2 py-0.5 text-[12px] font-medium",
                statusConfig.className,
              )}
            >
              {statusConfig.label}
            </span>
          )}
        </div>

        {mostrarPessoa && (
          <span className="text-[13px] text-text-muted">Emprestado a {contaReceber.pessoa}</span>
        )}

        <div className="flex items-center justify-between text-[13px] text-text-muted">
          <span>
            Valor total{" "}
            <span className="font-medium text-text-body">{formatarMoeda(contaReceber.valorTotal)}</span>
          </span>
          <span>
            Saldo pendente{" "}
            <span
              className={cn(
                "font-medium",
                saldoPendenteEmAberto ? "text-alerta" : "text-positivo",
              )}
            >
              {formatarMoeda(contaReceber.saldoPendente)}
            </span>
          </span>
        </div>

        {contaReceber.dataPrevista && (
          <span className="text-[12px] text-text-faint">
            Previsto para {formatarData(contaReceber.dataPrevista)}
          </span>
        )}
      </CardContent>
    </Card>
  )
}
