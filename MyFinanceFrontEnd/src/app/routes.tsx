import { Navigate, Route, Routes } from "react-router-dom"
import { LoginPage } from "@/features/auth/LoginPage"
import { Home } from "@/app/Home"
import { ProtectedRoute } from "@/app/ProtectedRoute"
import { ListaAtivosPage } from "@/features/investimentos/ListaAtivosPage"
import { ListaContasSimplesPage } from "@/features/investimentos/ListaContasSimplesPage"
import { ContaCartaoPage } from "@/features/cartao/ContaCartaoPage"
import { RelatorioCategoriaPage } from "@/features/cartao/RelatorioCategoriaPage"

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
            <ListaAtivosPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contas"
        element={
          <ProtectedRoute>
            <ListaContasSimplesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/cartao"
        element={
          <ProtectedRoute>
            <ContaCartaoPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/cartao/relatorio"
        element={
          <ProtectedRoute>
            <RelatorioCategoriaPage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
