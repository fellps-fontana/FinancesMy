import { NavLink, useLocation } from "react-router-dom"
import { MoreHorizontal } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import type { NavItem } from "@/app/navigation"

export const MAIS_PATH = "/mais"

type BottomTabBarProps = {
  // 4 destinos de maior uso, sempre visiveis na barra.
  primaryItems: NavItem[]
  // Destinos secundarios, alcancados pela rota /mais; usados so para manter
  // a aba "Mais" ativa quando o usuario esta em um deles.
  moreItems: NavItem[]
  // Mantidos por compatibilidade com AppShell; Sair/usuario vivem em MaisPage.
  username?: string | undefined
  onLogout?: () => void
}

function tabClassName(isActive: boolean) {
  return cn(
    "flex flex-1 flex-col items-center gap-1 py-2 text-[11px] font-medium transition-colors",
    isActive ? "text-primary" : "text-text-faint hover:text-text-body",
  )
}

function isOnPath(pathname: string, to: string) {
  return pathname === to || pathname.startsWith(`${to}/`)
}

// Barra mobile (< md) com 5 abas fixas: 4 destinos principais + "Mais",
// que e uma rota (/mais) e fica ativa tambem em qualquer destino secundario.
export function BottomTabBar({ primaryItems, moreItems }: BottomTabBarProps) {
  const { pathname } = useLocation()
  const isMoreActive = isOnPath(pathname, MAIS_PATH) || moreItems.some((item) => isOnPath(pathname, item.to))

  return (
    <nav
      aria-label="Navegacao principal"
      className="fixed inset-x-0 bottom-0 z-40 flex items-stretch border-t border-border bg-card pb-[env(safe-area-inset-bottom)] md:hidden"
    >
      {primaryItems.map(({ to, label, icon: Icon, end }) => (
        <NavLink key={to} to={to} end={end} className={({ isActive }) => tabClassName(isActive)}>
          <Icon className="size-5" strokeWidth={1.8} />
          {label}
        </NavLink>
      ))}
      <NavLink to={MAIS_PATH} className={tabClassName(isMoreActive)}>
        <MoreHorizontal className="size-5" strokeWidth={1.8} />
        Mais
      </NavLink>
    </nav>
  )
}
