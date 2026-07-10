import { CreditCard } from "lucide-react"
import { Card } from "@/shared/ui/card"

type CartaoVisualProps = {
  nome: string
}

// Representacao visual do cartao fisico (preserva a intencao da branch de
// referencia, so trocando Card/CSS Module por shared/ui/card.tsx + Tailwind).
// O numero mascarado e decorativo: o backend nao expoe (e nao deveria expor)
// numero de cartao nem nome do titular - dado que nao existe no schema de Conta.
export function CartaoVisual({ nome }: CartaoVisualProps) {
  return (
    <Card className="flex max-w-sm flex-col gap-6 bg-accent-deep px-5 py-5 text-accent-soft ring-0">
      <div className="flex items-center justify-between">
        <span className="text-[13px] font-medium">{nome}</span>
        <CreditCard className="size-5" strokeWidth={1.6} aria-hidden="true" />
      </div>
      <span className="text-sm tracking-[0.2em] text-text-primary">•••• •••• •••• ••••</span>
    </Card>
  )
}
