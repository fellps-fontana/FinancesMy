import { useState, type FormEvent } from "react"
import { Link } from "react-router-dom"
import { Plus } from "lucide-react"
import { useContas } from "@/features/contas/hooks/useContas"
import { useCriarConta } from "@/features/contas/hooks/useCriarConta"
import { useTotalInvestido } from "@/features/investimentos/hooks/useTotalInvestido"
import { agruparContas } from "@/features/contas/lib/agruparContas"
import { calcularPatrimonioTotal } from "@/features/contas/lib/calcularPatrimonioTotal"
import { aplicarMascaraMoeda, converterMascaraMoedaParaNumero } from "@/features/contas/lib/mascaraMoeda"
import { obterTipoESubtipoConta } from "@/features/contas/lib/obterTipoESubtipoConta"
import { PatrimonioTotalResumo } from "@/features/contas/components/PatrimonioTotalResumo"
import { ContaItem } from "@/features/contas/components/ContaItem"
import { FormNovaConta } from "@/features/contas/components/FormNovaConta"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { PageHeader } from "@/shared/ui/page-header"
import { ApiError } from "@/shared/api/client"
import type { TipoContaFormulario } from "@/features/contas/types"

// Container: le o estado de servidor (React Query, via useContas/
// useCriarConta) e decide qual estado exibir. Renderizacao pura fica nos
// componentes de apresentacao chamados abaixo - ver clean-code.md
// "Organizacao (React)".
//
// Lista as contas Banco (corrente/poupanca/dinheiro fisico, item 10),
// Investimento (item 8/8.5) e Cartao, agrupadas por tipo. O patrimonio soma os
// saldos Banco com o total investido do backend; Cartao e linha de credito e
// fica fora do patrimonio (tem pagina propria em /cartao).
export function ContasPage() {
  const { data: contas, isLoading, error } = useContas()
  const { mutate: criarConta, isPending: criandoConta } = useCriarConta()

  const [modalAberto, setModalAberto] = useState(false)
  const [nome, setNome] = useState("")
  const [tipoSelecionado, setTipoSelecionado] = useState<TipoContaFormulario>("CONTA_CORRENTE")
  const [saldoInicialMascarado, setSaldoInicialMascarado] = useState(() => aplicarMascaraMoeda(""))
  const [icone, setIcone] = useState<string | null>(null)
  const [cor, setCor] = useState<string | null>(null)
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  if (error) {
    console.error("Falha ao carregar contas", error)
  }

  const {
    data: totalInvestido,
    isLoading: carregandoTotalInvestido,
    error: erroTotalInvestido,
  } = useTotalInvestido()
  if (erroTotalInvestido) {
    console.error("Falha ao carregar total investido", erroTotalInvestido)
  }

  // Sem o total investido o patrimonio ficaria subestimado: so e calculado
  // quando as contas e o total estao disponiveis.
  const patrimonioTotal =
    contas && totalInvestido ? calcularPatrimonioTotal(contas, totalInvestido.totalInvestido) : 0

  function abrirModal() {
    setNome("")
    setTipoSelecionado("CONTA_CORRENTE")
    setSaldoInicialMascarado(aplicarMascaraMoeda(""))
    setIcone(null)
    setCor(null)
    setErroFormulario(null)
    setModalAberto(true)
  }

  function fecharModal() {
    setModalAberto(false)
    setErroFormulario(null)
  }

  function handleSubmitNovaConta(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (nome.trim().length === 0) {
      setErroFormulario("Informe um nome para a conta.")
      return
    }

    const { tipo, subtipo } = obterTipoESubtipoConta(tipoSelecionado)

    criarConta(
      {
        nome: nome.trim(),
        tipo,
        subtipo,
        icone: icone ?? undefined,
        cor: cor ?? undefined,
        saldoManual: converterMascaraMoedaParaNumero(saldoInicialMascarado),
      },
      {
        onSuccess: fecharModal,
        onError: (erroCriacao) => {
          console.error("Falha ao criar conta", erroCriacao)
          setErroFormulario(
            erroCriacao instanceof ApiError
              ? erroCriacao.message
              : "Nao foi possivel criar a conta. Tente novamente.",
          )
        },
      },
    )
  }

  const grupos = contas ? agruparContas(contas) : []

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <PageHeader
        title="Contas"
        action={
          <>
            <Button
              type="button"
              onClick={abrirModal}
              className="hidden h-auto gap-1.5 rounded-[9px] px-4 py-2.5 text-[13px] md:inline-flex"
            >
              <Plus className="size-3.5" strokeWidth={2} aria-hidden="true" />
              Nova conta
            </Button>
            <Button
              type="button"
              onClick={abrirModal}
              aria-label="Nova conta"
              className="size-8 rounded-[9px] p-0 md:hidden"
            >
              <Plus className="size-4" strokeWidth={2} aria-hidden="true" />
            </Button>
          </>
        }
      />

      {modalAberto && (
        <FormNovaConta
          nome={nome}
          tipoSelecionado={tipoSelecionado}
          saldoInicialMascarado={saldoInicialMascarado}
          iconeSelecionado={icone}
          corSelecionada={cor}
          isSubmitting={criandoConta}
          errorMessage={erroFormulario}
          onNomeChange={setNome}
          onTipoChange={setTipoSelecionado}
          onSaldoInicialChange={setSaldoInicialMascarado}
          onIconeChange={setIcone}
          onCorChange={setCor}
          onSubmit={handleSubmitNovaConta}
          onFechar={fecharModal}
        />
      )}

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Não foi possível carregar as contas</AlertTitle>
          <AlertDescription>Verifique sua conexão e tente novamente.</AlertDescription>
        </Alert>
      ) : (
        <>
          <PatrimonioTotalResumo
            carregando={isLoading || carregandoTotalInvestido}
            erro={Boolean(erroTotalInvestido)}
            patrimonioTotal={patrimonioTotal}
            quantidadeContas={contas?.length ?? 0}
          />

          {isLoading ? (
            <p className="text-sm text-text-muted">Carregando contas...</p>
          ) : grupos.length > 0 ? (
            grupos.map((grupo) => (
              <section key={grupo.tipo} className="flex flex-col gap-2.5">
                <h2 className="text-[13px] text-text-muted">{grupo.titulo}</h2>
                <div className="grid grid-cols-1 content-start gap-3.5 md:grid-cols-2">
                  {grupo.contas.map((conta) => (
                    <ContaItem key={conta.id} conta={conta} />
                  ))}
                </div>
              </section>
            ))
          ) : (
            <p className="text-sm text-text-muted">
              Nenhuma conta cadastrada ainda. Contas de banco e investimento aparecem aqui assim que
              forem cadastradas.
            </p>
          )}

          <Link className="text-sm text-primary underline-offset-4 hover:underline" to="/investimentos">
            Ver investimentos (ativos)
          </Link>
        </>
      )}
    </div>
  )
}
