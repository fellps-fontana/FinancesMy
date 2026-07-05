import { useContasInvestimento } from "@/features/investimentos/hooks/useContasInvestimento"
import { useTotalInvestido } from "@/features/investimentos/hooks/useTotalInvestido"
import { TotalInvestidoResumo } from "@/features/investimentos/components/TotalInvestidoResumo"
import { ContaInvestimentoCard } from "@/features/investimentos/components/ContaInvestimentoCard"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"

// Container: le o estado de servidor (React Query) e decide qual estado
// exibir. Renderizacao pura fica nos componentes de apresentacao chamados
// abaixo - ver clean-code.md "Organizacao (React)".
export function ListaContasInvestimento() {
  const { data: contas, isLoading: carregandoContas, error: erroContas } = useContasInvestimento()
  const { data: total, isLoading: carregandoTotal, error: erroTotal } = useTotalInvestido()

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

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-[19px] font-medium text-foreground">Investimentos</h1>
        <p className="text-sm text-muted-foreground">
          Cofrinho, corretoras e carteira de acoes cadastrados como contas manuais.
        </p>
      </header>

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
                <ContaInvestimentoCard key={conta.id} conta={conta} />
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
