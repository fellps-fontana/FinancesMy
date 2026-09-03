import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react"
import { NavLink } from "react-router-dom"
import {
  CalendarSync,
  CreditCard,
  HandCoins,
  LayoutDashboard,
  Receipt,
  Repeat,
  Tags,
  Target,
  TrendingUp,
  Wallet,
  type LucideIcon,
} from "lucide-react"
import { useAuth } from "@/features/auth/useAuth"
import { Button } from "@/shared/ui/button"
import { cn } from "@/shared/lib/utils"
import { BottomTabBar } from "@/app/components/BottomTabBar"
import { ThemeToggle } from "@/app/components/ThemeToggle"
import {
  ThemeContext,
  THEME_STORAGE_KEY,
  applyResolvedTheme,
  readStoredThemePreference,
  resolveTheme,
  type ThemeContextValue,
  type ThemePreference,
} from "@/shared/hooks/useTheme"

type AppShellProps = {
  children: ReactNode
}

export type NavItem = {
  to: string
  label: string
  icon: LucideIcon
  end?: boolean
}

// Os 10 destinos de navegacao do app (stack.md "Estrutura de pastas (src/)" -
// um destino por modulo/feature). Ordem espelha a prioridade de uso diario:
// visao geral primeiro, depois contas/dinheiro, depois organizacao (regras,
// categorias, limites). Puramente estrutural - nenhuma regra de dominio mora
// aqui, so rotulo + rota + icone.
//
// Fonte unica da sidebar desktop (>= md), que permanece INALTERADA nesta
// mudanca (ver ESCOPO da task de bottom tab bar mobile). A navegacao mobile
// (< md) usa MOBILE_PRIMARY_ITEMS + MOBILE_MORE_ITEMS abaixo, derivados
// deste array para nao duplicar rota/icone.
const NAV_ITEMS: NavItem[] = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/contas", label: "Contas", icon: Wallet },
  { to: "/cartao", label: "Cartao de credito", icon: CreditCard },
  { to: "/investimentos", label: "Investimentos", icon: TrendingUp },
  { to: "/contas-fixas", label: "Contas fixas", icon: Repeat },
  { to: "/contas-receber", label: "Contas a receber", icon: HandCoins },
  { to: "/recebiveis-recorrentes", label: "Recebiveis recorrentes", icon: CalendarSync },
  { to: "/lancamentos", label: "Lancamentos", icon: Receipt },
  { to: "/categorias", label: "Categorias", icon: Tags },
  { to: "/limites-gasto", label: "Limites de gasto", icon: Target },
]

// 4 destinos de maior uso, sempre visiveis na bottom tab bar mobile (padrao
// repetido nos mockups "02 Dashboard", "03 Contas", "05 Cartao de Credito").
// Rotulos curtos ("Inicio"/"Cartao") sao PROPOSITAIS - o mockup usa texto
// mais compacto que a sidebar (11px, 5 itens na largura da tela) - por isso
// nao reaproveita o rotulo de NAV_ITEMS, so rota/icone equivalentes. A
// sidebar desktop continua usando NAV_ITEMS sem alteracao.
const MOBILE_PRIMARY_ITEMS: NavItem[] = [
  { to: "/", label: "Inicio", icon: LayoutDashboard, end: true },
  { to: "/lancamentos", label: "Lancamentos", icon: Receipt },
  { to: "/cartao", label: "Cartao", icon: CreditCard },
  { to: "/contas", label: "Contas", icon: Wallet },
]

const MOBILE_PRIMARY_PATHS = new Set(MOBILE_PRIMARY_ITEMS.map((item) => item.to))

// Os 6 destinos restantes (todo NAV_ITEMS fora dos 4 primarios acima),
// abertos pelo item "Mais" da bottom tab bar - reaproveita rotulo/icone/rota
// de NAV_ITEMS, sem duplicar texto nem perder nenhuma rota.
const MOBILE_MORE_ITEMS: NavItem[] = NAV_ITEMS.filter((item) => !MOBILE_PRIMARY_PATHS.has(item.to))

function navLinkClassName(isActive: boolean) {
  return cn(
    "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
    isActive
      ? "bg-primary/10 text-primary"
      : "text-text-muted hover:bg-secondary hover:text-text-body",
  )
}

// Lista de navegacao pura da sidebar fixa (desktop) - unico consumidor
// depois que o drawer mobile foi substituido pela BottomTabBar.
function NavList() {
  return (
    <nav className="flex flex-1 flex-col gap-1 overflow-y-auto p-3">
      {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
        <NavLink key={to} to={to} end={end} className={({ isActive }) => navLinkClassName(isActive)}>
          <Icon className="size-4 shrink-0" strokeWidth={1.6} />
          {label}
        </NavLink>
      ))}
    </nav>
  )
}

// Nome do app - usado no header da sidebar desktop e no header da folha
// "Mais" da BottomTabBar (mobile). Centralizado aqui para nao duplicar
// texto/classe entre os dois pontos.
export function BrandTitle() {
  return <span className="text-[19px] font-medium text-text-primary">Financeiro Pessoal</span>
}

type UserFooterProps = {
  username: string | undefined
  onLogout: () => void
}

// Rodape com identidade do usuario logado + acao de sair - unico ponto onde
// AppShell toca useAuth, so para leitura de nome e disparo de logout (sem
// fetch de dominio, sem regra de negocio: ver ESCOPO da tarefa). Usado na
// sidebar desktop e, agora, na folha "Mais" da BottomTabBar mobile (o antigo
// drawer full-screen tambem expunha o logout - a folha "Mais" e o substituto
// dessa superficie no mobile). ThemeToggle fica ao lado do botao "Sair" -
// unico ponto de alternancia de tema, visivel nas duas superficies.
export function UserFooter({ username, onLogout }: UserFooterProps) {
  return (
    <div className="flex items-center justify-between gap-3 border-t border-border p-3">
      <span className="truncate text-sm text-text-body">Ola, {username}</span>
      <div className="flex items-center gap-2">
        <ThemeToggle />
        <Button variant="outline" size="sm" onClick={onLogout}>
          Sair
        </Button>
      </div>
    </div>
  )
}

// Provider do ThemeContext (shared/hooks/useTheme.ts) - montado aqui porque
// AppShell e "o shell da app" citado em identidade-visual.md, "Mecanismo de
// alternancia" (mesmo padrao arquitetural de AuthProvider em
// features/auth/AuthContext.tsx). O script inline de index.html ja aplicou
// a classe de tema ANTES do primeiro paint; este Provider so mantem o
// estado em sincronia com interacao do usuario (ThemeToggle) e com mudanca
// de preferencia do SO em tempo real, quando a escolha ainda e "system".
function ThemeProvider({ children }: { children: ReactNode }) {
  const [preference, setPreferenceState] = useState<ThemePreference>(() => readStoredThemePreference())

  useEffect(() => {
    applyResolvedTheme(resolveTheme(preference))

    if (preference !== "system") {
      return
    }

    const media = window.matchMedia("(prefers-color-scheme: dark)")
    const handleChange = () => applyResolvedTheme(resolveTheme(preference))
    media.addEventListener("change", handleChange)
    return () => media.removeEventListener("change", handleChange)
  }, [preference])

  const setTheme = useCallback((next: ThemePreference) => {
    setPreferenceState(next)
    // "system" nao fica gravado - ausencia de chave e o proprio default,
    // mesmo contrato que o script inline de index.html le
    if (next === "system") {
      window.localStorage.removeItem(THEME_STORAGE_KEY)
    } else {
      window.localStorage.setItem(THEME_STORAGE_KEY, next)
    }
  }, [])

  const toggleTheme = useCallback(() => {
    setTheme(resolveTheme(preference) === "dark" ? "light" : "dark")
  }, [preference, setTheme])

  const value = useMemo<ThemeContextValue>(
    () => ({ preference, resolvedTheme: resolveTheme(preference), setTheme, toggleTheme }),
    [preference, setTheme, toggleTheme],
  )

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export function AppShell({ children }: AppShellProps) {
  const { usuario, logout } = useAuth()

  return (
    <ThemeProvider>
      <div className="flex min-h-svh flex-col bg-background md:flex-row">
        {/* Sidebar fixa (>= md) - INALTERADA por esta mudanca. */}
        <aside className="hidden shrink-0 border-r border-border bg-card md:sticky md:top-0 md:flex md:h-svh md:w-64 md:flex-col">
          <div className="border-b border-border px-4 py-4">
            <BrandTitle />
          </div>
          <NavList />
          <UserFooter username={usuario?.username} onLogout={logout} />
        </aside>

        {/* pb-24 da folga pra bottom tab bar fixa (< md) nao cobrir o fim do
            conteudo; sem efeito em >= md (bar so existe no mobile). */}
        <main className="flex-1 px-4 pt-8 pb-24 md:pb-8">
          <div className="mx-auto w-full max-w-2xl">{children}</div>
        </main>

        {/* Bottom tab bar mobile (< md): substitui o antigo drawer full-screen
            aberto por hamburguer - ver BottomTabBar.tsx. */}
        <BottomTabBar
          primaryItems={MOBILE_PRIMARY_ITEMS}
          moreItems={MOBILE_MORE_ITEMS}
          username={usuario?.username}
          onLogout={logout}
        />
      </div>
    </ThemeProvider>
  )
}
