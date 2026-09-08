import { useState } from "react"
import { RefreshCcw } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { ApiError } from "@/shared/api/client"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Modal } from "@/shared/ui/Modal"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { useDesativarAssinaturaCartao } from "@/features/assinaturas-cartao/hooks/useDesativarAssinaturaCartao"
import { useReativarAssinaturaCartao } from "@/features/assinaturas-cartao/hooks/useReativarAssinaturaCartao"
import { FormAssinaturaCartao } from "@/features/assinaturas-cartao/FormAssinaturaCartao"
import type { AssinaturaCartaoResponse } from "@/features/assinaturas-cartao/types"

type StatusAssinatura = "ATIVA" | "INATIVA"

// Mesmo mapeamento de cor por status usado em ContaFixaItem
// (features/contas-fixas/components/ContaFixaItem.tsx) - ATIVA e o estado
// "em vigor" (gera compra na fatura todo ciclo), INATIVA e neutro.
const CONFIG_POR_STATUS: Record<StatusAssinatura, { label: string; className: string }> = {
  ATIVA: { label: "Ativa", className: "bg-positivo/15 text-positivo" },
  INATIVA: { label: "Inativa", className: "bg-muted text-text-muted" },
}

type AssinaturaCartaoItemProps = {
  assinatura: AssinaturaCartaoResponse
  categoriaNome: string | null
}

// Componente de apresentacao: exibe o que ja vem pronto do backend, com
// acoes de editar (via Modal, mesmo formulario de criacao em modo edicao),
// desativar (com confirmacao inline, ja que interrompe a geracao de novas
// compras) e reativar (gera a compra do mes corrente de novo,
// AssinaturaCartaoService.ReativarAsync).
export function AssinaturaCartaoItem({ assinatura, categoriaNome }: AssinaturaCartaoItemProps) {
  const status: StatusAssinatura = assinatura.ativa ? "ATIVA" : "INATIVA"
  const statusConfig = CONFIG_POR_STATUS[status]

  const [editando, setEditando] = useState(false)
  const [confirmandoDesativacao, setConfirmandoDesativacao] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: desativar, isPending: desativando } = useDesativarAssinaturaCartao()
  const { mutate: reativar, isPending: reativando } = useReativarAssinaturaCartao()

  function confirmarDesativacao() {
    desativar(assinatura.id, {
      onSuccess: () => {
        setErro(null)
        setConfirmandoDesativacao(false)
      },
      onError: (error) => {
        console.error("Falha ao desativar assinatura de cartao", error)
        setErro(
          error instanceof ApiError
            ? error.message
            : "Nao foi possivel desativar a assinatura. Tente novamente.",
        )
        setConfirmandoDesativacao(false)
      },
    })
  }

  function handleReativar() {
    reativar(assinatura.id, {
      onSuccess: () => setErro(null),
      onError: (error) => {
        console.error("Falha ao reativar assinatura de cartao", error)
        setErro(
          error instanceof ApiError
            ? error.message
            : "Nao foi possivel reativar a assinatura. Tente novamente.",
        )
      },
    })
  }

  return (
    <Card size="sm">
      <CardContent className="flex flex-col gap-3">
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-3">
            <div
              className={cn(
                "flex size-[34px] shrink-0 items-center justify-center rounded-[10px]",
                assinatura.ativa ? "bg-accent-deep" : "bg-muted",
              )}
            >
              <RefreshCcw
                className={cn("size-4", assinatura.ativa ? "text-accent-soft" : "text-text-muted")}
                strokeWidth={1.6}
                aria-hidden="true"
              />
            </div>

            <div className="flex flex-col gap-1">
              <span className="text-[19px] font-medium text-text-primary">{assinatura.descricao}</span>
              {categoriaNome && (
                <span className="text-[12px] text-text-muted">{categoriaNome}</span>
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
          <span className="text-[19px] font-medium text-text-primary">
            {formatarMoeda(assinatura.valor)}
          </span>
          <span>Cobra todo dia {assinatura.diaReferencia}</span>
        </div>

        {erro && (
          <Alert variant="destructive">
            <AlertDescription>{erro}</AlertDescription>
          </Alert>
        )}

        <div className="flex items-center justify-end gap-2">
          {assinatura.ativa && (
            <Button type="button" variant="ghost" size="sm" onClick={() => setEditando(true)}>
              Editar
            </Button>
          )}

          {assinatura.ativa ? (
            confirmandoDesativacao ? (
              <div className="flex items-center gap-2 text-[12px] text-text-muted">
                <span className="text-alerta">Desativar interrompe as proximas cobrancas. Confirma?</span>
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
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={desativando}
                onClick={() => setConfirmandoDesativacao(true)}
              >
                Desativar
              </Button>
            )
          ) : (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              disabled={reativando}
              title="Reativar gera a cobranca do mes corrente novamente"
              onClick={handleReativar}
            >
              {reativando ? "Reativando..." : "Reativar"}
            </Button>
          )}
        </div>
      </CardContent>

      <Modal open={editando} onClose={() => setEditando(false)} title="Editar assinatura">
        <FormAssinaturaCartao
          contaId={assinatura.contaId}
          assinaturaParaEditar={assinatura}
          onSalvar={() => setEditando(false)}
        />
      </Modal>
    </Card>
  )
}
