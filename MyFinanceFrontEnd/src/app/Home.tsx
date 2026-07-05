import { useAuth } from "@/features/auth/useAuth"
import { Button } from "@/shared/ui/button"

// Placeholder para provar que a guarda de rota funciona. As features reais
// (dashboard, contas, lancamentos...) ainda nao foram implementadas - ver
// pastas stub em src/features/.
export function Home() {
  const { usuario, logout } = useAuth()

  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-4 bg-background px-4 text-center">
      <div>
        <p className="text-[19px] font-medium text-foreground">Ola, {usuario?.username}</p>
        <p className="text-sm text-muted-foreground">
          Guarda de rota funcionando. Modulos ainda nao implementados.
        </p>
      </div>
      <Button variant="outline" onClick={logout}>
        Sair
      </Button>
    </div>
  )
}
