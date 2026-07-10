import type { FormEvent } from "react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

type CriarContaCartaoFormProps = {
  nome: string
  diaFechamento: string
  diaVencimento: string
  isSubmitting: boolean
  errorMessage: string | null
  onNomeChange: (value: string) => void
  onDiaFechamentoChange: (value: string) => void
  onDiaVencimentoChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
}

// Componente burro: so exibe estado e dispara callbacks - mesmo padrao de
// FormCriarContaInvestimento (estado do formulario e a mutation ficam no
// container - ContaCartaoPage). Form dedicado (em vez de reaproveitar o de
// investimento) porque cartao exige dia de fechamento/vencimento, campos que
// nao existem em nenhuma outra conta manual (regra de negocio item 12).
export function CriarContaCartaoForm({
  nome,
  diaFechamento,
  diaVencimento,
  isSubmitting,
  errorMessage,
  onNomeChange,
  onDiaFechamentoChange,
  onDiaVencimentoChange,
  onSubmit,
}: CriarContaCartaoFormProps) {
  return (
    <form
      onSubmit={onSubmit}
      className="flex flex-col gap-4 rounded-xl border border-border bg-card px-4 py-4"
    >
      <div className="flex flex-col gap-1">
        <h2 className="text-[19px] font-medium text-text-primary">Novo cartao</h2>
        <p className="text-sm text-text-muted">
          Nenhum cartao cadastrado ainda. Informe os dados do ciclo de fatura para comecar.
        </p>
      </div>

      {errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="nomeContaCartao">Nome do cartao</Label>
        <Input
          id="nomeContaCartao"
          name="nome"
          placeholder="Ex: Nubank Ultravioleta"
          autoFocus
          required
          value={nome}
          onChange={(event) => onNomeChange(event.target.value)}
        />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="diaFechamentoContaCartao">Dia de fechamento</Label>
          <Input
            id="diaFechamentoContaCartao"
            name="diaFechamento"
            type="number"
            min="1"
            max="31"
            inputMode="numeric"
            required
            value={diaFechamento}
            onChange={(event) => onDiaFechamentoChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="diaVencimentoContaCartao">Dia de vencimento</Label>
          <Input
            id="diaVencimentoContaCartao"
            name="diaVencimento"
            type="number"
            min="1"
            max="31"
            inputMode="numeric"
            required
            value={diaVencimento}
            onChange={(event) => onDiaVencimentoChange(event.target.value)}
          />
        </div>
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Criando..." : "Criar cartao"}
        </Button>
      </div>
    </form>
  )
}
