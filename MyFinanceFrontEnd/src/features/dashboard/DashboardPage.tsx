import { useEffect, useState } from "react"
import { AcoesRapidas } from "@/features/dashboard/components/AcoesRapidas"
import { CardSaldoProjetado } from "@/features/dashboard/components/CardSaldoProjetado"
import { GraficoEntradasSaidas } from "@/features/dashboard/components/GraficoEntradasSaidas"
import { LimiteGastoIndicador } from "@/features/dashboard/components/LimiteGastoIndicador"
import { UltimosLancamentos } from "@/features/dashboard/components/UltimosLancamentos"
import { SeletorWidgets } from "@/features/dashboard/components/SeletorWidgets"
import {
  lerPreferenciaWidgets,
  salvarPreferenciaWidgets,
  type PreferenciaWidgets,
  type WidgetId,
} from "@/features/dashboard/lib/preferenciaWidgets"
import { GraficoConsolidadoAtivos } from "@/features/investimentos/components/GraficoConsolidadoAtivos"
import { GraficoRendimentosPorTipo } from "@/features/investimentos/components/GraficoRendimentosPorTipo"
import { useResumoAtivos } from "@/features/investimentos/hooks/useResumoAtivos"

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
 * para comparacao visual. Excecao unica: GraficoConsolidadoAtivos (item 8,
 * "Resumo por tipo") e um componente de apresentacao puro que recebe
 * `resumo`/`carregando` via props (nao busca sozinho, ver
 * GraficoConsolidadoAtivos.tsx) -- por isso este container chama
 * `useResumoAtivos` para alimenta-lo, mesmo padrao ja usado por
 * ListaAtivosPage.tsx (investimentos), a unica outra tela que consome esse
 * componente hoje.
 *
 * AcoesRapidas (mockup "02 Dashboard.dc.html") fica logo abaixo do saldo,
 * mesma ordem do mockup: saldo -> acoes rapidas -> resumo do mes. Tambem
 * puramente navegacional, sem estado proprio (ver componente).
 *
 * Widgets escolhiveis (liga/desliga, preferencia so em localStorage - sem
 * endpoint de backend): a lista de widgets exibidos vem de `preferencia`
 * (estado de UI local, inicializado por `lerPreferenciaWidgets` e persistido
 * a cada mudanca por `salvarPreferenciaWidgets`, ambos em
 * lib/preferenciaWidgets.ts). CardSaldoProjetado continua o MESMO
 * componente/formula da regra-de-negocio.md item 9 (CRITICA) em qualquer
 * estado do switch -- o toggle so decide SE ele e montado na arvore, nunca
 * reinterpreta ou recalcula o que ele mostra.
 */
export function DashboardPage() {
  const [preferencia, setPreferencia] = useState<PreferenciaWidgets>(lerPreferenciaWidgets)
  const { data: resumoAtivos, isLoading: carregandoResumoAtivos } = useResumoAtivos()

  useEffect(() => {
    salvarPreferenciaWidgets(preferencia)
  }, [preferencia])

  function alternarWidget(id: WidgetId) {
    setPreferencia((atual) => ({ ...atual, [id]: !atual[id] }))
  }

  return (
    <div className="flex flex-col gap-4">
      <SeletorWidgets preferencia={preferencia} onAlternarWidget={alternarWidget} />

      {preferencia["saldo-projetado"] && <CardSaldoProjetado ano={anoAtual} mes={mesAtual} />}
      {preferencia["acoes-rapidas"] && <AcoesRapidas />}
      {preferencia["grafico-entradas-saidas"] && (
        <GraficoEntradasSaidas ano={anoAtual} mes={mesAtual} />
      )}
      {preferencia["limite-gasto"] && <LimiteGastoIndicador ano={anoAtual} mes={mesAtual} />}
      {preferencia["investimentos"] && (
        <GraficoConsolidadoAtivos resumo={resumoAtivos} carregando={carregandoResumoAtivos} />
      )}
      {preferencia["rendimentos"] && <GraficoRendimentosPorTipo />}
      {preferencia["ultimos-lancamentos"] && <UltimosLancamentos />}
    </div>
  )
}
