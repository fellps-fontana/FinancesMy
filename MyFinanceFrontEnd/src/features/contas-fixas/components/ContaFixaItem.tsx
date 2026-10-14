import { useState } from "react"
import { Pencil, Repeat, Trash2 } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { FormModal } from "@/shared/ui/FormModal"
import { Button } from "@/shared/ui/button"
import { ConfirmDialog } from "@/shared/ui/confirm-dialog"
import { IconTile } from "@/shared/ui/icon-tile"
import { Switch } from "@/shared/ui/switch"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { FormContaFixa } from "@/features/contas-fixas/FormContaFixa"
import { useApagarContaFixa } from "@/features/contas-fixas/hooks/useApagarContaFixa"
import { useDesativarContaFixa } from "@/features/contas-fixas/hooks/useDesativarContaFixa"
import { useReativarContaFixa } from "@/features/contas-fixas/hooks/useReativarContaFixa"
import type { ContaFixaResponse, PeriodicidadeContaFixa } from "@/features/contas-fixas/types"

// Colunas da tabela no desktop (mockup s_fixa: Descricao / Recorrencia /
// Valor / Ativa / acoes). Exportado para o cabecalho da lista usar a mesma
// grade e as colunas ficarem alinhadas.
export const GRADE_COLUNAS_CONTA_FIXA = "md:grid-cols-[2.4fr_1fr_1fr_0.6fr_64px]"

const LABEL_POR_PERIODICIDADE: Record<PeriodicidadeContaFixa, string> = {
  MENSAL: "Mensal",
  ANUAL: "Anual",
}

// Texto "Mensal - dia 5" (mockup). O backend nao devolve o mes da conta fixa
// anual (types.ts so tem diaVencimento), entao "Anual" mostra so o dia.
function formatarRecorrencia(contaFixa: ContaFixaResponse): string {
  return `${LABEL_POR_PERIODICIDADE[contaFixa.periodicidade]} - dia ${contaFixa.diaVencimento}`
}

type AcaoPendente = "desativar" | "apagar" | null

type ContaFixaItemProps = {
  contaFixa: ContaFixaResponse
  // Nome da categoria resolvido pelo container; null sem categoria vinculada.
  categoriaNome: string | null
}

function mensagemDeErro(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.message : fallback
}

// Linha da tabela de contas fixas (regra-de-negocio.md item 6). Estado
// ativa/inativa vem do backend; o switch dispara desativar (com confirmacao,
// pois exclui lancamentos pendentes) ou reativar. Apagar e hard delete do
// molde, sem desfazer. Nenhum calculo de dominio mora aqui.
export function ContaFixaItem({ contaFixa, categoriaNome }: ContaFixaItemProps) {
  const [acaoPendente, setAcaoPendente] = useState<AcaoPendente>(null)
  const [editando, setEditando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: desativar, isPending: desativando } = useDesativarContaFixa()
  const { mutate: reativar, isPending: reativando } = useReativarContaFixa()
  const { mutate: apagar, isPending: apagando } = useApagarContaFixa()

  function handleAlternarAtiva(ativa: boolean) {
    if (!ativa) {
      setAcaoPendente("desativar")
      return
    }
    reativar(contaFixa.id, {
      onSuccess: () => setErro(null),
      onError: (error) => {
        console.error("Falha ao reativar conta fixa", error)
        setErro(mensagemDeErro(error, "Nao foi possivel reativar a conta fixa. Tente novamente."))
      },
    })
  }

  function confirmarDesativacao() {
    desativar(contaFixa.id, {
      onSuccess: () => {
        setErro(null)
        setAcaoPendente(null)
      },
      onError: (error) => {
        console.error("Falha ao desativar conta fixa", error)
        setErro(mensagemDeErro(error, "Nao foi possivel desativar a conta fixa. Tente novamente."))
        setAcaoPendente(null)
      },
    })
  }

  function confirmarApagar() {
    apagar(contaFixa.id, {
      onSuccess: () => {
        setErro(null)
        setAcaoPendente(null)
      },
      onError: (error) => {
        console.error("Falha ao apagar conta fixa", error)
        setErro(mensagemDeErro(error, "Nao foi possivel apagar a conta fixa. Tente novamente."))
        setAcaoPendente(null)
      },
    })
  }

  return (
    <li className="flex flex-col gap-2 rounded-[14px] border-[0.5px] border-border bg-card p-3.5 md:rounded-none md:border-0 md:border-b-[0.5px] md:bg-transparent md:px-[22px] md:last:border-b-0">
      <div className={cn("flex items-center gap-3 md:grid", GRADE_COLUNAS_CONTA_FIXA)}>
        <div className="flex min-w-0 flex-1 items-center gap-3">
          <IconTile className={cn(!contaFixa.ativa && "bg-muted text-text-muted")}>
            <Repeat className="size-4" strokeWidth={1.6} aria-hidden="true" />
          </IconTile>
          <div className="flex min-w-0 flex-col">
            <span className="truncate text-sm text-text-body">{contaFixa.descricao}</span>
            <span className="truncate text-xs text-text-faint md:hidden">
              {formatarRecorrencia(contaFixa)}
              {categoriaNome ? ` - ${categoriaNome}` : ""}
            </span>
            {categoriaNome && (
              <span className="hidden truncate text-xs text-text-faint md:block">{categoriaNome}</span>
            )}
          </div>
        </div>

        <span className="hidden text-[13px] whitespace-nowrap text-text-muted md:block">
          {formatarRecorrencia(contaFixa)}
        </span>

        <span
          className={cn(
            "text-sm whitespace-nowrap md:text-right",
            contaFixa.ativa ? "text-text-primary" : "text-text-faint",
          )}
        >
          {formatarMoeda(contaFixa.valor)}
        </span>

        <div className="flex justify-end">
          <Switch
            checked={contaFixa.ativa}
            onCheckedChange={handleAlternarAtiva}
            disabled={desativando || reativando}
            label={contaFixa.ativa ? "Desativar conta fixa" : "Reativar conta fixa"}
            className="w-auto [&>span:first-child]:sr-only"
          />
        </div>

        <div className="flex items-center justify-end gap-1">
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label={`Editar ${contaFixa.descricao}`}
            title="Editar"
            onClick={() => setEditando(true)}
          >
            <Pencil className="size-4 text-text-muted" aria-hidden="true" />
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label={`Apagar ${contaFixa.descricao}`}
            title="Apagar"
            onClick={() => setAcaoPendente("apagar")}
          >
            <Trash2 className="size-4 text-negativo" aria-hidden="true" />
          </Button>
        </div>
      </div>

      {erro && (
        <Alert variant="destructive">
          <AlertDescription>{erro}</AlertDescription>
        </Alert>
      )}

      <FormModal open={editando} onClose={() => setEditando(false)} title="Editar conta fixa">
        <FormContaFixa
          contaFixaParaEditar={contaFixa}
          onSalvar={() => setEditando(false)}
          onCancelar={() => setEditando(false)}
        />
      </FormModal>

      <ConfirmDialog
        open={acaoPendente === "desativar"}
        title={`Desativar ${contaFixa.descricao}?`}
        description="Desativar exclui os lancamentos pendentes ja gerados. Os pagos permanecem. Voce pode reativar depois."
        icon={<Repeat className="size-4" strokeWidth={1.6} />}
        confirmLabel="Desativar"
        destructive
        loading={desativando}
        onConfirm={confirmarDesativacao}
        onCancel={() => setAcaoPendente(null)}
      />

      <ConfirmDialog
        open={acaoPendente === "apagar"}
        title={`Apagar ${contaFixa.descricao}?`}
        description="Os lancamentos pendentes serao excluidos. Os ja pagos permanecem como lancamentos avulsos. Nao e possivel desfazer."
        icon={<Trash2 className="size-4" strokeWidth={1.6} />}
        confirmLabel="Apagar"
        destructive
        loading={apagando}
        onConfirm={confirmarApagar}
        onCancel={() => setAcaoPendente(null)}
      />
    </li>
  )
}
