import { useMemo, useState } from "react"
import { cn } from "@/shared/lib/utils"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Label } from "@/shared/ui/label"
import { useContasParaSelecao } from "@/features/contas-receber/hooks/useContasParaSelecao"
import { useFluxoCaixa } from "@/features/lancamentos/hooks/useFluxoCaixa"
import { filtrarLancamentosDoMes } from "@/features/lancamentos/lib/filtrarPeriodo"
import { LancamentoItem } from "@/features/lancamentos/components/LancamentoItem"
import { FormLancamento } from "@/features/lancamentos/components/FormLancamento"
import { FormTransferencia } from "@/features/lancamentos/components/FormTransferencia"
import type { LancamentoResponse } from "@/features/lancamentos/types"

// Mes calendario corrente (regra-de-negocio.md item 9) - sem seletor de mes/
// ano nesta leva, mesmo padrao ja usado em DashboardPage.tsx e
// ComparativoLimiteGastoPage.tsx. `mes` e 1-indexed, mesma convencao de
// filtrarLancamentosDoMes (lib/filtrarPeriodo.ts).
const hoje = new Date()
const anoAtual = hoje.getFullYear()
const mesAtual = hoje.getMonth() + 1

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

const CLASSE_SELECT =
  "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30"

// Componente roteado da feature (stack.md "raiz da feature"). Container puro:
// nenhum calculo de dominio mora aqui - a classificacao DEBIT/CREDIT vive em
// LancamentoItem, o recorte de mes vem de filtrarLancamentosDoMes (lib/
// filtrarPeriodo.ts, ja aprovado) e a mutacao de dados vive nos hooks
// consumidos por FormLancamento/FormTransferencia. Esta pagina so decide QUAL
// conta esta em foco e QUAL formulario esta aberto.
//
// Selecao de conta reaproveita useContasParaSelecao (banco + investimento,
// sem cartao - mesmo universo ja usado em FormTransferencia/
// FormRegistrarContaReceber para origem/destino de mesma titularidade).
export function LancamentosPage() {
  const [contaId, setContaId] = useState("")

  const {
    data: contas,
    isLoading: carregandoContas,
    error: erroContas,
  } = useContasParaSelecao()

  if (erroContas) {
    console.error("Falha ao carregar contas para selecao", erroContas)
  }

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-[19px] font-medium text-text-primary">Lancamentos</h1>
        <p className="text-sm text-text-muted">
          Fluxo de caixa da conta: entradas e saidas reais do mes corrente.
        </p>
      </header>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="contaLancamentos">Conta</Label>
        <select
          id="contaLancamentos"
          name="contaId"
          disabled={carregandoContas}
          value={contaId}
          onChange={(event) => setContaId(event.target.value)}
          className={cn(CLASSE_SELECT)}
        >
          <option value="">{carregandoContas ? "Carregando contas..." : "Selecione uma conta"}</option>
          {contas?.map((conta) => (
            <option key={conta.id} value={conta.id}>
              {conta.nome}
            </option>
          ))}
        </select>
        {erroContas && (
          <span className="text-[12px] text-alerta">
            Nao foi possivel carregar as contas. Tente novamente.
          </span>
        )}
      </div>

      {/* Sem conta selecionada: nenhum fetch de fluxo-caixa acontece ainda -
          FluxoDeCaixaDaConta (e o useFluxoCaixa dentro dela) so monta quando
          ha uma conta escolhida. `key={contaId}` forca remount ao trocar de
          conta: alem do proprio useFluxoCaixa refazer o fetch pela query-key
          (lancamentosKeys.fluxoCaixa(contaId), ja indexada por conta), o
          remount tambem zera o formulario aberto da conta anterior, evitando
          que uma edicao/criar em andamento vaze entre contas diferentes. */}
      {contaId === "" ? (
        <p className="text-sm text-text-muted">
          Selecione uma conta para ver os lancamentos do mes.
        </p>
      ) : (
        <FluxoDeCaixaDaConta key={contaId} contaId={contaId} />
      )}
    </div>
  )
}

type FluxoDeCaixaDaContaProps = {
  contaId: string
}

// Nao exportado: existe so pra isolar o useFluxoCaixa(contaId) atras de uma
// montagem condicional (a forma correta de "pular" um fetch quando nao ha
// hook de `enabled` disponivel em useFluxoCaixa - nunca envolver o hook em um
// `if` dentro do mesmo componente, isso quebraria a regra dos hooks). Fica
// neste arquivo em vez de components/ porque a task que criou este arquivo
// restringe a escrita a um unico arquivo novo (LancamentosPage.tsx); nao e o
// padrao default de organizacao (stack.md preferiria um arquivo proprio em
// components/), so uma excecao pragmatica documentada, mesmo espirito das
// "Excecoes conhecidas" do stack.md.
function FluxoDeCaixaDaConta({ contaId }: FluxoDeCaixaDaContaProps) {
  const [formulario, setFormulario] = useState<EstadoFormulario>(null)

  const { data: lancamentos, isLoading: carregandoLancamentos, error: erroLancamentos } =
    useFluxoCaixa(contaId)

  if (erroLancamentos) {
    console.error("Falha ao carregar fluxo de caixa da conta", erroLancamentos)
  }

  const lancamentosDoMes = useMemo(
    () => filtrarLancamentosDoMes(lancamentos ?? [], anoAtual, mesAtual),
    [lancamentos],
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

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-[19px] font-medium text-text-primary">Lancamentos do mes</h2>
        <Button type="button" onClick={handleAlternarNovo}>
          {formulario?.modo === "criar" ? "Cancelar" : "Novo"}
        </Button>
      </div>

      {formulario?.modo === "criar" && (
        <div className="flex flex-col gap-3">
          {/* Segmented control Lancamento/Transferencia - mesmo padrao ja
              usado no toggle Recebivel/Emprestimo de
              FormRegistrarContaReceber.tsx (dois Button com variant
              condicional, sem componente de tabs novo). A conta origem da
              transferencia NAO e travada na conta selecionada no topo -
              FormTransferencia tem seus proprios selects de origem/destino
              (useContasParaSelecao interno), independentes desta pagina. */}
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
            <FormLancamento contaId={contaId} onSalvar={handleFecharFormulario} />
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
            contaId={contaId}
            lancamentoParaEditar={formulario.lancamento}
            onSalvar={handleFecharFormulario}
          />
        </div>
      )}

      {erroLancamentos ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar os lancamentos</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : carregandoLancamentos ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : lancamentosDoMes.length > 0 ? (
        <div className="flex flex-col gap-3">
          {lancamentosDoMes.map((lancamento) => (
            <LancamentoItem key={lancamento.id} lancamento={lancamento} onEditar={handleEditar} />
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhum lancamento neste mes.</p>
      )}
    </div>
  )
}
