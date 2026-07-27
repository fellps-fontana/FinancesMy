import { Navigate, Route, Routes } from "react-router-dom"
import { LoginPage } from "@/features/auth/LoginPage"
import { DashboardPage } from "@/features/dashboard/DashboardPage"
import { ProtectedRoute } from "@/app/ProtectedRoute"
import { ListaAtivosPage } from "@/features/investimentos/ListaAtivosPage"
import { ListaContasSimplesPage } from "@/features/investimentos/ListaContasSimplesPage"
import { ContaCartaoPage } from "@/features/cartao/ContaCartaoPage"
import { RelatorioCategoriaPage } from "@/features/cartao/RelatorioCategoriaPage"
import { ComparativoLimiteGastoPage } from "@/features/limite-gasto/ComparativoLimiteGastoPage"
import { ListaContasReceber } from "@/features/contas-receber/ListaContasReceber"
import { ListaContasFixas } from "@/features/contas-fixas/ListaContasFixas"

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <DashboardPage />
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
      <Route
        path="/limites-gasto"
        element={
          <ProtectedRoute>
            <ComparativoLimiteGastoPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contas-receber"
        element={
          <ProtectedRoute>
            <ListaContasReceber />
          </ProtectedRoute>
        }
      />
      <Route
        path="/contas-fixas"
        element={
          <ProtectedRoute>
            <ListaContasFixas />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
