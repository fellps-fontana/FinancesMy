import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

type CampoLimiteGastoProps = {
  valor: string
  onChange: (valor: string) => void
  /** Categoria ja tem limite gravado: habilita "Remover limite". */
  temLimite: boolean
  onRemover: () => void
  removendo?: boolean
  disabled?: boolean
}

// Campo de limite de gasto mensal (regra-de-negocio.md item 14), embutido no
// formulario de categoria - nao ha tela separada de Limites. Apresentacao
// pura e controlada: o valor, a gravacao e a remocao (endpoint existente)
// pertencem ao FormCategoria. Quem renderiza este campo ja decidiu que a
// categoria e DESPESA (categoria RECEITA nunca tem limite, item 14).
export function CampoLimiteGasto({
  valor,
  onChange,
  temLimite,
  onRemover,
  removendo = false,
  disabled = false,
}: CampoLimiteGastoProps) {
  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-center justify-between gap-2">
        <Label htmlFor="limiteGastoCategoria">Limite de gasto mensal (opcional)</Label>
        {temLimite && (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={onRemover}
            disabled={disabled || removendo}
          >
            {removendo ? "Removendo..." : "Remover limite"}
          </Button>
        )}
      </div>
      <Input
        id="limiteGastoCategoria"
        type="text"
        inputMode="decimal"
        placeholder="Sem limite"
        value={valor}
        disabled={disabled || removendo}
        onChange={(event) => onChange(event.target.value)}
      />
    </div>
  )
}
