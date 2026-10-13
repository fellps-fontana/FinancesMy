import { ArrowLeftRight } from "lucide-react"
import { Link } from "react-router-dom"
import { Badge, type BadgeVariant } from "@/shared/ui/badge"
import { Card, CardContent } from "@/shared/ui/card"
import { IconTile } from "@/shared/ui/icon-tile"
import { cn } from "@/shared/lib/utils"
import { obterIconeCatalogo } from "@/shared/lib/catalogoIcones"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import type { CategoriaResponse } from "@/features/categorias/types"
import { useUltimosLancamentos } from "@/features/dashboard/hooks/useUltimosLancamentos"
import type {
  LancamentoResponse,
  StatusLancamento,
  TipoLancamento,
} from "@/features/lancamentos/types"

// Cor/sinal por tipo (regra-de-negocio.md item 2, CRITICA: "usar SEMPRE o
// campo tipo... Nunca somar valor cru"). verde = entrada, coral = saida
// (identidade-visual.md). Transferencia (item 3) nao e entrada nem saida:
// ver LinhaUltimoLancamento.
const CONFIG_POR_TIPO: Record<TipoLancamento, { corValor: string; sinal: string }> = {
  DEBIT: { corValor: "text-negativo", sinal: "-" },
  CREDIT: { corValor: "text-positivo", sinal: "+" },
}

// Status (item 5) -> variante do Badge compartilhado: pago -> positivo,
// pendente -> alerta, sugerido -> accent (identidade-visual.md).
const CONFIG_POR_STATUS: Record<StatusLancamento, { label: string; variant: BadgeVariant }> = {
  PENDENTE: { label: "Pendente", variant: "pendente" },
  SUGERIDO: { label: "Sugerido", variant: "sugerido" },
  PAGO: { label: "Pago", variant: "pago" },
}

const QUANTIDADE_EXIBIDA = 5

const formatadorDiaMes = new Intl.DateTimeFormat("pt-BR", { day: "numeric", month: "long" })

// "Hoje" / "Ontem" / "2 de julho" a partir de yyyy-MM-dd. Parse manual
// evita o deslocamento de fuso de `new Date(iso)`.
export function rotuloData(dataIso: string, hoje: Date): string {
  const [ano, mes, dia] = dataIso.split("-").map(Number)
  const data = new Date(ano, mes - 1, dia)
  const inicioHoje = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate())
  const dias = Math.round((inicioHoje.getTime() - data.getTime()) / (24 * 60 * 60 * 1000))

  if (dias === 0) return "Hoje"
  if (dias === 1) return "Ontem"
  return formatadorDiaMes.format(data)
}

// Indice id -> categoria sobre a hierarquia que o backend ja devolve
// aninhada (item 7). So achata para lookup de nome/icone.
function indexarCategorias(categorias: CategoriaResponse[]): Map<string, CategoriaResponse> {
  const indice = new Map<string, CategoriaResponse>()
  const visitar = (lista: CategoriaResponse[]) => {
    for (const categoria of lista) {
      indice.set(categoria.id, categoria)
      visitar(categoria.subcategorias)
    }
  }
  visitar(categorias)
  return indice
}

type UltimosLancamentosProps = {
  className?: string
}

/**
 * Widget "Ultimos lancamentos" (mockups 02 Dashboard) - previa SOMENTE
 * LEITURA do fluxo de caixa (regra-de-negocio.md item 12), agregada entre as
 * contas BANCO (ver useUltimosLancamentos.ts). Clique leva a /lancamentos.
 * No mobile e uma lista solta; no desktop vira card. Valores e status vem
 * prontos do backend; aqui so formatacao.
 */
export function UltimosLancamentos({ className }: UltimosLancamentosProps) {
  const { data, isLoading, isError } = useUltimosLancamentos(QUANTIDADE_EXIBIDA)
  const { data: categorias } = useCategorias()
  const indiceCategorias = indexarCategorias(categorias ?? [])

  return (
    <Card
      className={cn(
        "gap-0 bg-transparent py-0 ring-0 [--card-spacing:0px]",
        "md:rounded-2xl md:bg-card md:py-5 md:ring-1 md:[--card-spacing:--spacing(5.5)]",
        className,
      )}
    >
      <CardContent className="flex flex-col gap-2.5 md:gap-3.5">
        <Link
          to="/lancamentos"
          className="text-[13px] text-text-muted hover:text-text-body focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50 md:text-[15px] md:text-text-primary"
        >
          Ultimos lancamentos
        </Link>

        {isLoading && <p className="text-sm text-text-muted">Carregando lancamentos...</p>}

        {isError && (
          <p className="text-sm text-negativo">Nao foi possivel carregar os ultimos lancamentos.</p>
        )}

        {!isLoading && !isError && (data === undefined || data.length === 0) && (
          <p className="text-sm text-text-muted">Nenhum lancamento registrado ainda.</p>
        )}

        {!isLoading && !isError && data !== undefined && data.length > 0 && (
          <ul className="flex flex-col gap-2.5 md:gap-3">
            {data.map((lancamento) => (
              <LinhaUltimoLancamento
                key={lancamento.id}
                lancamento={lancamento}
                categoria={
                  lancamento.categoriaId ? indiceCategorias.get(lancamento.categoriaId) : undefined
                }
              />
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}

type LinhaUltimoLancamentoProps = {
  lancamento: LancamentoResponse
  categoria?: CategoriaResponse
}

function LinhaUltimoLancamento({ lancamento, categoria }: LinhaUltimoLancamentoProps) {
  const statusConfig = CONFIG_POR_STATUS[lancamento.status]
  const ehTransferencia = lancamento.classificacao === "TRANSFERENCIA"
  const tipoConfig = CONFIG_POR_TIPO[lancamento.tipo]
  const Icone = ehTransferencia ? ArrowLeftRight : obterIconeCatalogo(categoria?.icone)

  return (
    <li className="flex items-center gap-3">
      <IconTile>
        <Icone strokeWidth={1.6} aria-hidden="true" />
      </IconTile>

      <div className="flex min-w-0 flex-1 flex-col">
        <span className="truncate text-sm text-text-body">
          {lancamento.descricao ?? "Sem descricao"}
        </span>
        <span className="truncate text-[12px] text-text-faint">
          {rotuloData(lancamento.data, new Date())}
          {categoria !== undefined && <span className="hidden md:inline"> · {categoria.nome}</span>}
        </span>
      </div>

      <div className="flex shrink-0 flex-col items-end gap-0.5 md:flex-row md:items-center md:gap-3">
        <span className={cn("text-sm", ehTransferencia ? "text-text-body" : tipoConfig.corValor)}>
          {ehTransferencia ? "" : tipoConfig.sinal}
          {formatarMoeda(lancamento.valor)}
        </span>
        <Badge variant={statusConfig.variant} className="md:min-w-[70px]">
          {statusConfig.label}
        </Badge>
      </div>
    </li>
  )
}
