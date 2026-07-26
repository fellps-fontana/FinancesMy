import { useAuth } from "@/features/auth/useAuth"
import { Button } from "@/shared/ui/button"
import { CardSaldoProjetado } from "@/features/dashboard/components/CardSaldoProjetado"
import { GraficoEntradasSaidas } from "@/features/dashboard/components/GraficoEntradasSaidas"
import { LimiteGastoIndicador } from "@/features/dashboard/components/LimiteGastoIndicador"

const hoje = new Date()
const anoAtual = hoje.getFullYear()
const mesAtual = hoje.getMonth() + 1

/**
 * Pagina raiz do dashboard (rota "/"), componente roteado da feature
 * (stack.md "Estrutura de pastas (src/)" - raiz da feature = alvo direto de
 * uma <Route>). Substitui o placeholder Home.tsx: mantem a mesma saudacao +
 * logout (useAuth) que provava a guarda de rota, e agora compoe os dois
 * indicadores do mes corrente (regra-de-negocio.md itens 9 e 14):
 * CardSaldoProjetado (projecao do mes) e LimiteGastoIndicador (limite de
 * gasto por categoria). Sem seletor de mes/ano nesta leva - sempre o mes
 * corrente via `new Date()`, mesmo padrao ja usado em
 * ComparativoLimiteGastoPage.tsx.
 *
 * Container puro: nenhum calculo de dominio mora aqui - cada card busca seus
 * proprios dados via hook interno (useProjecaoMes,
 * useGastoVsLimiteTodasCategorias), este componente so os posiciona na
 * pagina. GraficoEntradasSaidas usa o mesmo useProjecaoMes de
 * CardSaldoProjetado (mesmo periodo), so agrupando os 4 termos em 2 barras
 * para comparacao visual.
 */
export function DashboardPage() {
  const { usuario, logout } = useAuth()

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex items-center justify-between gap-4">
        <p className="text-[19px] font-medium text-text-primary">Ola, {usuario?.username}</p>
        <Button variant="outline" onClick={logout}>
          Sair
        </Button>
      </header>

      <div className="flex flex-col gap-4">
        <CardSaldoProjetado ano={anoAtual} mes={mesAtual} />
        <GraficoEntradasSaidas ano={anoAtual} mes={mesAtual} />
        <LimiteGastoIndicador ano={anoAtual} mes={mesAtual} />
      </div>
    </div>
  )
}
