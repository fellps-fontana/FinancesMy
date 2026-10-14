import { ArrowLeftRight, CreditCard, Plus, type LucideIcon } from "lucide-react"
import { Link } from "react-router-dom"
import { cn } from "@/shared/lib/utils"

type AcaoRapida = {
  label: string
  to: string
  icon: LucideIcon
}

// Destinos do mockup (mockups/02 Dashboard.dc.html, bloco de 3 acoes entre o
// card de saldo e o resumo do mes).
//
// "Novo lancamento" e "Transferir" levam pra /lancamentos com o query param
// `novo` indicando qual lado do segmented control Lancamento/Transferencia
// (ver LancamentosPage.tsx, EstadoFormulario) deveria abrir pre-selecionado.
// Esta leva cobre so a navegacao: LancamentosPage.tsx nao esta em
// ARQUIVOS PERMITIDOS desta task e ainda nao le esse parametro, entao o
// formulario continua fechado ao chegar - ler `novo` e chamar
// handleAlternarNovo/handleTrocarTipoNovo fica para task seguinte que toque
// aquele arquivo.
//
// "Transferir" abre o fluxo de transferencia de /lancamentos (decisao do
// usuario, substitui o destino /contas do mockup mobile).
//
// "Pagar conta" vai para /conciliacao (conta a pagar -> pagamento real,
// regra-de-negocio.md item 5), destino do mockup mobile.
const ACOES: AcaoRapida[] = [
  { label: "Novo lancamento", to: "/lancamentos?novo=lancamento", icon: Plus },
  { label: "Transferir", to: "/lancamentos?novo=transferencia", icon: ArrowLeftRight },
  { label: "Pagar conta", to: "/conciliacao", icon: CreditCard },
]

type AcoesRapidasProps = {
  className?: string
}

/**
 * Acoes rapidas do topo do dashboard. Componente de apresentacao puro: cada
 * item e um `<Link>` real do react-router (nao onClick + navigate), pra
 * preservar comportamento nativo de link (abrir em nova aba, foco por
 * teclado, prefetch de rota) - nenhum estado nem fetch mora aqui.
 */
export function AcoesRapidas({ className }: AcoesRapidasProps) {
  return (
    <div className={cn("grid grid-cols-3 gap-2.5", className)} role="group" aria-label="Acoes rapidas">
      {ACOES.map(({ label, to, icon: Icon }) => (
        <Link
          key={label}
          to={to}
          className={cn(
            "flex flex-col items-center gap-1.5 rounded-xl border-[0.5px] border-border bg-card px-2 py-3 text-center",
            "text-[12px] text-text-body transition-colors",
            "hover:bg-muted/60 focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50",
          )}
        >
          <Icon className="size-[18px] text-accent-soft" strokeWidth={1.6} aria-hidden="true" />
          <span>{label}</span>
        </Link>
      ))}
    </div>
  )
}
