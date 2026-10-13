import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react"
import { NavLink } from "react-router-dom"
import { LogOut, Wallet } from "lucide-react"
import { useAuth } from "@/features/auth/useAuth"
import { Button } from "@/shared/ui/button"
import { cn } from "@/shared/lib/utils"
import { NAV_GROUPS, MOBILE_MORE_ITEMS, MOBILE_PRIMARY_ITEMS, type NavItem } from "@/app/navigation"
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

export type { NavItem }

function navLinkClassName(isActive: boolean) {
  return cn(
    "flex items-center gap-2.5 rounded-[10px] px-3 py-[9px] text-sm transition-colors",
    isActive
      ? "bg-accent-deep text-text-primary"
      : "text-text-muted hover:bg-secondary hover:text-text-body",
  )
}

// Lista de navegacao da sidebar fixa (desktop), agrupada conforme o mockup.
function NavList() {
  return (
    <nav className="flex flex-1 flex-col gap-6 overflow-y-auto">
      {NAV_GROUPS.map((group, index) => (
        <div key={group.label ?? index} className="flex flex-col gap-0.5">
          {group.label && <div className="px-3 pb-1.5 text-xs text-text-faint">{group.label}</div>}
          {group.items.map(({ to, label, icon: Icon, end }) => (
            <NavLink key={to} to={to} end={end} className={({ isActive }) => navLinkClassName(isActive)}>
              {({ isActive }) => (
                <>
                  <Icon
                    className={cn("size-4 shrink-0", isActive ? "text-accent-soft" : "text-text-faint")}
                    strokeWidth={1.6}
                  />
                  {label}
                </>
              )}
            </NavLink>
          ))}
        </div>
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

function SidebarBrand() {
  return (
    <div className="flex items-center gap-2.5 px-2">
      <div className="flex size-8 items-center justify-center rounded-[10px] bg-accent-deep text-accent-soft">
        <Wallet className="size-4" strokeWidth={1.6} />
      </div>
      <span className="text-sm font-medium text-text-primary">Financeiro</span>
    </div>
  )
}

// Rodape da sidebar desktop: Sair + alternancia de tema (unico ponto de tema).
function SidebarFooter({ onLogout }: { onLogout: () => void }) {
  return (
    <div className="flex items-center justify-between gap-2 border-t border-border pt-4">
      <button
        type="button"
        onClick={onLogout}
        className="flex items-center gap-2.5 rounded-[10px] px-3 py-[9px] text-sm text-text-muted transition-colors hover:bg-secondary hover:text-text-body"
      >
        <LogOut className="size-4 shrink-0 text-text-faint" strokeWidth={1.6} />
        Sair
      </button>
      <ThemeToggle />
    </div>
  )
}

export function AppShell({ children }: AppShellProps) {
  const { usuario, logout } = useAuth()

  return (
    <ThemeProvider>
      <div className="flex min-h-svh flex-col bg-background md:flex-row">
        {/* Sidebar fixa (>= md), padrao do mockup desktop. */}
        <aside className="hidden shrink-0 gap-6 border-r border-border bg-card px-4 py-6 md:sticky md:top-0 md:flex md:h-svh md:w-[232px] md:flex-col">
          <SidebarBrand />
          <NavList />
          <SidebarFooter onLogout={logout} />
        </aside>

        {/* pb-24 da folga pra bottom tab bar fixa (< md) nao cobrir o fim do
            conteudo; sem efeito em >= md (bar so existe no mobile). */}
        <main className="flex-1 px-4 pt-8 pb-24 md:px-10 md:py-8">
          <div className="mx-auto w-full">{children}</div>
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
