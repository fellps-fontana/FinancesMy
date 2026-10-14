import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { cn } from "@/shared/lib/utils"
import { CLASSE_CAMPO } from "@/shared/ui/FormModal"
import { montarResumoParcelamento } from "@/features/lancamentos/lib/montarResumoParcelamento"

type FormLancamentoParceladoProps = {
  // Valor total ja convertido (null quando o campo esta vazio/invalido).
  valorTotal: number | null
  parcelas: string
  onParcelasChange: (parcelas: string) => void
  disabled?: boolean
}

// Bloco "Parcelas + Resumo" do mockup (so despesa, item 17). Apresentacao
// pura: o resumo e previa; quem grava as parcelas e o backend.
export function FormLancamentoParcelado({
  valorTotal,
  parcelas,
  onParcelasChange,
  disabled,
}: FormLancamentoParceladoProps) {
  const resumo = montarResumoParcelamento(valorTotal, Number(parcelas))

  return (
    <div className="flex items-end gap-2.5">
      <div className="flex w-24 shrink-0 flex-col gap-1.5">
        <Label htmlFor="parcelasLancamento">Parcelas</Label>
        <Input
          id="parcelasLancamento"
          type="number"
          min={2}
          step={1}
          inputMode="numeric"
          disabled={disabled}
          value={parcelas}
          onChange={(event) => onParcelasChange(event.target.value)}
          className={CLASSE_CAMPO}
        />
      </div>
      <div className="min-w-0 flex-1 rounded-[10px] border-[0.5px] border-border bg-background px-3.5 py-1.5">
        <div className="text-[11px] text-text-faint">Resumo</div>
        <div
          className={cn("mt-0.5 text-[13px]", resumo.valido ? "text-text-primary" : "text-text-muted")}
        >
          {resumo.texto}
        </div>
      </div>
    </div>
  )
}
