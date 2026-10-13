import { useState } from "react"
import { ArrowDownToLine, Pencil, Plus, Trash2 } from "lucide-react"
import { ApiError } from "@/shared/api/client"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { cn } from "@/shared/lib/utils"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { BottomSheet } from "@/shared/ui/bottom-sheet"
import { Button } from "@/shared/ui/button"
import { Chip } from "@/shared/ui/chip"
import { ConfirmDialog } from "@/shared/ui/confirm-dialog"
import { PageHeader } from "@/shared/ui/page-header"
import { resumirContasAbertas } from "@/features/conciliacao/lib/situacaoDoVencimento"
import { useContasReceber } from "@/features/contas-receber/hooks/useContasReceber"
import { useExcluirContaReceber } from "@/features/contas-receber/hooks/useExcluirContaReceber"
import {
  CabecalhoTabelaContasReceber,
  ContaReceberItem,
} from "@/features/contas-receber/components/ContaReceberItem"
import { FormRegistrarContaReceber } from "@/features/contas-receber/FormRegistrarContaReceber"
import { FormRegistrarRecebimento } from "@/features/contas-receber/FormRegistrarRecebimento"
import { FormEditarContaReceber } from "@/features/contas-receber/FormEditarContaReceber"
import type { ContaReceberResponse } from "@/features/contas-receber/types"

type FiltroTipo = "TODOS" | "EMPRESTIMO" | "RECEBIVEL"

const FILTROS: ReadonlyArray<{ valor: FiltroTipo; rotulo: string }> = [
  { valor: "TODOS", rotulo: "Todos" },
  { valor: "EMPRESTIMO", rotulo: "Emprestimos" },
  { valor: "RECEBIVEL", rotulo: "Recebiveis" },
]

const DICA_EDICAO_BLOQUEADA = "Edite o molde em Recebiveis recorrentes"

// Uma unica superficie aberta por vez: formulario (nova/receber/editar), folha
// de acoes (mobile) ou confirmacao de exclusao.
type Superficie =
  | { tipo: "nova" }
  | { tipo: "receber"; conta: ContaReceberResponse }
  | { tipo: "editar"; conta: ContaReceberResponse }
  | { tipo: "acoes"; conta: ContaReceberResponse }
  | { tipo: "excluir"; conta: ContaReceberResponse }
  | null

type ResumoContasReceber = {
  aReceber: number
  jaRecebido: number
  atrasado: number
}

// Soma dos valores que o backend ja entrega por conta (saldoPendente e
// valorTotal; item 13). "Atrasado" reaproveita a regra de vencimento de
// conciliacao: saldo pendente de conta com data prevista anterior a hoje.
function resumirContasReceber(contas: ReadonlyArray<ContaReceberResponse>): ResumoContasReceber {
  const { emAberto, atrasado } = resumirContasAbertas(
    contas.map((conta) => ({ valor: conta.saldoPendente, vencimento: conta.dataPrevista })),
  )
  const jaRecebido = contas.reduce((total, conta) => total + (conta.valorTotal - conta.saldoPendente), 0)
  return { aReceber: emAberto, jaRecebido, atrasado }
}

function mensagemDeErro(error: unknown, padrao: string): string {
  return error instanceof ApiError ? error.message : padrao
}

// Container: le o estado de servidor (React Query) e orquestra as superficies
// de UI; renderizacao de cada linha fica em ContaReceberItem. Saldos e status
// chegam prontos do backend (regra-de-negocio.md item 13).
export function ListaContasReceber() {
  const { data: contasReceber, isLoading, error } = useContasReceber()
  const { mutate: excluir, isPending: excluindo } = useExcluirContaReceber()
  const [filtro, setFiltro] = useState<FiltroTipo>("TODOS")
  const [superficie, setSuperficie] = useState<Superficie>(null)
  const [erroAcao, setErroAcao] = useState<string | null>(null)

  if (error) {
    console.error("Falha ao carregar contas a receber", error)
  }

  const todas = contasReceber ?? []
  const resumo = resumirContasReceber(todas)
  const visiveis = filtro === "TODOS" ? todas : todas.filter((conta) => conta.tipo === filtro)

  function fechar() {
    setSuperficie(null)
  }

  function abrir(proxima: Superficie) {
    setErroAcao(null)
    setSuperficie(proxima)
  }

  function handleConfirmarExclusao(conta: ContaReceberResponse) {
    setErroAcao(null)
    excluir(conta.id, {
      onSuccess: fechar,
      onError: (erroExclusao) => {
        console.error("Falha ao excluir conta a receber", erroExclusao)
        fechar()
        setErroAcao(mensagemDeErro(erroExclusao, "Nao foi possivel excluir a conta a receber. Tente novamente."))
      },
    })
  }

  const contaDaSuperficie = superficie && superficie.tipo !== "nova" ? superficie.conta : null
  const recorrente = Boolean(contaDaSuperficie?.recebivelRecorrenteId)

  return (
    <div className="mx-auto flex min-h-svh max-w-5xl flex-col gap-5 px-5 py-8 md:px-10">
      <PageHeader
        title="Contas a receber"
        action={
          <Button type="button" onClick={() => abrir({ tipo: "nova" })}>
            <Plus aria-hidden="true" />
            <span className="max-md:sr-only">Novo recebivel</span>
          </Button>
        }
      />

      <div className="grid grid-cols-2 gap-3 md:grid-cols-3 md:gap-4">
        <ResumoCard rotulo="A receber" valor={resumo.aReceber} destaque />
        <ResumoCard rotulo="Ja recebido" valor={resumo.jaRecebido} corValor="text-positivo" />
        <ResumoCard
          rotulo="Atrasado"
          valor={resumo.atrasado}
          corValor="text-negativo"
          className="max-md:col-span-2"
        />
      </div>

      <div className="flex gap-2" role="group" aria-label="Tipo de conta a receber">
        {FILTROS.map(({ valor, rotulo }) => (
          <Chip
            key={valor}
            selected={filtro === valor}
            onClick={() => setFiltro(valor)}
            className="px-3.5 py-2 md:text-[13px]"
          >
            {rotulo}
          </Chip>
        ))}
      </div>

      {erroAcao && (
        <Alert variant="destructive">
          <AlertDescription>{erroAcao}</AlertDescription>
        </Alert>
      )}

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar as contas a receber</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : visiveis.length > 0 ? (
        <div className="flex flex-col gap-2.5 md:gap-0 md:overflow-hidden md:rounded-2xl md:border-[0.5px] md:border-border md:bg-card">
          <CabecalhoTabelaContasReceber />
          {visiveis.map((conta) => (
            <ContaReceberItem
              key={conta.id}
              contaReceber={conta}
              dicaEdicaoBloqueada={conta.recebivelRecorrenteId ? DICA_EDICAO_BLOQUEADA : null}
              onReceber={(c) => abrir({ tipo: "receber", conta: c })}
              onEditar={(c) => abrir({ tipo: "editar", conta: c })}
              onExcluir={(c) => abrir({ tipo: "excluir", conta: c })}
              onAbrirAcoes={(c) => abrir({ tipo: "acoes", conta: c })}
            />
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhuma conta a receber cadastrada ainda.</p>
      )}

      <BottomSheet open={superficie?.tipo === "nova"} onClose={fechar} title="Novo recebivel">
        <FormRegistrarContaReceber onRegistrado={fechar} onCancelar={fechar} />
      </BottomSheet>

      <BottomSheet open={superficie?.tipo === "receber"} onClose={fechar} title="Registrar recebimento">
        {superficie?.tipo === "receber" && (
          <FormRegistrarRecebimento
            key={superficie.conta.id}
            contaReceberId={superficie.conta.id}
            descricao={superficie.conta.descricao}
            valorTotal={superficie.conta.valorTotal}
            saldoPendente={superficie.conta.saldoPendente}
            onRegistrado={fechar}
            onCancelar={fechar}
          />
        )}
      </BottomSheet>

      <BottomSheet open={superficie?.tipo === "editar"} onClose={fechar} title="Editar conta a receber">
        {superficie?.tipo === "editar" && (
          <FormEditarContaReceber
            key={superficie.conta.id}
            contaReceber={superficie.conta}
            onSalvo={fechar}
            onCancelar={fechar}
          />
        )}
      </BottomSheet>

      {/* Folha de acoes (mobile): toque no card. */}
      <BottomSheet
        open={superficie?.tipo === "acoes"}
        onClose={fechar}
        title={superficie?.tipo === "acoes" ? superficie.conta.descricao : "Acoes"}
      >
        {superficie?.tipo === "acoes" && (
          <div className="flex flex-col gap-1">
            {superficie.conta.status !== "RECEBIDO" && (
              <Button
                type="button"
                variant="ghost"
                size="lg"
                className="h-11 justify-start gap-3 px-3 text-text-body"
                onClick={() => abrir({ tipo: "receber", conta: superficie.conta })}
              >
                <ArrowDownToLine className="text-text-muted" aria-hidden="true" />
                Receber
              </Button>
            )}
            <Button
              type="button"
              variant="ghost"
              size="lg"
              disabled={recorrente}
              className="h-11 justify-start gap-3 px-3 text-text-body"
              onClick={() => abrir({ tipo: "editar", conta: superficie.conta })}
            >
              <Pencil className="text-text-muted" aria-hidden="true" />
              Editar
            </Button>
            {recorrente && <p className="px-3 text-xs text-text-faint">{DICA_EDICAO_BLOQUEADA}</p>}
            <Button
              type="button"
              variant="ghost"
              size="lg"
              className="h-11 justify-start gap-3 px-3 text-negativo"
              onClick={() => abrir({ tipo: "excluir", conta: superficie.conta })}
            >
              <Trash2 aria-hidden="true" />
              Excluir
            </Button>
          </div>
        )}
      </BottomSheet>

      <ConfirmDialog
        open={superficie?.tipo === "excluir"}
        title={superficie?.tipo === "excluir" ? `Excluir ${superficie.conta.descricao}?` : "Excluir"}
        description={
          recorrente
            ? "Esta e as proximas parcelas pendentes deste recebivel recorrente serao excluidas."
            : "A conta a receber sera excluida."
        }
        icon={<Trash2 className="size-[18px]" />}
        destructive
        confirmLabel={excluindo ? "Excluindo..." : "Excluir"}
        loading={excluindo}
        onCancel={fechar}
        onConfirm={() => {
          if (superficie?.tipo === "excluir") handleConfirmarExclusao(superficie.conta)
        }}
      />
    </div>
  )
}

type ResumoCardProps = {
  rotulo: string
  valor: number
  /** Valor principal: escala grande no desktop. */
  destaque?: boolean
  /** Cor semantica do valor (positivo/negativo); sem ela, texto primario. */
  corValor?: string
  className?: string
}

function ResumoCard({ rotulo, valor, destaque = false, corValor, className }: ResumoCardProps) {
  return (
    <div
      className={cn(
        "rounded-[14px] border-[0.5px] border-border bg-card p-4 md:rounded-2xl md:p-[22px]",
        className,
      )}
    >
      <span className="text-xs text-text-muted md:text-[13px]">{rotulo}</span>
      <div
        className={cn(
          "mt-1 text-[19px] font-medium md:mt-2",
          destaque ? "text-text-primary md:text-[28px]" : corValor,
        )}
      >
        {formatarMoeda(valor)}
      </div>
    </div>
  )
}
