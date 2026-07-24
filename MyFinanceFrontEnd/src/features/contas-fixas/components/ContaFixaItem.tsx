import { cn } from "@/shared/lib/utils"
import { Card, CardContent } from "@/shared/ui/card"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import type { ContaFixaResponse } from "@/features/contas-fixas/types"

type StatusContaFixa = "ATIVA" | "INATIVA"

// Mapeamento de cor por status ativa/inativa (regra-de-negocio.md item 6).
// Nao ha token proprio para este estado em identidade-visual.md (o
// documento cobre pago/pendente/manual/sugerido), entao seguimos o mesmo
// espirito ja aplicado em ContaReceberItem: ATIVA e o estado "em vigor" da
// conta fixa (gera Lancamento PENDENTE todo mes - positivo, mesma familia
// semantica de "recebido"/"pago" no documento), INATIVA e tratada como
// origem/estado neutro (bg-muted + text-muted), o mesmo par de classes que
// identidade-visual.md reserva para "manual -> neutro". Nao usamos "alerta"
// porque inativa e um estado intencional do usuario, nao uma pendencia.
const CONFIG_POR_STATUS: Record<StatusContaFixa, { label: string; className: string }> = {
  ATIVA: { label: "Ativa", className: "bg-positivo/15 text-positivo" },
  INATIVA: { label: "Inativa", className: "bg-muted text-text-muted" },
}

type ContaFixaItemProps = {
  contaFixa: ContaFixaResponse
}

// Componente de apresentacao (burro): so exibe o que ja vem pronto do
// backend (descricao, valor, diaVencimento, ativa). Nenhum calculo de
// regra de negocio mora aqui - formatarMoeda e a unica funcao aplicada, e e
// puramente de apresentacao (locale), nao regra de dominio - ver
// clean-code.md "Organizacao (React)".
//
// Categoria: categoriaId ainda nao tem lookup de nome no front (feature
// categorias/ e so um placeholder, sem tela/hook/tipo implementado ate
// agora). Exibir o id cru violaria "nunca dado cru na tela" (identidade-
// visual.md), entao a categoria fica omitida nesta tela ate existir um
// componente de categoria pronto para resolver o nome (decisao documentada
// na TASK-063).
export function ContaFixaItem({ contaFixa }: ContaFixaItemProps) {
  const status: StatusContaFixa = contaFixa.ativa ? "ATIVA" : "INATIVA"
  const statusConfig = CONFIG_POR_STATUS[status]

  return (
    <Card size="sm">
      <CardContent className="flex flex-col gap-2">
        <div className="flex items-start justify-between gap-2">
          <span className="text-[19px] font-medium text-text-primary">{contaFixa.descricao}</span>

          <span
            className={cn(
              "inline-flex items-center rounded-[5px] px-2 py-0.5 text-[12px] font-medium",
              statusConfig.className,
            )}
          >
            {statusConfig.label}
          </span>
        </div>

        <div className="flex items-center justify-between text-[13px] text-text-muted">
          <span>
            Valor <span className="font-medium text-text-body">{formatarMoeda(contaFixa.valor)}</span>
          </span>
          <span>Vence todo dia {contaFixa.diaVencimento}</span>
        </div>
      </CardContent>
    </Card>
  )
}
