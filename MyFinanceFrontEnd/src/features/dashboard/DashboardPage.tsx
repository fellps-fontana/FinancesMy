import { useEffect, useState } from "react"
import { AcoesRapidas } from "@/features/dashboard/components/AcoesRapidas"
import {
  CardMovimentoMes,
  CardSaldoProjetado,
} from "@/features/dashboard/components/CardSaldoProjetado"
import { ContasAPagarResumo } from "@/features/dashboard/components/ContasAPagarResumo"
import { CardSaldoTotal, ContasResumo } from "@/features/dashboard/components/ContasResumo"
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
import { useAuth } from "@/features/auth/useAuth"
import { GraficoConsolidadoAtivos } from "@/features/investimentos/components/GraficoConsolidadoAtivos"
import { GraficoRendimentosPorTipo } from "@/features/investimentos/components/GraficoRendimentosPorTipo"
import { useResumoAtivos } from "@/features/investimentos/hooks/useResumoAtivos"

const hoje = new Date()
const formatadorDataCompleta = new Intl.DateTimeFormat("pt-BR", {
  weekday: "long",
  day: "numeric",
  month: "long",
  year: "numeric",
})
const formatadorDataCurta = new Intl.DateTimeFormat("pt-BR", {
  weekday: "long",
  day: "numeric",
  month: "long",
})

function capitalizar(texto: string): string {
  return texto.charAt(0).toUpperCase() + texto.slice(1)
}
const anoAtual = hoje.getFullYear()
const mesAtual = hoje.getMonth() + 1

/**
 * Pagina raiz do dashboard (rota "/"), componente roteado da feature
 * (stack.md "Estrutura de pastas (src/)" - raiz da feature = alvo direto de
 * uma <Route>). Compoe os dois indicadores do mes corrente
 * (regra-de-negocio.md itens 9 e 14): CardSaldoProjetado (projecao do mes) e
 * LimiteGastoIndicador (sublimite do planejamento mensal). Sem seletor de
 * mes/ano nesta leva - sempre o mes corrente via `new Date()`.
 *
 * Saudacao "Ola, {usuario}" + data (mockups 02 Dashboard) vive aqui; o botao
 * Sair continua so no AppShell. Layout segue o mockup: desktop = 3 cards de
 * topo + grade 1.6fr/1fr (ultimos lancamentos | contas a pagar + contas);
 * mobile = saldo, acoes rapidas, resumo do mes, ultimos lancamentos. Widgets
 * fora do mockup ficam abaixo da grade.
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
 * reinterpreta ou recalcula o que ele mostra. `SeletorWidgets` fica no FIM
 * da pagina (depois de todos os widgets), colapsado por padrao -- e
 * configuracao, nao conteudo, entao nao deve competir com o dado real pelo
 * topo da tela (ver SeletorWidgets.tsx para o comportamento de expandir).
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

  const { usuario } = useAuth()

  return (
    <div className="flex flex-col gap-5 md:gap-6">
      <header>
        <h1 className="text-[19px] leading-snug font-medium text-text-primary">
          Ola, {usuario?.username}
        </h1>
        <p className="mt-0.5 text-[13px] text-text-muted">
          <span className="md:hidden">{capitalizar(formatadorDataCurta.format(hoje))}</span>
          <span className="hidden md:inline">{capitalizar(formatadorDataCompleta.format(hoje))}</span>
        </p>
      </header>

      {/* Linha de topo: mobile empilha saldo -> acoes rapidas -> resumo do
          mes; desktop vira 3 cards lado a lado (saldo, entradas, saidas). */}
      <div className="grid gap-5 md:grid-cols-[1.4fr_1fr_1fr] md:gap-4">
        <CardSaldoTotal />
        {preferencia["acoes-rapidas"] && <AcoesRapidas className="md:hidden" />}
        <section className="flex flex-col gap-2.5 md:contents" aria-label="Resumo do mes">
          <h2 className="text-[13px] font-normal text-text-muted md:hidden">Resumo do mes</h2>
          <div className="grid grid-cols-2 gap-2.5 md:contents">
            <CardMovimentoMes tipo="entradas" ano={anoAtual} mes={mesAtual} />
            <CardMovimentoMes tipo="saidas" ano={anoAtual} mes={mesAtual} />
          </div>
        </section>
      </div>

      {/* Grade do mockup desktop: ultimos lancamentos + coluna de contas.
          Contas a pagar e Contas so existem no desktop (mockup mobile nao os
          tem). */}
      <div className="grid gap-4 md:grid-cols-[1.6fr_1fr]">
        {preferencia["ultimos-lancamentos"] && <UltimosLancamentos />}
        <div className="hidden flex-col gap-4 md:flex">
          <ContasAPagarResumo />
          <ContasResumo />
        </div>
      </div>

      {/* Widgets alem do mockup, mantidos por decisao do usuario. */}
      {preferencia["saldo-projetado"] && <CardSaldoProjetado ano={anoAtual} mes={mesAtual} />}
      {preferencia["grafico-entradas-saidas"] && (
        <GraficoEntradasSaidas ano={anoAtual} mes={mesAtual} />
      )}
      {preferencia["limite-gasto"] && <LimiteGastoIndicador ano={anoAtual} mes={mesAtual} />}
      {preferencia["investimentos"] && (
        <GraficoConsolidadoAtivos resumo={resumoAtivos} carregando={carregandoResumoAtivos} />
      )}
      {preferencia["rendimentos"] && <GraficoRendimentosPorTipo />}

      <SeletorWidgets preferencia={preferencia} onAlternarWidget={alternarWidget} />
    </div>
  )
}
