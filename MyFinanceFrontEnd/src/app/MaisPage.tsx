import { Link } from "react-router-dom"
import { ChevronRight, LogOut } from "lucide-react"
import { useAuth } from "@/features/auth/useAuth"
import { ThemeToggle } from "@/app/components/ThemeToggle"
import { NAV_GROUPS, type NavGroup } from "@/app/navigation"

// Grupos secundarios (Organizacao, Acompanhamento): tudo que nao esta na
// barra inferior. O primeiro grupo (principal) ja vive nas 4 abas fixas.
const SECONDARY_GROUPS: NavGroup[] = NAV_GROUPS.slice(1)

function UserCard({ username, email }: { username: string; email: string }) {
  return (
    <div className="flex items-center gap-3 rounded-2xl border border-border bg-card p-4">
      <div className="flex size-[42px] shrink-0 items-center justify-center rounded-xl bg-accent-deep text-[15px] font-medium text-accent-soft">
        {username.charAt(0).toUpperCase()}
      </div>
      <div className="min-w-0">
        <div className="truncate text-[15px] font-medium text-text-primary">{username}</div>
        <div className="mt-0.5 truncate text-xs text-text-muted">{email}</div>
      </div>
    </div>
  )
}

function NavGroupCard({ group }: { group: NavGroup }) {
  return (
    <section className="flex flex-col gap-2.5">
      {group.label && <h2 className="text-[13px] text-text-muted">{group.label}</h2>}
      <div className="overflow-hidden rounded-[14px] border border-border bg-card">
        {group.items.map(({ to, label, icon: Icon }, index) => (
          <Link
            key={to}
            to={to}
            className={`flex items-center gap-3 px-3.5 py-3 transition-colors hover:bg-secondary ${
              index > 0 ? "border-t border-border" : ""
            }`}
          >
            <span className="flex size-8 shrink-0 items-center justify-center rounded-[9px] bg-accent-deep">
              <Icon className="size-4 text-accent-soft" strokeWidth={1.6} />
            </span>
            <span className="flex-1 text-sm text-text-body">{label}</span>
            <ChevronRight className="size-3.5 text-text-faint" strokeWidth={2} />
          </Link>
        ))}
      </div>
    </section>
  )
}

// Tela "Mais" (mobile): substitui a antiga folha da BottomTabBar. Em >= md a
// navegacao fica na sidebar, entao a rota so e alcancada pela aba mobile.
export function MaisPage() {
  const { usuario, logout } = useAuth()

  return (
    <div className="flex flex-col gap-5">
      <h1 className="text-[19px] font-medium text-text-primary">Mais</h1>
      {usuario && <UserCard username={usuario.username} email={usuario.email} />}
      {SECONDARY_GROUPS.map((group) => (
        <NavGroupCard key={group.label} group={group} />
      ))}
      <div className="flex items-center gap-2 overflow-hidden rounded-[14px] border border-border bg-card pr-3">
        <button
          type="button"
          onClick={logout}
          className="flex flex-1 items-center gap-3 px-3.5 py-3 text-left transition-colors hover:bg-secondary"
        >
          <span className="flex size-8 shrink-0 items-center justify-center rounded-[9px] bg-secondary">
            <LogOut className="size-4 text-destructive" strokeWidth={1.6} />
          </span>
          <span className="text-sm text-destructive">Sair</span>
        </button>
        <ThemeToggle />
      </div>
    </div>
  )
}
