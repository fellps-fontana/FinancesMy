import { useContasReceber } from "@/features/contas-receber/hooks/useContasReceber"
import { ContaReceberItem } from "@/features/contas-receber/components/ContaReceberItem"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"

// Container: le o estado de servidor (React Query, via useContasReceber) e
// decide qual estado exibir (carregando/erro/vazio/lista). Renderizacao pura
// de cada item fica em ContaReceberItem - ver clean-code.md
// "Organizacao (React)".
export function ListaContasReceber() {
  const { data: contasReceber, isLoading, error } = useContasReceber()

  // Log com contexto antes de exibir a mensagem generica ao usuario - ver
  // clean-code.md "Tratamento de erro": falha nao pode ser silenciosa, mesmo
  // quando a UI so mostra um aviso generico.
  if (error) {
    console.error("Falha ao carregar contas a receber", error)
  }

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-[19px] font-medium text-text-primary">Contas a Receber</h1>
        <p className="text-sm text-text-muted">Recebiveis e emprestimos aguardando recebimento.</p>
      </header>

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar as contas a receber</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : contasReceber && contasReceber.length > 0 ? (
        <div className="flex flex-col gap-3">
          {contasReceber.map((contaReceber) => (
            <ContaReceberItem key={contaReceber.id} contaReceber={contaReceber} />
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhuma conta a receber cadastrada ainda.</p>
      )}
    </div>
  )
}
