import { useState, type ReactNode } from "react"
import { NavLink } from "react-router-dom"
import {
  CreditCard,
  HandCoins,
  LayoutDashboard,
  Menu,
  Receipt,
  Repeat,
  Tags,
  Target,
  TrendingUp,
  Wallet,
  X,
  type LucideIcon,
} from "lucide-react"
import { useAuth } from "@/features/auth/useAuth"
import { Button } from "@/shared/ui/button"
import { cn } from "@/shared/lib/utils"

type AppShellProps = {
  children: ReactNode
}

type NavItem = {
  to: string
  label: string
  icon: LucideIcon
  end?: boolean
}

// Os 9 destinos de navegacao do app (stack.md "Estrutura de pastas (src/)" -
// um destino por modulo/feature). Ordem espelha a prioridade de uso diario:
// visao geral primeiro, depois contas/dinheiro, depois organizacao (regras,
// categorias, limites). Puramente estrutural - nenhuma regra de dominio mora
// aqui, so rotulo + rota + icone.
//
// ATENCAO: "/lancamentos" e "/categorias" ainda NAO tem rota registrada em
// routes.tsx - sao links mortos ate a TASK-096 (rota /lancamentos) e a
// TASK-105 (rota /categorias, fechamento do Bloco D - Categorias,
// TASK-098 a 106) serem concluidas. Ambas ja estao no tasks.md e serao
// ligadas nesta mesma leva de trabalho; os itens ficam no array desde ja
// para nao exigir retrabalho de remover/recriar.
const NAV_ITEMS: NavItem[] = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/contas", label: "Contas", icon: Wallet },
  { to: "/cartao", label: "Cartao de credito", icon: CreditCard },
  { to: "/investimentos", label: "Investimentos", icon: TrendingUp },
  { to: "/contas-fixas", label: "Contas fixas", icon: Repeat },
  { to: "/contas-receber", label: "Contas a receber", icon: HandCoins },
  { to: "/lancamentos", label: "Lancamentos", icon: Receipt },
  { to: "/categorias", label: "Categorias", icon: Tags },
  { to: "/limites-gasto", label: "Limites de gasto", icon: Target },
]

function navLinkClassName(isActive: boolean) {
  return cn(
    "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
    isActive
      ? "bg-primary/10 text-primary"
      : "text-text-muted hover:bg-secondary hover:text-text-body",
  )
}

type NavListProps = {
  onNavigate?: () => void
}

// Lista de navegacao pura, compartilhada entre a sidebar fixa (desktop) e o
// drawer (mobile) - mesmo conteudo, dois containeres diferentes.
function NavList({ onNavigate }: NavListProps) {
  return (
    <nav className="flex flex-1 flex-col gap-1 overflow-y-auto p-3">
      {NAV_ITEMS.map(({ to, label, icon: Icon, end }) => (
        <NavLink
          key={to}
          to={to}
          end={end}
          onClick={onNavigate}
          className={({ isActive }) => navLinkClassName(isActive)}
        >
          <Icon className="size-4 shrink-0" strokeWidth={1.6} />
          {label}
        </NavLink>
      ))}
    </nav>
  )
}

// Nome do app, repetido na topbar mobile, no header do drawer e no header da
// sidebar - centralizado aqui para nao duplicar texto/classe nos 3 pontos.
function BrandTitle() {
  return <span className="text-[19px] font-medium text-text-primary">Financeiro Pessoal</span>
}

type UserFooterProps = {
  username: string | undefined
  onLogout: () => void
}

// Rodape com identidade do usuario logado + acao de sair - unico ponto onde
// AppShell toca useAuth, so para leitura de nome e disparo de logout (sem
// fetch de dominio, sem regra de negocio: ver ESCOPO da tarefa).
function UserFooter({ username, onLogout }: UserFooterProps) {
  return (
    <div className="flex items-center justify-between gap-3 border-t border-border p-3">
      <span className="truncate text-sm text-text-body">Ola, {username}</span>
      <Button variant="outline" size="sm" onClick={onLogout}>
        Sair
      </Button>
    </div>
  )
}

export function AppShell({ children }: AppShellProps) {
  const { usuario, logout } = useAuth()
  const [isDrawerOpen, setIsDrawerOpen] = useState(false)

  return (
    <div className="flex min-h-svh flex-col bg-background md:flex-row">
      {/* Topbar mobile (< md): titulo + hamburguer que abre o drawer. */}
      <header className="flex items-center justify-between border-b border-border bg-card px-4 py-3 md:hidden">
        <BrandTitle />
        <button
          type="button"
          onClick={() => setIsDrawerOpen(true)}
          aria-label="Abrir menu de navegacao"
          aria-expanded={isDrawerOpen}
          className="flex size-9 items-center justify-center rounded-lg text-text-muted hover:bg-secondary hover:text-text-body"
        >
          <Menu className="size-5" strokeWidth={1.6} />
        </button>
      </header>

      {/* Drawer mobile (< md): mesmos itens da sidebar, sobreposto ao conteudo. */}
      {isDrawerOpen && (
        <div className="fixed inset-0 z-50 flex md:hidden">
          <button
            type="button"
            aria-label="Fechar menu de navegacao"
            onClick={() => setIsDrawerOpen(false)}
            className="absolute inset-0 bg-background/80"
          />
          <div className="relative z-10 flex h-full w-72 max-w-[80vw] flex-col bg-card">
            <div className="flex items-center justify-between border-b border-border px-4 py-3">
              <BrandTitle />
              <button
                type="button"
                onClick={() => setIsDrawerOpen(false)}
                aria-label="Fechar menu de navegacao"
                className="flex size-9 items-center justify-center rounded-lg text-text-muted hover:bg-secondary hover:text-text-body"
              >
                <X className="size-5" strokeWidth={1.6} />
              </button>
            </div>
            <NavList onNavigate={() => setIsDrawerOpen(false)} />
            <UserFooter username={usuario?.username} onLogout={logout} />
          </div>
        </div>
      )}

      {/* Sidebar fixa (>= md). */}
      <aside className="hidden shrink-0 border-r border-border bg-card md:sticky md:top-0 md:flex md:h-svh md:w-64 md:flex-col">
        <div className="border-b border-border px-4 py-4">
          <BrandTitle />
        </div>
        <NavList />
        <UserFooter username={usuario?.username} onLogout={logout} />
      </aside>

      <main className="flex-1 px-4 py-8">
        <div className="mx-auto w-full max-w-2xl">{children}</div>
      </main>
    </div>
  )
}
