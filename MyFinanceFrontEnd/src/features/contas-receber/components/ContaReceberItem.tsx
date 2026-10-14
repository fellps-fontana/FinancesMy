import { Pencil, Receipt, Trash2, UserRound } from "lucide-react"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { cn } from "@/shared/lib/utils"
import { Badge, type BadgeVariant } from "@/shared/ui/badge"
import { IconTile } from "@/shared/ui/icon-tile"
import { descreverVencimento } from "@/features/contas-receber/lib/resumirContasReceber"
import { formatarData } from "@/features/cartao/lib/formatarData"
import {
  STATUS_CONTA_RECEBER,
  type ContaReceberResponse,
  type StatusContaReceber,
} from "@/features/contas-receber/types"

// Grade unica da tabela desktop (mockup s_receber): descricao, vencimento,
// progresso, valor total, status, Receber, acoes. Cabecalho e linhas dividem
// a mesma string para as colunas nunca desalinharem.
const GRADE_DESKTOP = "md:grid md:grid-cols-[2fr_1fr_1.4fr_1fr_0.8fr_0.8fr_64px] md:items-center"

// Cor de status carrega o dominio (item 13): PENDENTE e PARCIAL ainda aguardam
// recebimento (alerta/accent), RECEBIDO e final (positivo). "Atrasado" nao e
// status do backend: e o vencimento de uma conta ainda em aberto, derivado da
// data prevista e exibido como estado proprio (coral).
const BADGE_POR_STATUS: Record<StatusContaReceber, { rotulo: string; variante: BadgeVariant }> = {
  [STATUS_CONTA_RECEBER.PENDENTE]: { rotulo: "Pendente", variante: "pendente" },
  [STATUS_CONTA_RECEBER.PARCIAL]: { rotulo: "Parcial", variante: "sugerido" },
  [STATUS_CONTA_RECEBER.RECEBIDO]: { rotulo: "Recebido", variante: "pago" },
}

const BADGES_POR_STATUS_API: Record<string, { rotulo: string; variante: BadgeVariant } | undefined> = BADGE_POR_STATUS

type ContaReceberItemProps = {
  contaReceber: ContaReceberResponse
  onReceber: (conta: ContaReceberResponse) => void
  onEditar: (conta: ContaReceberResponse) => void
  onExcluir: (conta: ContaReceberResponse) => void
  /** Mobile: toque no card abre a folha de acoes. */
  onAbrirAcoes: (conta: ContaReceberResponse) => void
  /** Preenchida quando Editar esta bloqueado (ocorrencia de recebivel recorrente). */
  dicaEdicaoBloqueada: string | null
}

type BarraRecebidoProps = {
  valorTotal: number
  saldoPendente: number
  className?: string
}

// Barra de progresso do recebimento. valorTotal e saldoPendente chegam do
// backend (item 13); "recebido" e a mesma subtracao que define o saldo, so
// invertida para exibir "R$ 300 de R$ 900". O percentual e clamp visual.
export function BarraRecebido({ valorTotal, saldoPendente, className }: BarraRecebidoProps) {
  const valorRecebido = valorTotal - saldoPendente
  const percentual = valorTotal > 0 ? Math.min(100, Math.max(0, (valorRecebido / valorTotal) * 100)) : 0

  return (
    <div
      role="progressbar"
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={Math.round(percentual)}
      aria-label="Progresso do recebimento"
      className={cn("h-[5px] overflow-hidden rounded-[5px] bg-muted", className)}
    >
      <div className="h-full bg-positivo" style={{ width: `${percentual}%` }} />
    </div>
  )
}

export function CabecalhoTabelaContasReceber() {
  return (
    <div
      className={cn(
        "hidden border-b-[0.5px] border-border px-[22px] py-3.5 text-xs text-text-muted",
        GRADE_DESKTOP,
      )}
    >
      <span>Descricao</span>
      <span>Vencimento</span>
      <span>Progresso</span>
      <span className="text-right">Valor total</span>
      <span className="text-right">Status</span>
      <span />
      <span />
    </div>
  )
}

// Apresentacao pura (regra-de-negocio.md item 13): valorTotal, saldoPendente e
// status vem prontos do backend; aqui so se exibe. Desktop: linha de tabela com
// "Receber" em texto e icones Editar/Excluir; mobile: card inteiro e alvo de
// toque que abre a folha de acoes.
export function ContaReceberItem({
  contaReceber,
  onReceber,
  onEditar,
  onExcluir,
  onAbrirAcoes,
  dicaEdicaoBloqueada,
}: ContaReceberItemProps) {
  const emAberto = contaReceber.status !== STATUS_CONTA_RECEBER.RECEBIDO
  const vencimento = descreverVencimento(contaReceber, formatarData)
  const badge: { rotulo: string; variante: BadgeVariant } = vencimento.atrasada
    ? { rotulo: "Atrasado", variante: "atrasado" }
    : (BADGES_POR_STATUS_API[contaReceber.status] ?? { rotulo: contaReceber.status, variante: "manual" })
  const emprestimo = contaReceber.tipo === "EMPRESTIMO"
  const Icone = emprestimo ? UserRound : Receipt
  const valorRecebido = contaReceber.valorTotal - contaReceber.saldoPendente
  const textoProgresso = `${formatarMoeda(valorRecebido)} de ${formatarMoeda(contaReceber.valorTotal)}`
  const edicaoBloqueada = dicaEdicaoBloqueada !== null

  return (
    <div
      className={cn(
        "relative rounded-[14px] border-[0.5px] border-border bg-card p-3.5",
        "md:rounded-none md:border-0 md:border-b-[0.5px] md:bg-transparent md:px-[22px] md:py-3.5 md:last:border-b-0",
        GRADE_DESKTOP,
      )}
    >
      <button
        type="button"
        aria-label={`Acoes de ${contaReceber.descricao}`}
        onClick={() => onAbrirAcoes(contaReceber)}
        className="absolute inset-0 rounded-[14px] outline-none focus-visible:ring-3 focus-visible:ring-ring/50 md:hidden"
      />

      <div className="flex items-center gap-3">
        <IconTile>
          <Icone strokeWidth={1.6} aria-hidden="true" />
        </IconTile>
        <div className="min-w-0 flex-1">
          <div className="truncate text-sm text-text-body">{contaReceber.descricao}</div>
          {emprestimo && contaReceber.pessoa ? (
            <div className="truncate text-xs text-text-faint max-md:hidden">
              Emprestado a {contaReceber.pessoa}
            </div>
          ) : null}
          <div
            className={cn(
              "mt-0.5 text-xs md:hidden",
              vencimento.atrasada ? "text-negativo" : "text-text-faint",
            )}
          >
            {vencimento.rotulo}
          </div>
        </div>
        <div className="shrink-0 text-right md:hidden">
          <div className="text-sm text-text-primary">{formatarMoeda(contaReceber.valorTotal)}</div>
          <Badge variant={badge.variante} className="mt-1">
            {badge.rotulo}
          </Badge>
        </div>
      </div>

      <div className="mt-2.5 md:hidden">
        <div className="mb-1.5 flex justify-between text-xs text-text-muted">
          <span>Recebido</span>
          <span>{textoProgresso}</span>
        </div>
        <BarraRecebido valorTotal={contaReceber.valorTotal} saldoPendente={contaReceber.saldoPendente} />
      </div>

      <span
        className={cn(
          "hidden text-[13px] md:inline",
          vencimento.atrasada ? "text-negativo" : "text-text-muted",
        )}
      >
        {vencimento.rotulo}
      </span>

      <div className="hidden pr-4 md:block">
        <BarraRecebido valorTotal={contaReceber.valorTotal} saldoPendente={contaReceber.saldoPendente} />
        <div className="mt-1.5 text-xs text-text-muted">{textoProgresso}</div>
      </div>

      <span className="hidden text-right text-sm text-text-primary md:inline">
        {formatarMoeda(contaReceber.valorTotal)}
      </span>

      <div className="hidden justify-end md:flex">
        <Badge variant={badge.variante}>{badge.rotulo}</Badge>
      </div>

      <div className="hidden justify-end md:flex">
        {emAberto ? (
          <button
            type="button"
            onClick={() => onReceber(contaReceber)}
            className="rounded-md px-1 text-[13px] text-primary outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
          >
            Receber
          </button>
        ) : null}
      </div>

      <div className="hidden items-center justify-end gap-2 md:flex">
        <span title={dicaEdicaoBloqueada ?? "Editar"}>
          <button
            type="button"
            aria-label={`Editar ${contaReceber.descricao}`}
            disabled={edicaoBloqueada}
            onClick={() => onEditar(contaReceber)}
            className="flex size-7 items-center justify-center rounded-[7px] text-text-muted outline-none hover:bg-muted focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:bg-transparent"
          >
            <Pencil className="size-4" aria-hidden="true" />
          </button>
        </span>
        <button
          type="button"
          title="Excluir"
          aria-label={`Excluir ${contaReceber.descricao}`}
          onClick={() => onExcluir(contaReceber)}
          className="flex size-7 items-center justify-center rounded-[7px] text-negativo outline-none hover:bg-muted focus-visible:ring-3 focus-visible:ring-ring/50"
        >
          <Trash2 className="size-4" aria-hidden="true" />
        </button>
      </div>
    </div>
  )
}
