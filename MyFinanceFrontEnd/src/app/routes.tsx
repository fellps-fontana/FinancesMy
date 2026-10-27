import { Navigate, Route, Routes, useLocation } from "react-router-dom"
import { LoginPage } from "@/features/auth/LoginPage"
import { DashboardPage } from "@/features/dashboard/DashboardPage"
import { AuthenticatedLayout } from "@/app/AuthenticatedLayout"
import { ListaAtivosPage } from "@/features/investimentos/ListaAtivosPage"
import { ContasPage } from "@/features/contas/ContasPage"
import { ContaCartaoPage } from "@/features/cartao/ContaCartaoPage"
import { PlanejamentoMensalPage } from "@/features/planejamento-mensal/PlanejamentoMensalPage"
import { ListaContasReceber } from "@/features/contas-receber/ListaContasReceber"
import { ListaContasFixas } from "@/features/contas-fixas/ListaContasFixas"
import { ListaRecebiveisRecorrentes } from "@/features/recebiveis-recorrentes/ListaRecebiveisRecorrentes"
import { CategoriasPage } from "@/features/categorias/CategoriasPage"
import { LancamentosPage } from "@/features/lancamentos/LancamentosPage"
import { MaisPage } from "@/app/MaisPage"
import { DeParaPage } from "@/features/de-para/DeParaPage"
import { ConciliacaoPage } from "@/features/conciliacao/ConciliacaoPage"
import { RelatorioCategoriaPage } from "@/features/relatorios/RelatorioCategoriaPage"
import { RelatorioCategoriaDetalhePage } from "@/features/relatorios/RelatorioCategoriaDetalhePage"

function RedirecionarParaPlanejamento() {
  const location = useLocation()
  return <Navigate to={{ pathname: "/planejamento", search: location.search }} replace />
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/registrar" element={<LoginPage initialMode="register" />} />
      <Route element={<AuthenticatedLayout />}>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/investimentos" element={<ListaAtivosPage />} />
        <Route path="/contas" element={<ContasPage />} />
        <Route path="/cartao" element={<ContaCartaoPage />} />
        <Route path="/limites-gasto" element={<RedirecionarParaPlanejamento />} />
        <Route path="/planejamento" element={<PlanejamentoMensalPage />} />
        <Route path="/contas-receber" element={<ListaContasReceber />} />
        <Route path="/contas-fixas" element={<ListaContasFixas />} />
        <Route path="/recebiveis-recorrentes" element={<ListaRecebiveisRecorrentes />} />
        <Route path="/categorias" element={<CategoriasPage />} />
        <Route path="/de-para" element={<DeParaPage />} />
        <Route path="/conciliacao" element={<ConciliacaoPage />} />
        <Route path="/lancamentos" element={<LancamentosPage />} />
        <Route path="/relatorio" element={<RelatorioCategoriaPage />} />
        <Route path="/relatorio/sem-categoria" element={<RelatorioCategoriaDetalhePage />} />
        <Route path="/relatorio/:categoriaId" element={<RelatorioCategoriaDetalhePage />} />
        <Route path="/mais" element={<MaisPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
