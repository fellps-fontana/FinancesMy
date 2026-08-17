import { Navigate, Route, Routes } from "react-router-dom"
import { LoginPage } from "@/features/auth/LoginPage"
import { DashboardPage } from "@/features/dashboard/DashboardPage"
import { AuthenticatedLayout } from "@/app/AuthenticatedLayout"
import { ListaAtivosPage } from "@/features/investimentos/ListaAtivosPage"
import { ContasPage } from "@/features/contas/ContasPage"
import { ContaCartaoPage } from "@/features/cartao/ContaCartaoPage"
import { RelatorioCategoriaPage } from "@/features/cartao/RelatorioCategoriaPage"
import { ComparativoLimiteGastoPage } from "@/features/limite-gasto/ComparativoLimiteGastoPage"
import { ListaContasReceber } from "@/features/contas-receber/ListaContasReceber"
import { ListaContasFixas } from "@/features/contas-fixas/ListaContasFixas"
import { CategoriasPage } from "@/features/categorias/CategoriasPage"
import { LancamentosPage } from "@/features/lancamentos/LancamentosPage"

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<AuthenticatedLayout />}>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/investimentos" element={<ListaAtivosPage />} />
        <Route path="/contas" element={<ContasPage />} />
        <Route path="/cartao" element={<ContaCartaoPage />} />
        <Route path="/cartao/relatorio" element={<RelatorioCategoriaPage />} />
        <Route path="/limites-gasto" element={<ComparativoLimiteGastoPage />} />
        <Route path="/contas-receber" element={<ListaContasReceber />} />
        <Route path="/contas-fixas" element={<ListaContasFixas />} />
        <Route path="/categorias" element={<CategoriasPage />} />
        <Route path="/lancamentos" element={<LancamentosPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
