import { useState, type FormEvent } from "react"
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
export function ListaContasInvestimento() {
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
      { nome: nome.trim(), saldoInicial: converterSaldoParaNumero(saldoInicial) },
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
          <h1 className="text-[19px] font-medium text-foreground">Investimentos</h1>
          <p className="text-sm text-muted-foreground">
            Cofrinho, corretoras e carteira de acoes cadastrados como contas manuais.
          </p>
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
          <AlertTitle>Nao foi possivel carregar os investimentos</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : (
        <>
          <TotalInvestidoResumo carregando={carregandoTotal} totalInvestido={total?.totalInvestido} />

          {carregandoContas ? (
            <p className="text-sm text-muted-foreground">Carregando contas...</p>
          ) : contas && contas.length > 0 ? (
            <div className="flex flex-col gap-3">
              {contas.map((conta) => (
                <ContaInvestimentoItem key={conta.id} conta={conta} />
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              Nenhuma conta de investimento cadastrada ainda. Cofrinho, XP e carteira de acoes
              aparecem aqui assim que forem cadastrados.
            </p>
          )}
        </>
      )}
    </div>
  )
}
