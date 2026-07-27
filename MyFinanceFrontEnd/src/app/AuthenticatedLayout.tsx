import { Outlet } from "react-router-dom"
import { ProtectedRoute } from "@/app/ProtectedRoute"
import { AppShell } from "@/app/AppShell"

// Layout pai de toda rota autenticada: ProtectedRoute decide se o usuario
// pode entrar (senao redireciona a /login), AppShell da a casca visual
// (sidebar/topbar), Outlet renderiza a pagina da rota filha ativa.
export function AuthenticatedLayout() {
  return (
    <ProtectedRoute>
      <AppShell>
        <Outlet />
      </AppShell>
    </ProtectedRoute>
  )
}
