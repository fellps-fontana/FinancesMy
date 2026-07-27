import { useState } from "react"
import { useContasFixas } from "@/features/contas-fixas/hooks/useContasFixas"
import { ContaFixaItem } from "@/features/contas-fixas/components/ContaFixaItem"
import { FormContaFixa } from "@/features/contas-fixas/FormContaFixa"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"

// Container: le o estado de servidor (React Query, via useContasFixas) e
// decide qual estado exibir (carregando/erro/vazio/lista). Renderizacao pura
// de cada item fica em ContaFixaItem - ver clean-code.md
// "Organizacao (React)". Sem filtro de `ativa`: a tela lista tanto contas
// fixas ativas quanto inativas, distinguindo pelo badge de status (regra-de-
// negocio.md item 6).
//
// Criacao: FormContaFixa (sem `contaFixaParaEditar`) e embutido aqui atras de
// um toggle - o formulario ja e o mesmo usado pra edicao (ver ContaFixaItem),
// so que em modo criar. onSalvar fecha o toggle - a lista ja reflete o item
// novo sozinha via invalidacao de cache no proprio hook useCriarContaFixa.
export function ListaContasFixas() {
  const { data: contasFixas, isLoading, error } = useContasFixas()
  const [criandoContaFixa, setCriandoContaFixa] = useState(false)

  // Log com contexto antes de exibir a mensagem generica ao usuario - ver
  // clean-code.md "Tratamento de erro": falha nao pode ser silenciosa, mesmo
  // quando a UI so mostra um aviso generico.
  if (error) {
    console.error("Falha ao carregar contas fixas", error)
  }

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-[19px] font-medium text-text-primary">Contas Fixas</h1>
          <p className="text-sm text-text-muted">Despesas recorrentes geradas automaticamente todo mes.</p>
        </div>
        <Button type="button" onClick={() => setCriandoContaFixa((aberto) => !aberto)}>
          {criandoContaFixa ? "Cancelar" : "Nova conta fixa"}
        </Button>
      </header>

      {criandoContaFixa && <FormContaFixa onSalvar={() => setCriandoContaFixa(false)} />}

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar as contas fixas</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : contasFixas && contasFixas.length > 0 ? (
        <div className="flex flex-col gap-3">
          {contasFixas.map((contaFixa) => (
            <ContaFixaItem key={contaFixa.id} contaFixa={contaFixa} />
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhuma conta fixa cadastrada ainda.</p>
      )}
    </div>
  )
}
