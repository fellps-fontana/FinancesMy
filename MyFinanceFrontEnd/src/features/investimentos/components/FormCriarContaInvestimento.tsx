import type { FormEvent } from "react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

type FormCriarContaInvestimentoProps = {
  nome: string
  saldoInicial: string
  isSubmitting: boolean
  errorMessage: string | null
  onNomeChange: (value: string) => void
  onSaldoInicialChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onCancelar: () => void
}

// Componente burro: so exibe estado e dispara callbacks - mesma divisao de
// responsabilidade do LoginForm (estado do formulario e chamada da mutation
// ficam no container - ListaContasInvestimento). Sem origem/tipo de ativo
// aqui: conta manual criada por este form e sempre MANUAL, implicito no back
// (ver regra-de-negocio.md secao 8 e "Escopo: v1 vs v2").
export function FormCriarContaInvestimento({
  nome,
  saldoInicial,
  isSubmitting,
  errorMessage,
  onNomeChange,
  onSaldoInicialChange,
  onSubmit,
  onCancelar,
}: FormCriarContaInvestimentoProps) {
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
        <Label htmlFor="nomeContaInvestimento">Nome da conta</Label>
        <Input
          id="nomeContaInvestimento"
          name="nome"
          placeholder="Ex: Cofrinho, XP, Carteira de acoes"
          autoFocus
          required
          value={nome}
          onChange={(event) => onNomeChange(event.target.value)}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="saldoInicialContaInvestimento">Saldo inicial</Label>
        <Input
          id="saldoInicialContaInvestimento"
          name="saldoInicial"
          type="number"
          step="0.01"
          min="0"
          inputMode="decimal"
          required
          value={saldoInicial}
          onChange={(event) => onSaldoInicialChange(event.target.value)}
        />
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onCancelar} disabled={isSubmitting}>
          Cancelar
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Salvando..." : "Salvar"}
        </Button>
      </div>
    </form>
  )
}
