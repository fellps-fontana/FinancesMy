import { useState } from "react"
import { ArrowDownRight, ArrowUpRight } from "lucide-react"
import { ApiError } from "@/shared/api/client"
import { cn } from "@/shared/lib/utils"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarData } from "@/features/cartao/lib/formatarData"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { useMarcarComoPago } from "@/features/lancamentos/hooks/useMarcarComoPago"
import { useRemoverLancamento } from "@/features/lancamentos/hooks/useRemoverLancamento"
import type {
  LancamentoResponse,
  StatusLancamento,
  TipoLancamento,
} from "@/features/lancamentos/types"
import type { CategoriaResponse, TipoCategoria } from "@/features/categorias/types"

// Sinal SEMPRE derivado de `tipo`, nunca do sinal cru de `valor`
// (regra-de-negocio.md item 2, CRITICA: "usar SEMPRE o campo tipo... Nunca
// somar valor cru"). O icone do avatar tambem segue esse mesmo campo - mesmo
// espirito de ICONE_POR_TIPO em AtivoCard.tsx (identidade-visual.md, "Icone
// de status/categoria... significado de dominio, nao enfeite"): seta pra
// cima = entrada, seta pra baixo = saida. `tipoCategoria` alimenta
// useCategorias (Despesa/Receita, regra-de-negocio.md item 7) so pra
// resolver o NOME da categoria do lancamento - o backend nao expoe um icone
// por categoria, entao nao inventamos um (decoracao sem base em dado real
// violaria identidade-visual.md "cor/icone carrega significado, nao e
// enfeite").
type ConfigTipo = { corValor: string; sinal: string; tipoCategoria: TipoCategoria; icone: typeof ArrowUpRight }

const CONFIG_POR_TIPO: Record<TipoLancamento, ConfigTipo> = {
  DEBIT: { corValor: "text-negativo", sinal: "-", tipoCategoria: "Despesa", icone: ArrowDownRight },
  CREDIT: { corValor: "text-positivo", sinal: "+", tipoCategoria: "Receita", icone: ArrowUpRight },
}

// Status (regra-de-negocio.md item 5): PENDENTE -> PAGO e o unico caminho
// manual em v1. SUGERIDO so existe quando a conciliacao Open Finance entrar
// em v2 (nenhum form desta task oferece SUGERIDO como opcao), mas o badge
// precisa cobrir os 3 valores do tipo StatusLancamento pra nao quebrar em
// tempo de exibicao caso o dado chegue assim de outra origem. Cores conforme
// identidade-visual.md ("pago -> positivo; pendente -> alerta; sugerido ->
// accent" - roxo real). O token shadcn "accent" NAO e o roxo neste projeto
// (superficie neutra escura, ver index.css); "--primary" e o roxo de fato,
// mesmo mapeamento ja usado em ContaReceberItem/StatusFaturaBadge pro estado
// "em andamento"/intermediario.
const CONFIG_POR_STATUS: Record<StatusLancamento, { label: string; className: string }> = {
  PENDENTE: { label: "Pendente", className: "bg-alerta/15 text-alerta" },
  SUGERIDO: { label: "Sugerido", className: "bg-primary/15 text-primary" },
  PAGO: { label: "Pago", className: "bg-positivo/15 text-positivo" },
}

// Resolve o NOME da categoria (achatando categoria + subcategoria direta,
// mesmo universo ja percorrido por CategoriaSelect.tsx/achatarCategorias) so
// pra exibicao no item da lista (mockup "04 Lancamentos": nome da categoria
// abaixo da descricao). Nao e calculo de dominio - e um lookup id -> nome
// sobre uma lista ja carregada; `categorias` chega filtrada por tipo
// (Despesa/Receita) direto do backend via useCategorias(tipo).
function encontrarNomeCategoria(
  categorias: CategoriaResponse[] | undefined,
  categoriaId: string | null,
): string | undefined {
  if (!categorias || !categoriaId) {
    return undefined
  }

  for (const categoria of categorias) {
    if (categoria.id === categoriaId) {
      return categoria.nome
    }

    const subcategoria = categoria.subcategorias.find((item) => item.id === categoriaId)
    if (subcategoria) {
      return subcategoria.nome
    }
  }

  return undefined
}

type LancamentoItemProps = {
  lancamento: LancamentoResponse
  onEditar: (lancamento: LancamentoResponse) => void
}

// Componente de apresentacao (burro): so exibe o que ja vem pronto do
// backend. As mutations (marcar como pago / remover) sao acionadas aqui
// porque sao acoes pontuais de item de lista, mesmo padrao de
// ContaFixaItem/ContaReceberItem - "Editar" nao importa FormLancamento
// diretamente (evita acoplar o item ao formulario), so dispara `onEditar`
// pro componente pai decidir onde/como renderizar o form.
//
// Layout redesenhado conforme mockup "04 Lancamentos.dc.html": avatar de
// icone (accent-deep/accent-soft, mesmo padrao de AtivoCard.tsx) + descricao
// + categoria numa linha, valor colorido + badge de status na outra ponta.
// O badge redundante "Entrada/Saida" do layout anterior foi removido - o
// sinal (+/-) e a cor do valor ja comunicam a mesma informacao (regra-de-
// negocio.md item 2) de forma mais compacta, igual ao mockup. Acoes (marcar
// como pago/editar/remover) continuam TODAS presentes, so reagrupadas numa
// faixa inferior separada por borda - nenhuma funcionalidade foi removida,
// so reorganizada visualmente.
export function LancamentoItem({ lancamento, onEditar }: LancamentoItemProps) {
  const [confirmandoRemocao, setConfirmandoRemocao] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: marcarComoPago, isPending: marcando } = useMarcarComoPago()
  const { mutate: removerLancamento, isPending: removendo } = useRemoverLancamento()

  const tipoConfig = CONFIG_POR_TIPO[lancamento.tipo]
  const statusConfig = CONFIG_POR_STATUS[lancamento.status]
  const IconeTipo = tipoConfig.icone

  const { data: categorias } = useCategorias(tipoConfig.tipoCategoria)
  const nomeCategoria = encontrarNomeCategoria(categorias, lancamento.categoriaId)

  function handleMarcarComoPago() {
    marcarComoPago(
      { contaId: lancamento.contaId, lancamentoId: lancamento.id },
      {
        onSuccess: () => setErro(null),
        onError: (error) => {
          console.error("Falha ao marcar lancamento como pago", error)
          setErro(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel marcar o lancamento como pago. Tente novamente.",
          )
        },
      },
    )
  }

  function confirmarRemocao() {
    removerLancamento(
      { contaId: lancamento.contaId, lancamentoId: lancamento.id },
      {
        onSuccess: () => {
          setErro(null)
          setConfirmandoRemocao(false)
        },
        onError: (error) => {
          console.error("Falha ao remover lancamento", error)
          setErro(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel remover o lancamento. Tente novamente.",
          )
          setConfirmandoRemocao(false)
        },
      },
    )
  }

  return (
    <Card size="sm">
      <CardContent className="flex flex-col gap-3">
        <div className="flex items-center gap-3">
          <div className="flex size-[34px] shrink-0 items-center justify-center rounded-lg bg-accent-deep">
            <IconeTipo className="size-4 text-accent-soft" strokeWidth={1.6} aria-hidden="true" />
          </div>

          <div className="flex min-w-0 flex-1 flex-col">
            <span className="truncate text-[14px] text-text-body">
              {lancamento.descricao ?? "Sem descricao"}
            </span>
            <div className="flex flex-wrap items-center gap-1.5">
              <span className="truncate text-[12px] text-text-faint">
                {nomeCategoria ?? "Sem categoria"}
              </span>
              {/* Simbolo de origem manual (regra-de-negocio.md item 1: "Todo
                  LANCAMENTO tem a flag manual, exibida como simbolo no UI").
                  So aparece quando manual === true. */}
              {lancamento.manual && (
                <span className="inline-flex items-center rounded-[5px] bg-muted px-1.5 py-0.5 text-[11px] font-medium text-text-muted">
                  Manual
                </span>
              )}
            </div>
          </div>

          <div className="flex shrink-0 flex-col items-end gap-1 text-right">
            <span className={cn("text-[14px] font-medium", tipoConfig.corValor)}>
              {tipoConfig.sinal} {formatarMoeda(lancamento.valor)}
            </span>
            <span
              className={cn(
                "inline-flex items-center rounded-[5px] px-2 py-0.5 text-[11px] font-medium",
                statusConfig.className,
              )}
            >
              {statusConfig.label}
            </span>
          </div>
        </div>

        <span className="text-[12px] text-text-faint">{formatarData(lancamento.data)}</span>

        {erro && (
          <Alert variant="destructive">
            <AlertDescription>{erro}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-wrap items-center justify-end gap-2 border-t border-border pt-2.5">
          {confirmandoRemocao ? (
            <>
              <span className="mr-auto text-[12px] text-alerta">Remover este lancamento?</span>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={removendo}
                onClick={() => setConfirmandoRemocao(false)}
              >
                Cancelar
              </Button>
              <Button
                type="button"
                variant="destructive"
                size="sm"
                disabled={removendo}
                onClick={confirmarRemocao}
              >
                {removendo ? "Removendo..." : "Sim, remover"}
              </Button>
            </>
          ) : (
            <>
              {lancamento.status === "PENDENTE" && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={marcando}
                  onClick={handleMarcarComoPago}
                >
                  {marcando ? "Marcando..." : "Marcar como pago"}
                </Button>
              )}
              <Button type="button" variant="ghost" size="sm" onClick={() => onEditar(lancamento)}>
                Editar
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => setConfirmandoRemocao(true)}
              >
                Remover
              </Button>
            </>
          )}
        </div>
      </CardContent>
    </Card>
  )
}
