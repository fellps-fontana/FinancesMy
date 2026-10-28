import {
  ArrowLeftRight,
  CalendarSync,
  ChartPie,
  CreditCard,
  HandCoins,
  LayoutDashboard,
  Receipt,
  Repeat,
  Shuffle,
  Tags,
  Target,
  TrendingUp,
  Wallet,
  type LucideIcon,
} from "lucide-react"

export type NavItem = {
  to: string
  label: string
  icon: LucideIcon
  end?: boolean
}

export type NavGroup = {
  // Sem rotulo = grupo principal (topo da sidebar)
  label?: string
  items: NavItem[]
}

// Estrutura da sidebar desktop (>= md), espelhando o mockup: principal,
// Organizacao e Acompanhamento. Puramente estrutural - rotulo + rota + icone.
// Planejamento (Organizacao, antes Limites de gasto; /limites-gasto segue por
// link da tela) e Recebiveis recorrentes (Acompanhamento)
// sao itens extras mantidos por decisao do usuario. /de-para, /conciliacao e
// /relatorio podem ainda nao ter tela: caem no redirect padrao do router.
export const NAV_GROUPS: NavGroup[] = [
  {
    items: [
      { to: "/", label: "Inicio", icon: LayoutDashboard, end: true },
      { to: "/lancamentos", label: "Lancamentos", icon: Receipt },
      { to: "/cartao", label: "Cartao", icon: CreditCard },
      { to: "/contas", label: "Contas", icon: Wallet },
    ],
  },
  {
    label: "Organizacao",
    items: [
      { to: "/categorias", label: "Categorias", icon: Tags },
      { to: "/de-para", label: "De-para de categoria", icon: Shuffle },
      { to: "/contas-fixas", label: "Contas fixas", icon: Repeat },
      { to: "/planejamento", label: "Planejamento", icon: Target },
    ],
  },
  {
    label: "Acompanhamento",
    items: [
      { to: "/conciliacao", label: "Conciliacao", icon: ArrowLeftRight },
      { to: "/contas-receber", label: "Contas a receber", icon: HandCoins },
      { to: "/recebiveis-recorrentes", label: "Recebiveis recorrentes", icon: CalendarSync },
      { to: "/investimentos", label: "Investimentos", icon: TrendingUp },
      { to: "/relatorio", label: "Relatorio por categoria", icon: ChartPie },
    ],
  },
]

// 4 destinos de maior uso na bottom tab bar mobile (= grupo principal).
export const MOBILE_PRIMARY_ITEMS: NavItem[] = NAV_GROUPS[0].items

const MOBILE_PRIMARY_PATHS = new Set(MOBILE_PRIMARY_ITEMS.map((item) => item.to))

// Demais destinos, abertos pelo item "Mais" da bottom tab bar.
export const MOBILE_MORE_ITEMS: NavItem[] = NAV_GROUPS.flatMap((group) => group.items).filter(
  (item) => !MOBILE_PRIMARY_PATHS.has(item.to),
)
