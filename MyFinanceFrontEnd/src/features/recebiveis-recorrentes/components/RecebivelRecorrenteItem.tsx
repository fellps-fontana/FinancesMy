import { useState } from "react"
import { CalendarSync, Pencil } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { ConfirmDialog } from "@/shared/ui/confirm-dialog"
import { FormModal } from "@/shared/ui/FormModal"
import { IconTile } from "@/shared/ui/icon-tile"
import { Switch } from "@/shared/ui/switch"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { FormRecebivelRecorrente } from "@/features/recebiveis-recorrentes/FormRecebivelRecorrente"
import { formatarRecorrencia } from "@/features/recebiveis-recorrentes/lib/formatarRecorrencia"
import { useDesativarRecebivelRecorrente } from "@/features/recebiveis-recorrentes/hooks/useDesativarRecebivelRecorrente"
import { useReativarRecebivelRecorrente } from "@/features/recebiveis-recorrentes/hooks/useReativarRecebivelRecorrente"
import type { RecebivelRecorrenteResponse } from "@/features/recebiveis-recorrentes/types"

// Colunas da tabela desktop (Descricao / Recorrencia / Valor / Ativa / acoes),
// mesma grade de Contas fixas. Exportada para o cabecalho da lista.
export const GRADE_COLUNAS_RECEBIVEL = "md:grid-cols-[2.4fr_1fr_1fr_0.6fr_64px]"

type RecebivelRecorrenteItemProps = {
  recebivel: RecebivelRecorrenteResponse
  // Nome resolvido pelo container (Response so traz categoriaId); null sem categoria.
  categoriaNome: string | null
}

// Linha do molde (regra-de-negocio.md item 15). Desktop: linha de tabela;
// mobile: card. O switch desativa (com confirmacao, pois exclui ocorrencias
// pendentes ja geradas) ou reativa. Nenhum calculo de dominio mora aqui.
export function RecebivelRecorrenteItem({ recebivel, categoriaNome }: RecebivelRecorrenteItemProps) {
  const [editando, setEditando] = useState(false)
  const [confirmandoDesativacao, setConfirmandoDesativacao] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: desativar, isPending: desativando } = useDesativarRecebivelRecorrente()
  const { mutate: reativar, isPending: reativando } = useReativarRecebivelRecorrente()

  function handleAlternarAtiva(ativa: boolean) {
    if (!ativa) {
      setConfirmandoDesativacao(true)
      return
    }
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

  return (
    <li className="flex flex-col gap-2 rounded-[14px] border-[0.5px] border-border bg-card p-3.5 md:rounded-none md:border-0 md:border-b-[0.5px] md:bg-transparent md:px-[22px] md:last:border-b-0">
      <div className={cn("flex items-center gap-3 md:grid", GRADE_COLUNAS_RECEBIVEL)}>
        <div className="flex min-w-0 flex-1 items-center gap-3">
          <IconTile className={cn(!recebivel.ativa && "bg-muted text-text-muted")}>
            <CalendarSync className="size-4" strokeWidth={1.6} aria-hidden="true" />
          </IconTile>
          <div className="flex min-w-0 flex-col">
            <span className="truncate text-sm text-text-body">{recebivel.descricao}</span>
            <span className="truncate text-xs text-text-faint md:hidden">
              {formatarRecorrencia(recebivel)}
              {categoriaNome ? ` - ${categoriaNome}` : ""}
            </span>
            {categoriaNome && (
              <span className="hidden truncate text-xs text-text-faint md:block">{categoriaNome}</span>
            )}
          </div>
        </div>

        <span className="hidden truncate text-[13px] text-text-muted md:block">
          {formatarRecorrencia(recebivel)}
        </span>

        <span
          className={cn(
            "text-sm whitespace-nowrap md:text-right",
            recebivel.ativa ? "text-text-primary" : "text-text-faint",
          )}
        >
          {formatarMoeda(recebivel.valor)}
        </span>

        <div className="flex justify-end">
          <Switch
            checked={recebivel.ativa}
            onCheckedChange={handleAlternarAtiva}
            disabled={desativando || reativando}
            label={recebivel.ativa ? "Desativar recebivel recorrente" : "Reativar recebivel recorrente"}
            className="w-auto [&>span:first-child]:sr-only"
          />
        </div>

        <div className="flex items-center justify-end gap-1">
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label={`Editar ${recebivel.descricao}`}
            title="Editar"
            onClick={() => setEditando(true)}
          >
            <Pencil className="size-4 text-text-muted" aria-hidden="true" />
          </Button>
        </div>
      </div>

      {erro && (
        <Alert variant="destructive">
          <AlertDescription>{erro}</AlertDescription>
        </Alert>
      )}

      <FormModal open={editando} onClose={() => setEditando(false)} title="Editar recebível recorrente">
        <FormRecebivelRecorrente
          recebivelParaEditar={recebivel}
          onSalvar={() => setEditando(false)}
          onCancelar={() => setEditando(false)}
        />
      </FormModal>

      <ConfirmDialog
        open={confirmandoDesativacao}
        title={`Desativar ${recebivel.descricao}?`}
        description="Desativar exclui as ocorrencias pendentes ja geradas. Voce pode reativar depois."
        icon={<CalendarSync className="size-4" strokeWidth={1.6} />}
        confirmLabel="Desativar"
        destructive
        loading={desativando}
        onConfirm={confirmarDesativacao}
        onCancel={() => setConfirmandoDesativacao(false)}
      />
    </li>
  )
}
