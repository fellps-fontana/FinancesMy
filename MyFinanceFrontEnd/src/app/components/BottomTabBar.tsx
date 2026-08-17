import { useState } from "react"
import { NavLink, useLocation } from "react-router-dom"
import { MoreHorizontal, X } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { BrandTitle, UserFooter, type NavItem } from "@/app/AppShell"

type BottomTabBarProps = {
  // 4 destinos de maior uso, sempre visiveis na barra (mockups "02
  // Dashboard", "03 Contas", "05 Cartao de Credito" etc).
  primaryItems: NavItem[]
  // Demais destinos, abertos pela folha "Mais".
  moreItems: NavItem[]
  username: string | undefined
  onLogout: () => void
}

function tabClassName(isActive: boolean) {
  return cn(
    "flex flex-1 flex-col items-center gap-1 py-2 text-[11px] font-medium transition-colors",
    isActive ? "text-primary" : "text-text-faint hover:text-text-body",
  )
}

function moreItemClassName(isActive: boolean) {
  return cn(
    "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
    isActive
      ? "bg-primary/10 text-primary"
      : "text-text-muted hover:bg-secondary hover:text-text-body",
  )
}

// Navegacao mobile (< md), padrao repetido nos mockups: barra fixa no
// rodape com 4 destinos de maior uso + "Mais", que abre uma folha com os
// destinos restantes. Substitui o antigo drawer full-screen aberto por
// hamburguer - ver AppShell.tsx para a divisao mobile (< md) x desktop
// (sidebar >= md, INALTERADA por esta mudanca).
export function BottomTabBar({ primaryItems, moreItems, username, onLogout }: BottomTabBarProps) {
  const [isMoreOpen, setIsMoreOpen] = useState(false)
  const location = useLocation()

  // "Mais" fica destacado tambem quando a rota ativa e um dos itens
  // secundarios (ex: usuario esta em /categorias) - o usuario precisa ver de
  // relance onde esta, mesmo com o destino fora dos 4 primarios.
  const isMoreActive = moreItems.some((item) => location.pathname === item.to)

  return (
    <>
      {isMoreOpen && (
        <div className="fixed inset-0 z-50 flex items-end md:hidden">
          <button
            type="button"
            aria-label="Fechar menu Mais"
            onClick={() => setIsMoreOpen(false)}
            className="absolute inset-0 bg-background/80"
          />
          <div className="relative z-10 flex w-full flex-col rounded-t-xl border-t border-border bg-card pb-[max(0.75rem,env(safe-area-inset-bottom))]">
            <div className="flex items-center justify-between border-b border-border px-4 py-3">
              <BrandTitle />
              <button
                type="button"
                onClick={() => setIsMoreOpen(false)}
                aria-label="Fechar menu Mais"
                className="flex size-9 items-center justify-center rounded-lg text-text-muted hover:bg-secondary hover:text-text-body"
              >
                <X className="size-5" strokeWidth={1.6} />
              </button>
            </div>
            <nav className="flex flex-col gap-1 p-3">
              {moreItems.map(({ to, label, icon: Icon, end }) => (
                <NavLink
                  key={to}
                  to={to}
                  end={end}
                  onClick={() => setIsMoreOpen(false)}
                  className={({ isActive }) => moreItemClassName(isActive)}
                >
                  <Icon className="size-4 shrink-0" strokeWidth={1.6} />
                  {label}
                </NavLink>
              ))}
            </nav>
            <UserFooter username={username} onLogout={onLogout} />
          </div>
        </div>
      )}

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
        <button
          type="button"
          onClick={() => setIsMoreOpen(true)}
          aria-haspopup="dialog"
          aria-expanded={isMoreOpen}
          className={tabClassName(isMoreOpen || isMoreActive)}
        >
          <MoreHorizontal className="size-5" strokeWidth={1.8} />
          Mais
        </button>
      </nav>
    </>
  )
}
