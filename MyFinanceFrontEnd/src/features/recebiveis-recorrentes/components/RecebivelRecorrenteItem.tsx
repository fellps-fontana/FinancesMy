import { useState } from "react"
import { CalendarSync } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { ApiError } from "@/shared/api/client"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Modal } from "@/shared/ui/Modal"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { FormRecebivelRecorrente } from "@/features/recebiveis-recorrentes/FormRecebivelRecorrente"
import { formatarRecorrencia } from "@/features/recebiveis-recorrentes/lib/formatarRecorrencia"
import { useDesativarRecebivelRecorrente } from "@/features/recebiveis-recorrentes/hooks/useDesativarRecebivelRecorrente"
import { useReativarRecebivelRecorrente } from "@/features/recebiveis-recorrentes/hooks/useReativarRecebivelRecorrente"
import type { RecebivelRecorrenteResponse } from "@/features/recebiveis-recorrentes/types"

type StatusRecebivelRecorrente = "ATIVA" | "INATIVA"

// Cor por status ativa/inativa (regra-de-negocio.md item 15). Nao ha token
// proprio para este estado em identidade-visual.md (que cobre
// pago/pendente/manual/sugerido) - segue o mesmo criterio de ContaFixaItem:
// ATIVA e o estado "em vigor" (gera ocorrencia de entrada esperada -
// positivo, familia de "recebido"), INATIVA e neutro (bg-muted + text-muted,
// o par que identidade-visual.md reserva para "manual -> neutro"). Nao usa
// "alerta" porque inativa e escolha intencional do usuario, nao pendencia.
const CONFIG_POR_STATUS: Record<
  StatusRecebivelRecorrente,
  { label: string; className: string }
> = {
  ATIVA: { label: "Ativo", className: "bg-positivo/15 text-positivo" },
  INATIVA: { label: "Inativo", className: "bg-muted text-text-muted" },
}

type RecebivelRecorrenteItemProps = {
  recebivel: RecebivelRecorrenteResponse
  // Nome resolvido pelo container a partir de categoriaId (mesmo motivo de
  // ContaFixaItem: Response so traz o id). null quando o molde nao tem
  // categoria vinculada (FK opcional, item 15) - a categoria some da UI, sem
  // texto placeholder.
  categoriaNome: string | null
}

// Componente de apresentacao: exibe o que ja vem pronto do backend
// (descricao, valor, periodicidade + campos de ancora, ativa) mais o nome de
// categoria resolvido via prop. formatarMoeda (locale pt-BR) e
// formatarRecorrencia (rotulo do molde) sao apresentacao pura, nao regra de
// dominio. Nenhum calculo de proxima ocorrencia mora aqui - o backend
// materializa; as ocorrencias em si aparecem na tela de Contas a Receber.
// Guarda o estado de UI de dois toggles: abrir o form de edicao (Modal) e
// confirmar a desativacao inline.
export function RecebivelRecorrenteItem({
  recebivel,
  categoriaNome,
}: RecebivelRecorrenteItemProps) {
  const status: StatusRecebivelRecorrente = recebivel.ativa ? "ATIVA" : "INATIVA"
  const statusConfig = CONFIG_POR_STATUS[status]

  const [editando, setEditando] = useState(false)
  // Desativar exclui as ocorrencias PENDENTE ja geradas (item 15) - acao
  // destrutiva, por isso confirmacao inline antes da mutation. Reativar so
  // gera ocorrencia nova - clique direto, avisando via title.
  const [confirmandoDesativacao, setConfirmandoDesativacao] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: desativar, isPending: desativando } = useDesativarRecebivelRecorrente()
  const { mutate: reativar, isPending: reativando } = useReativarRecebivelRecorrente()

  function confirmarDesativacao() {
    desativar(recebivel.id, {
      onSuccess: () => {
        setErro(null)
        setConfirmandoDesativacao(false)
      },
      onError: (error) => {
        console.error("Falha ao desativar recebivel recorrente", error)
        setErro(
          error instanceof ApiError
            ? error.message
            : "Nao foi possivel desativar o recebivel recorrente. Tente novamente.",
        )
        setConfirmandoDesativacao(false)
      },
    })
  }

  function handleReativar() {
    reativar(recebivel.id, {
      onSuccess: () => setErro(null),
      onError: (error) => {
        console.error("Falha ao reativar recebivel recorrente", error)
        setErro(
          error instanceof ApiError
            ? error.message
            : "Nao foi possivel reativar o recebivel recorrente. Tente novamente.",
        )
      },
    })
  }

  return (
    <Card size="sm">
      <CardContent className="flex flex-col gap-3">
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-3">
            {/* Icone de recorrencia (identidade-visual.md: "quadrado
                arredondado 34px com icone dentro"). Um unico icone - nao ha
                campo de dominio que justifique variacao. A cor reflete
                `ativa`, que e dado real. */}
            <div
              className={cn(
                "flex size-[34px] shrink-0 items-center justify-center rounded-[10px]",
                recebivel.ativa ? "bg-accent-deep" : "bg-muted",
              )}
            >
              <CalendarSync
                className={cn(
                  "size-4",
                  recebivel.ativa ? "text-accent-soft" : "text-text-muted",
                )}
                strokeWidth={1.6}
                aria-hidden="true"
              />
            </div>

            <div className="flex flex-col gap-1">
              <span className="text-[19px] font-medium text-text-primary">
                {recebivel.descricao}
              </span>
              <div className="flex flex-wrap items-center gap-1.5">
                <span className="inline-flex items-center rounded-[5px] bg-muted px-2 py-0.5 text-[12px] font-medium text-text-muted">
                  {formatarRecorrencia(recebivel)}
                </span>
                {categoriaNome && (
                  <span className="text-[12px] text-text-muted">{categoriaNome}</span>
                )}
              </div>
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

        <span className="text-[19px] font-medium text-text-primary">
          {formatarMoeda(recebivel.valor)}
        </span>

        {erro && (
          <Alert variant="destructive">
            <AlertDescription>{erro}</AlertDescription>
          </Alert>
        )}

        {recebivel.ativa ? (
          confirmandoDesativacao ? (
            <div className="flex flex-wrap items-center justify-end gap-2 text-[12px] text-text-muted">
              <span className="text-alerta">
                Desativar exclui as ocorrências pendentes já geradas. Confirma?
              </span>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={desativando}
                onClick={() => setConfirmandoDesativacao(false)}
              >
                Cancelar
              </Button>
              <Button
                type="button"
                variant="destructive"
                size="sm"
                disabled={desativando}
                onClick={confirmarDesativacao}
              >
                {desativando ? "Desativando..." : "Sim, desativar"}
              </Button>
            </div>
          ) : (
            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => setEditando(true)}
              >
                Editar
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={desativando}
                onClick={() => setConfirmandoDesativacao(true)}
              >
                Desativar
              </Button>
            </div>
          )
        ) : (
          <div className="flex justify-end">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              disabled={reativando}
              title="Reativar volta a gerar as ocorrências pendentes do molde"
              onClick={handleReativar}
            >
              {reativando ? "Reativando..." : "Reativar"}
            </Button>
          </div>
        )}
      </CardContent>

      <Modal open={editando} onClose={() => setEditando(false)} title="Editar recebível recorrente">
        <FormRecebivelRecorrente
          recebivelParaEditar={recebivel}
          onSalvar={() => setEditando(false)}
        />
      </Modal>
    </Card>
  )
}
