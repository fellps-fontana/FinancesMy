import type { FormEvent } from "react"
import { X } from "lucide-react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

type LancarCompraFormProps = {
  descricao: string
  valor: string
  data: string
  isSubmitting: boolean
  errorMessage: string | null
  onDescricaoChange: (value: string) => void
  onValorChange: (value: string) => void
  onDataChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onFechar: () => void
}

/**
 * Formulario de lancamento de compra no cartao (regra de negocio item 12:
 * regime de COMPETENCIA - a compra muda o saldo calculado do cartao mas nao
 * entra no fluxo de caixa/lancamento geral). Puramente apresentacao: guarda
 * so o estado transiente dos campos; a mutation, o estado de envio e o erro
 * vem do container (ContaCartaoPage) - mesmo padrao de PagarFaturaModal.
 *
 * GAP CONHECIDO: o backend nao expoe endpoint de listagem de categorias do
 * usuario (regra de negocio item 7). O campo Categoria fica desabilitado,
 * com nota explicita, e toda compra lancada por aqui sai sem categoria
 * (`categoriaId: null`) ate essa integracao existir - nenhuma categoria fake
 * inventada aqui.
 */
export function LancarCompraForm({
  descricao,
  valor,
  data,
  isSubmitting,
  errorMessage,
  onDescricaoChange,
  onValorChange,
  onDataChange,
  onSubmit,
  onFechar,
}: LancarCompraFormProps) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4"
      role="presentation"
      onClick={onFechar}
    >
      <form
        role="dialog"
        aria-modal="true"
        aria-label="Lancar compra"
        onClick={(event) => event.stopPropagation()}
        onSubmit={onSubmit}
        className="flex w-full max-w-sm flex-col gap-4 rounded-xl border border-border bg-card px-5 py-5"
      >
        <div className="flex items-center justify-between">
          <h2 className="text-[19px] font-medium text-text-primary">Lancar compra</h2>
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

        {errorMessage && (
          <Alert variant="destructive">
            <AlertDescription>{errorMessage}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="descricaoCompra">Descricao</Label>
          <Input
            id="descricaoCompra"
            placeholder="Ex: Restaurante"
            required
            autoFocus
            value={descricao}
            onChange={(event) => onDescricaoChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="valorCompra">Valor</Label>
          <Input
            id="valorCompra"
            type="number"
            step="0.01"
            min="0.01"
            inputMode="decimal"
            placeholder="0,00"
            required
            value={valor}
            onChange={(event) => onValorChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="dataCompra">Data da compra</Label>
          <Input
            id="dataCompra"
            type="date"
            required
            value={data}
            onChange={(event) => onDataChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="categoriaCompra">Categoria</Label>
          <select
            id="categoriaCompra"
            disabled
            defaultValue=""
            className="h-8 w-full rounded-lg border border-input bg-input/30 px-2.5 text-sm text-text-muted disabled:cursor-not-allowed disabled:opacity-50"
          >
            <option value="">Sem categorias cadastradas</option>
          </select>
          <span className="text-[12px] text-text-faint">
            Ainda nao ha endpoint de categorias no backend. A compra sera lancada sem categoria
            ate essa integracao existir.
          </span>
        </div>

        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Lancando..." : "Lancar compra"}
        </Button>
      </form>
    </div>
  )
}
