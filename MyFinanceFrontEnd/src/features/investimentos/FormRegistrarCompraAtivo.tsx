import type { FormEvent } from "react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

type FormRegistrarCompraAtivoProps = {
  ticker: string
  nome: string
  quantidade: string
  precoUnitario: string
  data: string
  isSubmitting: boolean
  errorMessage: string | null
  onTickerChange: (value: string) => void
  onNomeChange: (value: string) => void
  onQuantidadeChange: (value: string) => void
  onPrecoUnitarioChange: (value: string) => void
  onDataChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onCancelar: () => void
}

// Componente burro: so exibe estado e dispara callbacks - mesma divisao de
// responsabilidade de FormCriarContaInvestimento (estado do formulario e
// chamada da mutation ficam no container - ListaAtivos). Sem campo de
// precoMedio: e sempre calculado no back (regra-de-negocio.md item 8.2).
export function FormRegistrarCompraAtivo({
  ticker,
  nome,
  quantidade,
  precoUnitario,
  data,
  isSubmitting,
  errorMessage,
  onTickerChange,
  onNomeChange,
  onQuantidadeChange,
  onPrecoUnitarioChange,
  onDataChange,
  onSubmit,
  onCancelar,
}: FormRegistrarCompraAtivoProps) {
  return (
    <form
      onSubmit={onSubmit}
      className="flex flex-col gap-4 rounded-xl border border-border bg-card px-4 py-4"
    >
      {errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="tickerCompraAtivo">Ticker</Label>
        <Input
          id="tickerCompraAtivo"
          name="ticker"
          placeholder="Ex: PETR4, ITSA3"
          autoFocus
          required
          value={ticker}
          onChange={(event) => onTickerChange(event.target.value)}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="nomeCompraAtivo">Nome (opcional)</Label>
        <Input
          id="nomeCompraAtivo"
          name="nome"
          placeholder="Ex: Petrobras PN"
          value={nome}
          onChange={(event) => onNomeChange(event.target.value)}
        />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="quantidadeCompraAtivo">Quantidade</Label>
          <Input
            id="quantidadeCompraAtivo"
            name="quantidade"
            type="number"
            step="any"
            min="0"
            inputMode="decimal"
            required
            value={quantidade}
            onChange={(event) => onQuantidadeChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="precoUnitarioCompraAtivo">Preco unitario</Label>
          <Input
            id="precoUnitarioCompraAtivo"
            name="precoUnitario"
            type="number"
            step="0.01"
            min="0"
            inputMode="decimal"
            required
            value={precoUnitario}
            onChange={(event) => onPrecoUnitarioChange(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="dataCompraAtivo">Data da compra</Label>
        <Input
          id="dataCompraAtivo"
          name="data"
          type="date"
          required
          value={data}
          onChange={(event) => onDataChange(event.target.value)}
        />
      </div>

      {/* regra-de-negocio.md item 8.1: o preco unitario informado aqui vira o
          preco_atual do ativo e passa a valer para TODA a posicao (nao so
          esta leva) - o usuario precisa entender isso antes de confirmar. */}
      <p className="rounded-lg border border-alerta/30 bg-alerta/10 px-2.5 py-2 text-[12px] leading-snug text-alerta">
        O preco unitario informado passa a valer como preco atual de toda a posicao deste ativo,
        nao so da quantidade comprada agora. O preco medio (custo historico) e recalculado a
        parte pelo sistema.
      </p>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onCancelar} disabled={isSubmitting}>
          Cancelar
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Registrando..." : "Registrar compra"}
        </Button>
      </div>
    </form>
  )
}
