import { CardSaldoProjetado } from "@/features/dashboard/components/CardSaldoProjetado"
import { GraficoEntradasSaidas } from "@/features/dashboard/components/GraficoEntradasSaidas"
import { LimiteGastoIndicador } from "@/features/dashboard/components/LimiteGastoIndicador"

const hoje = new Date()
const anoAtual = hoje.getFullYear()
const mesAtual = hoje.getMonth() + 1

/**
 * Pagina raiz do dashboard (rota "/"), componente roteado da feature
 * (stack.md "Estrutura de pastas (src/)" - raiz da feature = alvo direto de
 * uma <Route>). Compoe os dois indicadores do mes corrente
 * (regra-de-negocio.md itens 9 e 14): CardSaldoProjetado (projecao do mes) e
 * LimiteGastoIndicador (limite de gasto por categoria). Sem seletor de
 * mes/ano nesta leva - sempre o mes corrente via `new Date()`, mesmo padrao
 * ja usado em ComparativoLimiteGastoPage.tsx.
 *
 * Saudacao "Ola, {usuario}" e botao Sair NAO vivem mais aqui: `AppShell`
 * (montado por `AuthenticatedLayout` em volta de toda pagina protegida, ver
 * app/AppShell.tsx) ja exibe os dois globalmente na sidebar/topbar. Repetir
 * aqui duplicava identidade do usuario e acao de logout na mesma tela.
 *
 * Container puro: nenhum calculo de dominio mora aqui - cada card busca seus
 * proprios dados via hook interno (useProjecaoMes,
 * useGastoVsLimiteTodasCategorias), este componente so os posiciona na
 * pagina. GraficoEntradasSaidas usa o mesmo useProjecaoMes de
 * CardSaldoProjetado (mesmo periodo), so agrupando os 4 termos em 2 barras
 * para comparacao visual.
 */
export function DashboardPage() {
  return (
    <div className="flex flex-col gap-4">
      <CardSaldoProjetado ano={anoAtual} mes={mesAtual} />
      <GraficoEntradasSaidas ano={anoAtual} mes={mesAtual} />
      <LimiteGastoIndicador ano={anoAtual} mes={mesAtual} />
    </div>
  )
}
