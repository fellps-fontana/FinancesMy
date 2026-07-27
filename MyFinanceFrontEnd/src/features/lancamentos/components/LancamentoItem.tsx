import { useState } from "react"
import { ApiError } from "@/shared/api/client"
import { cn } from "@/shared/lib/utils"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarData } from "@/features/cartao/lib/formatarData"
import { useMarcarComoPago } from "@/features/lancamentos/hooks/useMarcarComoPago"
import { useRemoverLancamento } from "@/features/lancamentos/hooks/useRemoverLancamento"
import type {
  LancamentoResponse,
  StatusLancamento,
  TipoLancamento,
} from "@/features/lancamentos/types"

// Sinal SEMPRE derivado de `tipo`, nunca do sinal cru de `valor`
// (regra-de-negocio.md item 2, CRITICA: "usar SEMPRE o campo tipo... Nunca
// somar valor cru"). O prefixo +/- e o mapeamento de cor abaixo sao so
// apresentacao em cima de `tipo` ja resolvido pelo backend - nenhum calculo
// de dominio novo mora aqui, so a tabela label/cor/sinal (mesmo idioma ja
// usado em CONFIG_POR_STATUS de ContaFixaItem/ContaReceberItem/
// StatusFaturaBadge).
type ConfigTipo = { label: string; className: string; corValor: string; sinal: string }

const CONFIG_POR_TIPO: Record<TipoLancamento, ConfigTipo> = {
  DEBIT: { label: "Saida", className: "bg-negativo/15 text-negativo", corValor: "text-negativo", sinal: "-" },
  CREDIT: { label: "Entrada", className: "bg-positivo/15 text-positivo", corValor: "text-positivo", sinal: "+" },
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

type LancamentoItemProps = {
  lancamento: LancamentoResponse
  onEditar: (lancamento: LancamentoResponse) => void
}

// Componente de apresentacao (burro): so exibe o que ja vem pronto do
// backend. As mutations (marcar como pago / remover) sao acionadas aqui
// porque sao acoes pontuais de item de lista, mesmo padrao de
// ContaFixaItem/ContaReceberItem - "Editar" nao importa FormLancamento
// diretamente (evita acoplar o item ao formulario), so dispara `onEditar`
// pro componente pai decidir onde/como renderizar o form (fora do escopo
// desta task, ver TASK-095).
export function LancamentoItem({ lancamento, onEditar }: LancamentoItemProps) {
  const [confirmandoRemocao, setConfirmandoRemocao] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: marcarComoPago, isPending: marcando } = useMarcarComoPago()
  const { mutate: removerLancamento, isPending: removendo } = useRemoverLancamento()

  const tipoConfig = CONFIG_POR_TIPO[lancamento.tipo]
  const statusConfig = CONFIG_POR_STATUS[lancamento.status]

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
      <CardContent className="flex flex-col gap-2">
        <div className="flex items-start justify-between gap-2">
          <div className="flex flex-col gap-1">
            <span className="text-[19px] font-medium text-text-primary">
              {lancamento.descricao ?? "Sem descricao"}
            </span>
            <div className="flex flex-wrap items-center gap-1.5">
              <span
                className={cn(
                  "inline-flex items-center rounded-[5px] px-2 py-0.5 text-[12px] font-medium",
                  tipoConfig.className,
                )}
              >
                {tipoConfig.label}
              </span>
              {/* Simbolo de origem manual (regra-de-negocio.md item 1: "Todo
                  LANCAMENTO tem a flag manual, exibida como simbolo no UI").
                  So aparece quando manual === true - lancamento nao-manual
                  (Open Finance, fora de escopo v1) fica sem esse indicador. */}
              {lancamento.manual && (
                <span className="inline-flex items-center rounded-[5px] bg-muted px-2 py-0.5 text-[12px] font-medium text-text-muted">
                  Manual
                </span>
              )}
            </div>
          </div>

          <span
            className={cn(
              "inline-flex shrink-0 items-center rounded-[5px] px-2 py-0.5 text-[12px] font-medium",
              statusConfig.className,
            )}
          >
            {statusConfig.label}
          </span>
        </div>

        <div className="flex items-center justify-between text-[13px] text-text-muted">
          <span className={cn("text-[19px] font-medium", tipoConfig.corValor)}>
            {tipoConfig.sinal} {formatarMoeda(lancamento.valor)}
          </span>
          <span>{formatarData(lancamento.data)}</span>
        </div>

        {erro && (
          <Alert variant="destructive">
            <AlertDescription>{erro}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-wrap items-center justify-end gap-2">
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
