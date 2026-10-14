import { Check, Pencil, Trash2 } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { Badge, type BadgeVariant } from "@/shared/ui/badge"
import { Button } from "@/shared/ui/button"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { formatarDiaMes } from "@/shared/lib/datas"
import { obterIconeCatalogo } from "@/shared/lib/catalogoIcones"
import { resolverDisponibilidadeDeEdicao } from "@/features/lancamentos/lib/dicaDeEdicaoLancamento"
import type { CategoriaResolvida } from "@/features/categorias/lib/construirMapaCategorias"
import type {
  LancamentoResponse,
  StatusLancamento,
  TipoLancamento,
} from "@/features/lancamentos/types"

// Colunas da tabela desktop (mockup s_lancamentos: Descricao / Categoria /
// Data / Valor / acoes). Exportado para cabecalho e demais linhas da lista
// usarem a mesma grade. A coluna de acoes e 96px (o mockup tem 64px) para
// caber o atalho "Marcar como pago", que o mockup omite mas o item 5 exige.
export const GRADE_COLUNAS_LANCAMENTO = "md:grid-cols-[2.4fr_1fr_1fr_1fr_96px]"

// Sinal e cor derivam SEMPRE de `tipo` (item 2, CRITICA), nunca do sinal de `valor`.
const CONFIG_POR_TIPO: Record<TipoLancamento, { cor: string; sinal: string }> = {
  DEBIT: { cor: "text-negativo", sinal: "-" },
  CREDIT: { cor: "text-positivo", sinal: "+" },
}

// Badge de status (item 5): pago -> positivo; pendente -> alerta; sugerido ->
// roxo (so aparece se o backend enviar SUGERIDO, v2).
const CONFIG_POR_STATUS: Record<StatusLancamento, { label: string; variant: BadgeVariant }> = {
  PAGO: { label: "Pago", variant: "pago" },
  PENDENTE: { label: "Pendente", variant: "pendente" },
  SUGERIDO: { label: "Sugerido", variant: "sugerido" },
}

type LancamentoItemProps = {
  lancamento: LancamentoResponse
  categoria: CategoriaResolvida | null
  // Existe ao menos uma parcela PENDENTE na compra (item 17); informado pelo container.
  podeCancelarCompra: boolean
  marcandoComoPago: boolean
  onEditar: (lancamento: LancamentoResponse) => void
  onExcluir: (lancamento: LancamentoResponse) => void
  onCancelarCompra: (lancamento: LancamentoResponse) => void
  onMarcarComoPago: (lancamento: LancamentoResponse) => void
  // Mobile: toque na linha abre a folha de acoes.
  onAbrirAcoes: (lancamento: LancamentoResponse) => void
}

const CLASSE_ICONE_ACAO = "size-4 text-text-muted"

// Linha de lancamento (apresentacao). Habilitacao de Editar/Excluir vem de
// `avulso` do backend (item 18); parcela mostra "n/N" (item 17) e a acao de
// cancelar a compra no lugar de editar/excluir, como no mockup.
export function LancamentoItem({
  lancamento,
  categoria,
  podeCancelarCompra,
  marcandoComoPago,
  onEditar,
  onExcluir,
  onCancelarCompra,
  onMarcarComoPago,
  onAbrirAcoes,
}: LancamentoItemProps) {
  const tipoConfig = CONFIG_POR_TIPO[lancamento.tipo]
  const statusConfig = CONFIG_POR_STATUS[lancamento.status]
  const disponibilidade = resolverDisponibilidadeDeEdicao(lancamento)
  const IconeCategoria = obterIconeCatalogo(categoria?.icone)
  const descricao = lancamento.descricao ?? "Sem descricao"
  const nomeCategoria = categoria?.nome ?? "Sem categoria"
  const ehParcela = lancamento.compraParceladaId !== null
  const mostrarCancelarCompra = ehParcela && podeCancelarCompra

  return (
    <li className="relative md:border-b-[0.5px] md:border-border md:last:border-b-0 py-1.5 md:px-[22px] md:py-3.5">
      <div className={cn("flex items-center gap-3 md:grid", GRADE_COLUNAS_LANCAMENTO)}>
        <div className="flex min-w-0 flex-1 items-center gap-3">
          <div className="flex size-[34px] shrink-0 items-center justify-center rounded-[10px] bg-accent-deep">
            <IconeCategoria className="size-4 text-accent-soft" strokeWidth={1.6} aria-hidden="true" />
          </div>
          <div className="flex min-w-0 flex-col">
            <div className="flex items-center gap-1.5">
              <span className="truncate text-sm text-text-body">{descricao}</span>
              {ehParcela && lancamento.parcelaNumero !== null && (
                <Badge variant="parcela">
                  {lancamento.parcelaNumero}/{lancamento.totalParcelas}
                </Badge>
              )}
            </div>
            <span className="truncate text-xs text-text-faint md:hidden">{nomeCategoria}</span>
          </div>
        </div>

        <span className="hidden truncate text-[13px] text-text-muted md:block">{nomeCategoria}</span>
        <span className="hidden text-[13px] text-text-muted md:block">
          {formatarDiaMes(lancamento.data)}
        </span>

        <div className="flex flex-col items-end gap-[3px] md:flex-row md:items-center md:justify-end md:gap-2">
          <span className={cn("text-sm", tipoConfig.cor)}>
            {tipoConfig.sinal}
            {formatarMoeda(lancamento.valor)}
          </span>
          <Badge variant={statusConfig.variant}>{statusConfig.label}</Badge>
        </div>

        <div className="relative z-10 hidden items-center justify-end gap-1 md:flex">
          {lancamento.status === "PENDENTE" && (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              aria-label={`Marcar ${descricao} como pago`}
              title="Marcar como pago"
              disabled={marcandoComoPago}
              onClick={() => onMarcarComoPago(lancamento)}
            >
              <Check className="size-4 text-positivo" aria-hidden="true" />
            </Button>
          )}

          {ehParcela ? (
            mostrarCancelarCompra && (
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                aria-label={`Cancelar compra parcelada ${descricao}`}
                title="Cancelar compra parcelada"
                onClick={() => onCancelarCompra(lancamento)}
              >
                <Trash2 className={CLASSE_ICONE_ACAO} aria-hidden="true" />
              </Button>
            )
          ) : (
            <>
              {/* title no wrapper: botao desabilitado nao recebe hover. */}
              <span title={disponibilidade.dica ?? "Editar"}>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-sm"
                  aria-label={`Editar ${descricao}`}
                  disabled={!disponibilidade.podeEditarExcluir}
                  onClick={() => onEditar(lancamento)}
                >
                  <Pencil className={CLASSE_ICONE_ACAO} aria-hidden="true" />
                </Button>
              </span>
              <span title={disponibilidade.dica ?? "Excluir"}>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-sm"
                  aria-label={`Excluir ${descricao}`}
                  disabled={!disponibilidade.podeEditarExcluir}
                  onClick={() => onExcluir(lancamento)}
                >
                  <Trash2 className={CLASSE_ICONE_ACAO} aria-hidden="true" />
                </Button>
              </span>
            </>
          )}
        </div>
      </div>

      {/* Mobile: a linha inteira abre a folha de acoes (mockup sheet:lanc:*). */}
      <button
        type="button"
        aria-label={`Acoes de ${descricao}`}
        onClick={() => onAbrirAcoes(lancamento)}
        className="absolute inset-0 md:hidden"
      />
    </li>
  )
}
