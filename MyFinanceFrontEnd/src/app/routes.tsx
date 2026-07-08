import { Navigate, Route, Routes } from "react-router-dom"
import { LoginPage } from "@/features/auth/LoginPage"
import { Home } from "@/app/Home"
import { ProtectedRoute } from "@/app/ProtectedRoute"
import { ListaContasInvestimento } from "@/features/investimentos/ListaContasInvestimento"

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <Home />
          </ProtectedRoute>
        }
      />
      <Route
        path="/investimentos"
        element={
          <ProtectedRoute>
            <ListaContasInvestimento />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
