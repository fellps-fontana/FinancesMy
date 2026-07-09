import type { FormEvent } from "react"
import { X } from "lucide-react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"

type PagarFaturaModalProps = {
  valorPendente: number
  contaOrigemId: string
  valor: string
  data: string
  isSubmitting: boolean
  errorMessage: string | null
  onContaOrigemIdChange: (value: string) => void
  onValorChange: (value: string) => void
  onDataChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onFechar: () => void
}

/**
 * Formulario de pagamento de fatura (regra de negocio item 12: o pagamento
 * pode ser ANTECIPADO - fatura ainda ABERTA - e PARCIAL, varios pagamentos
 * ate quitar o saldo pendente). O saldo pendente fica visivel no topo e o
 * campo "Valor a pagar" vem pre-preenchido com ele mas continua editavel
 * para um valor MENOR - e essa a UX que deixa claro que pagamento parcial e
 * permitido, preservando a intencao da branch de referencia.
 *
 * Puramente apresentacao: guarda so o estado transiente exibido; a mutation,
 * o estado de envio e o erro vem do container (FaturasSection).
 *
 * GAP CONHECIDO: nao ha endpoint para listar contas BANCO (mesma limitacao
 * de GET /api/contas registrada em hooks/useContaCartaoAtual.ts) - a conta
 * de origem e informada como texto livre (id da conta), ate esse endpoint
 * existir.
 */
export function PagarFaturaModal({
  valorPendente,
  contaOrigemId,
  valor,
  data,
  isSubmitting,
  errorMessage,
  onContaOrigemIdChange,
  onValorChange,
  onDataChange,
  onSubmit,
  onFechar,
}: PagarFaturaModalProps) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4"
      role="presentation"
      onClick={onFechar}
    >
      <form
        role="dialog"
        aria-modal="true"
        aria-label="Pagar fatura"
        onClick={(event) => event.stopPropagation()}
        onSubmit={onSubmit}
        className="flex w-full max-w-sm flex-col gap-4 rounded-xl border border-border bg-card px-5 py-5"
      >
        <div className="flex items-center justify-between">
          <h2 className="text-[19px] font-medium text-text-primary">Pagar fatura</h2>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            onClick={onFechar}
            aria-label="Fechar formulario"
          >
            <X className="size-4" aria-hidden="true" />
          </Button>
        </div>

        <div className="flex flex-col gap-1 rounded-lg bg-accent px-3 py-2">
          <span className="text-[12px] text-text-muted">Saldo pendente</span>
          <span className="text-[19px] font-medium text-alerta">{formatarMoeda(valorPendente)}</span>
        </div>

        {errorMessage && (
          <Alert variant="destructive">
            <AlertDescription>{errorMessage}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="contaOrigemPagamento">Conta de origem (id)</Label>
          <Input
            id="contaOrigemPagamento"
            placeholder="ID da conta bancaria de origem"
            required
            value={contaOrigemId}
            onChange={(event) => onContaOrigemIdChange(event.target.value)}
          />
          <span className="text-[12px] text-text-faint">
            Ainda nao ha selecao visual de contas - depende de um endpoint de listagem de contas
            BANCO que nao existe no backend.
          </span>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="dataPagamento">Data do pagamento</Label>
          <Input
            id="dataPagamento"
            type="date"
            required
            value={data}
            onChange={(event) => onDataChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="valorPagamento">Valor a pagar</Label>
          <Input
            id="valorPagamento"
            type="number"
            step="0.01"
            min="0.01"
            max={valorPendente}
            inputMode="decimal"
            required
            value={valor}
            onChange={(event) => onValorChange(event.target.value)}
          />
          <span className="text-[12px] text-text-faint">
            Pode ser menor que o saldo pendente - pagamento parcial e permitido, e a fatura
            continua em aberto pelo restante ate um novo pagamento.
          </span>
        </div>

        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Pagando..." : "Pagar fatura"}
        </Button>
      </form>
    </div>
  )
}
