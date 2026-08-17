import { useMemo, useState } from "react"
import { Plus } from "lucide-react"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { useFluxoCaixaTodasContas } from "@/features/lancamentos/hooks/useFluxoCaixaTodasContas"
import { mesAtualIso } from "@/features/cartao/lib/formatarData"
import {
  agruparLancamentosPorData,
  calcularResumoLancamentos,
  filtrarLancamentosDoMes,
  filtrarPorTipo,
  somarMeses,
  type FiltroTipoLancamento as FiltroTipoLancamentoValor,
} from "@/features/lancamentos/lib/filtrarPeriodo"
import { NavegadorMes } from "@/features/lancamentos/components/NavegadorMes"
import { FiltroTipoLancamento } from "@/features/lancamentos/components/FiltroTipoLancamento"
import { LancamentoItem } from "@/features/lancamentos/components/LancamentoItem"
import { TransferenciaFluxoCaixaItem } from "@/features/lancamentos/components/TransferenciaFluxoCaixaItem"
import { FormLancamento } from "@/features/lancamentos/components/FormLancamento"
import { FormTransferencia } from "@/features/lancamentos/components/FormTransferencia"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import type { LancamentoResponse } from "@/features/lancamentos/types"

type TipoNovoRegistro = "LANCAMENTO" | "TRANSFERENCIA"

// Estado de qual formulario esta aberto (mesmo espirito de CategoriasPage.tsx
// "EstadoFormulario"): `null` fecha tudo; `{ modo: "criar" }` abre o
// segmented control Lancamento/Transferencia; `{ modo: "editar" }` reabre
// FormLancamento com `lancamentoParaEditar` (o unico caminho de edicao e
// lancamento avulso - transferencia nao tem edicao nesta task).
type EstadoFormulario =
  | { modo: "criar"; tipo: TipoNovoRegistro }
  | { modo: "editar"; lancamento: LancamentoResponse }
  | null

// Componente roteado da feature (stack.md "raiz da feature"). Visao agregada
// de TODAS as contas por padrao (mockup "04 Lancamentos"): useFluxoCaixaTodasContas
// consome o endpoint agregado (GET /api/lancamentos/fluxo-caixa), entao a
// pagina abre ja mostrando os lancamentos e transferencias do mes, sem exigir
// selecao previa de conta (a selecao de conta migrou para dentro de
// FormLancamento, so no modo criar - ver components/FormLancamento.tsx).
//
// Container puro: nenhum calculo de dominio mora aqui - a classificacao
// DEBIT/CREDIT vive em LancamentoItem, o recorte/resumo/agrupamento de
// periodo vem de lib/filtrarPeriodo.ts (estendido nesta leva para operar
// sobre FluxoCaixaItem, a uniao LANCAMENTO/TRANSFERENCIA) e a mutacao de
// dados vive nos hooks consumidos por FormLancamento/FormTransferencia. Esta
// pagina so decide QUAL formulario esta aberto e QUAL mes/filtro estao em
// foco. Cada item da lista renderiza LancamentoItem ou TransferenciaFluxoCaixaItem
// conforme `tipoItem` - a decisao de qual componente usar fica aqui no pai;
// nenhum dos dois componentes de item sabe da uniao.
export function LancamentosPage() {
  const [formulario, setFormulario] = useState<EstadoFormulario>(null)
  const [mesReferencia, setMesReferencia] = useState(() => mesAtualIso())
  const [filtroTipo, setFiltroTipo] = useState<FiltroTipoLancamentoValor>("TODOS")

  const { data: itens, isLoading: carregandoItens, error: erroItens } = useFluxoCaixaTodasContas()

  if (erroItens) {
    console.error("Falha ao carregar fluxo de caixa", erroItens)
  }

  // Recorte de mes -> resumo do mes inteiro (Entradas/Saidas/Saldo, mockup
  // "04 Lancamentos") -> filtro de tipo (chip) -> agrupamento por data pra
  // renderizar a lista. O resumo usa `itensDoMes` (antes do filtro de tipo)
  // porque os 3 cards representam o mes inteiro, independente do chip
  // selecionado - o chip so afeta a LISTA abaixo. TRANSFERENCIA nunca entra
  // no resumo (regra-de-negocio.md itens 3 e 12, ver calcularResumoLancamentos).
  const [ano, mes] = mesReferencia.split("-").map(Number)

  const itensDoMes = useMemo(
    () => filtrarLancamentosDoMes(itens ?? [], ano, mes),
    [itens, ano, mes],
  )

  const resumo = useMemo(() => calcularResumoLancamentos(itensDoMes), [itensDoMes])

  const gruposPorData = useMemo(
    () => agruparLancamentosPorData(filtrarPorTipo(itensDoMes, filtroTipo)),
    [itensDoMes, filtroTipo],
  )

  function handleAlternarNovo() {
    setFormulario((atual) => (atual?.modo === "criar" ? null : { modo: "criar", tipo: "LANCAMENTO" }))
  }

  function handleTrocarTipoNovo(tipo: TipoNovoRegistro) {
    setFormulario({ modo: "criar", tipo })
  }

  function handleEditar(lancamento: LancamentoResponse) {
    setFormulario({ modo: "editar", lancamento })
  }

  function handleFecharFormulario() {
    setFormulario(null)
  }

  function handleMesAnterior() {
    setMesReferencia((atual) => somarMeses(atual, -1))
  }

  function handleProximoMes() {
    setMesReferencia((atual) => somarMeses(atual, 1))
  }

  return (
    <div className="relative mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8 pb-16 md:pb-8">
      <header className="flex items-center justify-between gap-2">
        <div className="flex flex-col gap-1">
          <h1 className="text-[19px] font-medium text-text-primary">Lancamentos</h1>
          <p className="text-sm text-text-muted">
            Fluxo de caixa de todas as contas: entradas e saidas reais do mes.
          </p>
        </div>
      </header>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <NavegadorMes
          mesReferencia={mesReferencia}
          onMesAnterior={handleMesAnterior}
          onProximoMes={handleProximoMes}
        />
        {/* No mobile a acao de criar vive no FAB fixo (mesmo padrao do
            mockup "04 Lancamentos"); no desktop o botao fica no topo, ao
            lado do navegador de mes. */}
        <Button type="button" onClick={handleAlternarNovo} className="hidden md:inline-flex">
          {formulario?.modo === "criar" ? "Cancelar" : "Novo lancamento"}
        </Button>
      </div>

      {/* Resumo do mes (mockup "04 Lancamentos": 3 cards Entradas/Saidas/
          Saldo). Valores ja formatados no locale pt-BR (formatarMoeda) -
          nenhum numero cru na tela. Soma vem de calcularResumoLancamentos
          (lib/filtrarPeriodo.ts), respeitando `tipo` (regra-de-negocio.md
          item 2, CRITICA) e ignorando toda linha TRANSFERENCIA (itens 3 e
          12) - nao um calculo inline aqui. */}
      <div className="grid grid-cols-3 gap-2.5">
        <div className="flex flex-col gap-1 rounded-xl border border-border bg-card px-3 py-2.5">
          <span className="text-[12px] text-positivo">Entradas</span>
          <span className="text-[16px] font-medium text-text-primary">
            {formatarMoeda(resumo.totalEntradas)}
          </span>
        </div>
        <div className="flex flex-col gap-1 rounded-xl border border-border bg-card px-3 py-2.5">
          <span className="text-[12px] text-negativo">Saidas</span>
          <span className="text-[16px] font-medium text-text-primary">
            {formatarMoeda(resumo.totalSaidas)}
          </span>
        </div>
        <div className="flex flex-col gap-1 rounded-xl border border-border bg-card px-3 py-2.5">
          <span className="text-[12px] text-text-muted">Saldo</span>
          <span className="text-[16px] font-medium text-text-primary">
            {formatarMoeda(resumo.saldo)}
          </span>
        </div>
      </div>

      <FiltroTipoLancamento valor={filtroTipo} onChange={setFiltroTipo} />

      {formulario?.modo === "criar" && (
        <div className="flex flex-col gap-3">
          {/* Segmented control Lancamento/Transferencia - mesmo padrao ja
              usado no toggle Recebivel/Emprestimo de
              FormRegistrarContaReceber.tsx (dois Button com variant
              condicional, sem componente de tabs novo). FormLancamento tem
              seu proprio select de conta (banco/investimento); FormTransferencia
              tem seus proprios selects de origem/destino - nenhum dos dois
              depende de uma "conta em foco" desta pagina. */}
          <div className="flex gap-2" role="group" aria-label="Tipo de novo registro">
            <Button
              type="button"
              variant={formulario.tipo === "LANCAMENTO" ? "default" : "outline"}
              onClick={() => handleTrocarTipoNovo("LANCAMENTO")}
              className="flex-1"
            >
              Lancamento
            </Button>
            <Button
              type="button"
              variant={formulario.tipo === "TRANSFERENCIA" ? "default" : "outline"}
              onClick={() => handleTrocarTipoNovo("TRANSFERENCIA")}
              className="flex-1"
            >
              Transferencia
            </Button>
          </div>

          {formulario.tipo === "LANCAMENTO" ? (
            <FormLancamento onSalvar={handleFecharFormulario} />
          ) : (
            <FormTransferencia onSalvar={handleFecharFormulario} />
          )}
        </div>
      )}

      {formulario?.modo === "editar" && (
        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between gap-2">
            <span className="text-[13px] text-text-muted">Editando lancamento</span>
            <Button type="button" variant="ghost" size="sm" onClick={handleFecharFormulario}>
              Cancelar
            </Button>
          </div>
          <FormLancamento
            lancamentoParaEditar={formulario.lancamento}
            onSalvar={handleFecharFormulario}
          />
        </div>
      )}

      {erroItens ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar os lancamentos</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : carregandoItens ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : gruposPorData.length > 0 ? (
        <div className="flex flex-col gap-4">
          {gruposPorData.map((grupo) => (
            <div key={grupo.data} className="flex flex-col gap-2">
              <span className="text-[12px] text-text-faint">{grupo.rotulo}</span>
              <div className="flex flex-col gap-2.5">
                {grupo.itens.map((item) =>
                  item.tipoItem === "LANCAMENTO" ? (
                    <LancamentoItem
                      key={item.lancamento.id}
                      lancamento={item.lancamento}
                      onEditar={handleEditar}
                    />
                  ) : (
                    <TransferenciaFluxoCaixaItem
                      key={item.transferencia.id}
                      transferencia={item.transferencia}
                    />
                  ),
                )}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhum lancamento neste mes.</p>
      )}

      {/* FAB (mockup "04 Lancamentos", variante mobile) - so em telas < md,
          onde nao ha espaco no header pro botao "Novo lancamento". */}
      <button
        type="button"
        onClick={handleAlternarNovo}
        aria-label={formulario?.modo === "criar" ? "Cancelar novo lancamento" : "Novo lancamento"}
        className="fixed right-5 bottom-20 flex size-13 items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-lg shadow-primary/35 md:hidden"
      >
        <Plus className="size-5" strokeWidth={2} />
      </button>
    </div>
  )
}
