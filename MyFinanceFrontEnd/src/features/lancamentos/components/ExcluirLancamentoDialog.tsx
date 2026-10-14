import { useState } from "react"
import { Trash2 } from "lucide-react"
import { ConfirmDialog } from "@/shared/ui/confirm-dialog"
import { mensagemDeErro } from "@/features/lancamentos/lib/mensagemDeErro"
import { useRemoverLancamento } from "@/features/lancamentos/hooks/useRemoverLancamento"
import type { LancamentoResponse } from "@/features/lancamentos/types"

type ExcluirLancamentoDialogProps = {
  lancamento: LancamentoResponse | null
  onFechar: () => void
}

// Hard delete sem desfazer, so de avulso (item 18). Se o backend recusar
// (409 nao avulso), a mensagem aparece no proprio dialogo.
export function ExcluirLancamentoDialog({ lancamento, onFechar }: ExcluirLancamentoDialogProps) {
  const [erro, setErro] = useState<string | null>(null)
  const { mutate: remover, isPending } = useRemoverLancamento()

  function fechar() {
    setErro(null)
    onFechar()
  }

  function confirmar() {
    if (!lancamento) return

    remover(
      { contaId: lancamento.contaId, lancamentoId: lancamento.id },
      {
        onSuccess: fechar,
        onError: (error) => {
          console.error("Falha ao remover lancamento", error)
          setErro(mensagemDeErro(error, "Nao foi possivel excluir o lancamento. Tente novamente."))
        },
      },
    )
  }

  return (
    <ConfirmDialog
      open={lancamento !== null}
      title={`Excluir ${lancamento?.descricao ?? "lancamento"}?`}
      description={
        <>
          <p>A exclusao e definitiva e nao pode ser desfeita.</p>
          {erro && <p className="mt-2 text-negativo">{erro}</p>}
        </>
      }
      icon={<Trash2 className="size-4" strokeWidth={1.6} />}
      confirmLabel={isPending ? "Excluindo..." : "Excluir"}
      destructive
      loading={isPending}
      onConfirm={confirmar}
      onCancel={fechar}
    />
  )
}
