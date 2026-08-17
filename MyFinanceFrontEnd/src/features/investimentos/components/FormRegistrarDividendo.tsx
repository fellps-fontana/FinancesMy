import type { FormEvent } from "react"
import { X } from "lucide-react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

type FormRegistrarDividendoProps = {
  ativoNome: string
  valor: string
  data: string
  isSubmitting: boolean
  errorMessage: string | null
  onValorChange: (value: string) => void
  onDataChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onFechar: () => void
}

// Componente de apresentacao (burro): formulario "Registrar dividendo"
// (regra-de-negocio.md item 8.4). So coleta `valor` e `data` - tipo
// (DIVIDENDO) e origem (MANUAL) sao decididos pelo backend, nunca expostos
// aqui (briefing desta tarefa). Mesmo padrao visual de overlay do
// ModalNovoAtivo: fundo fixo com dialog centralizado.
export function FormRegistrarDividendo({
  ativoNome,
  valor,
  data,
  isSubmitting,
  errorMessage,
  onValorChange,
  onDataChange,
  onSubmit,
  onFechar,
}: FormRegistrarDividendoProps) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4"
      role="presentation"
      onClick={onFechar}
    >
      <form
        role="dialog"
        aria-modal="true"
        aria-label="Registrar dividendo"
        onClick={(event) => event.stopPropagation()}
        onSubmit={onSubmit}
        className="flex w-full max-w-sm flex-col gap-4 rounded-xl border border-border bg-card px-5 py-5"
      >
        <div className="flex items-center justify-between">
          <div className="flex flex-col">
            <h2 className="text-[19px] font-medium text-text-primary">Registrar dividendo</h2>
            <span className="text-[12px] text-text-muted">{ativoNome}</span>
          </div>
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

        <div className="grid grid-cols-2 gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="valorRegistrarDividendo">Valor</Label>
            <Input
              id="valorRegistrarDividendo"
              type="number"
              step="0.01"
              min="0.01"
              inputMode="decimal"
              autoFocus
              required
              value={valor}
              onChange={(event) => onValorChange(event.target.value)}
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="dataRegistrarDividendo">Data</Label>
            <Input
              id="dataRegistrarDividendo"
              type="date"
              required
              value={data}
              onChange={(event) => onDataChange(event.target.value)}
            />
          </div>
        </div>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="ghost" onClick={onFechar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Salvando..." : "Salvar dividendo"}
          </Button>
        </div>
      </form>
    </div>
  )
}
