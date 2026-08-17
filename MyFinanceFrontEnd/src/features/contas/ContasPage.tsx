import { useState, type FormEvent } from "react"
import { Link } from "react-router-dom"
import { useContas } from "@/features/contas/hooks/useContas"
import { useCriarConta } from "@/features/contas/hooks/useCriarConta"
import { calcularPatrimonioTotal } from "@/features/contas/lib/calcularPatrimonioTotal"
import { aplicarMascaraMoeda, converterMascaraMoedaParaNumero } from "@/features/contas/lib/mascaraMoeda"
import { obterTipoESubtipoConta } from "@/features/contas/lib/obterTipoESubtipoConta"
import { PatrimonioTotalResumo } from "@/features/contas/components/PatrimonioTotalResumo"
import { ContaItem } from "@/features/contas/components/ContaItem"
import { FormNovaConta } from "@/features/contas/components/FormNovaConta"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { ApiError } from "@/shared/api/client"
import type { TipoContaFormulario } from "@/features/contas/types"

// Container: le o estado de servidor (React Query, via useContas/
// useCriarConta) e decide qual estado exibir. Renderizacao pura fica nos
// componentes de apresentacao chamados abaixo - ver clean-code.md
// "Organizacao (React)".
//
// Lista TODAS as contas manuais tipo Banco (corrente/poupanca/dinheiro
// fisico, regra-de-negocio.md item 10) + Investimento (saldo simples, item
// 8) - Cartao fica de fora, tem pagina propria em /cartao e e linha de
// credito, nao patrimonio. Rota /contas (TASK-129) troca o destino de
// ListaContasSimplesPage para esta pagina.
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

  const patrimonioTotal = contas ? calcularPatrimonioTotal(contas) : 0

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

  return (
    <div className="mx-auto flex min-h-svh max-w-3xl flex-col gap-6 px-4 py-8">
      <header className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-[19px] font-medium text-text-primary">Contas</h1>
          <Link
            className="text-sm text-primary underline-offset-4 hover:underline"
            to="/investimentos"
          >
            Ver investimentos (ativos)
          </Link>
        </div>
        <Button type="button" onClick={abrirModal}>
          Nova conta
        </Button>
      </header>

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
            carregando={isLoading}
            patrimonioTotal={patrimonioTotal}
            quantidadeContas={contas?.length ?? 0}
          />

          {isLoading ? (
            <p className="text-sm text-text-muted">Carregando contas...</p>
          ) : contas && contas.length > 0 ? (
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {contas.map((conta) => (
                <ContaItem key={conta.id} conta={conta} />
              ))}
            </div>
          ) : (
            <p className="text-sm text-text-muted">
              Nenhuma conta cadastrada ainda. Contas de banco e investimento aparecem aqui assim que
              forem cadastradas.
            </p>
          )}
        </>
      )}
    </div>
  )
}
