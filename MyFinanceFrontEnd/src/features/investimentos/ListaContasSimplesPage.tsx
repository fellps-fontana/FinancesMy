import { useState, type FormEvent } from "react"
import { Link } from "react-router-dom"
import { useContasInvestimento } from "@/features/investimentos/hooks/useContasInvestimento"
import { useTotalInvestido } from "@/features/investimentos/hooks/useTotalInvestido"
import { useCriarContaInvestimento } from "@/features/investimentos/hooks/useCriarContaInvestimento"
import { TotalInvestidoResumo } from "@/features/investimentos/components/TotalInvestidoResumo"
import { ContaInvestimentoItem } from "@/features/investimentos/components/ContaInvestimentoItem"
import { FormCriarContaInvestimento } from "@/features/investimentos/components/FormCriarContaInvestimento"
import { validarNovaConta } from "@/features/investimentos/lib/validarNovaConta"
import { converterSaldoParaNumero } from "@/features/investimentos/lib/validarSaldo"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { ApiError } from "@/shared/api/client"

// Container: le o estado de servidor (React Query) e decide qual estado
// exibir. Renderizacao pura fica nos componentes de apresentacao chamados
// abaixo - ver clean-code.md "Organizacao (React)".
//
// Tela propria para conta de investimento SIMPLES (cofrinho Mercado Pago, XP
// sem detalhe de ativo) - regra-de-negocio.md item 8: "CONTA MANUAL propria
// (tipo INVESTIMENTO), saldo atualizado pelo usuario via saldo_manual, igual
// qualquer conta manual". Sem relacao com o modulo de Ativo (posicao
// individual, ver ListaAtivosPage.tsx) - sao dois modulos independentes que
// so compartilham a raiz "investimentos" por organizacao de pastas.
export function ListaContasSimplesPage() {
  const { data: contas, isLoading: carregandoContas, error: erroContas } = useContasInvestimento()
  const { data: total, isLoading: carregandoTotal, error: erroTotal } = useTotalInvestido()
  const { mutate: criarConta, isPending: criandoConta } = useCriarContaInvestimento()

  const [mostrarFormulario, setMostrarFormulario] = useState(false)
  const [nome, setNome] = useState("")
  const [saldoInicial, setSaldoInicial] = useState("")
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const erro = erroContas ?? erroTotal

  // Log com contexto (qual query falhou) antes de exibir a mensagem generica
  // ao usuario - ver clean-code.md "Tratamento de erro": falha nao pode ser
  // silenciosa, mesmo quando a UI so mostra um aviso generico.
  if (erroContas) {
    console.error("Falha ao carregar contas de investimento", erroContas)
  }
  if (erroTotal) {
    console.error("Falha ao carregar total investido", erroTotal)
  }

  function fecharFormulario() {
    setMostrarFormulario(false)
    setNome("")
    setSaldoInicial("")
    setErroFormulario(null)
  }

  function handleSubmitNovaConta(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarNovaConta(nome, saldoInicial)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    criarConta(
      { nome: nome.trim(), tipo: "INVESTIMENTO", saldoManual: converterSaldoParaNumero(saldoInicial) },
      {
        onSuccess: fecharFormulario,
        onError: (error) => {
          console.error("Falha ao criar conta de investimento", error)
          setErroFormulario(
            error instanceof ApiError ? error.message : "Nao foi possivel criar a conta. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-[19px] font-medium text-text-primary">Contas simples</h1>
          <p className="text-sm text-text-muted">
            Cofrinho, corretoras e outras contas manuais sem detalhe de ativo.
          </p>
          <Link
            className="text-sm text-primary underline-offset-4 hover:underline"
            to="/investimentos"
          >
            Ver investimentos (ativos)
          </Link>
        </div>
        {!mostrarFormulario && (
          <Button type="button" onClick={() => setMostrarFormulario(true)}>
            Nova conta
          </Button>
        )}
      </header>

      {mostrarFormulario && (
        <FormCriarContaInvestimento
          nome={nome}
          saldoInicial={saldoInicial}
          isSubmitting={criandoConta}
          errorMessage={erroFormulario}
          onNomeChange={setNome}
          onSaldoInicialChange={setSaldoInicial}
          onSubmit={handleSubmitNovaConta}
          onCancelar={fecharFormulario}
        />
      )}

      {erro ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar as contas</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : (
        <>
          <TotalInvestidoResumo carregando={carregandoTotal} totalInvestido={total?.totalInvestido} />

          {carregandoContas ? (
            <p className="text-sm text-text-muted">Carregando contas...</p>
          ) : contas && contas.length > 0 ? (
            <div className="flex flex-col gap-3">
              {contas.map((conta) => (
                <ContaInvestimentoItem key={conta.id} conta={conta} />
              ))}
            </div>
          ) : (
            <p className="text-sm text-text-muted">
              Nenhuma conta de investimento cadastrada ainda. Cofrinho, XP e outras contas manuais
              aparecem aqui assim que forem cadastradas.
            </p>
          )}
        </>
      )}
    </div>
  )
}
