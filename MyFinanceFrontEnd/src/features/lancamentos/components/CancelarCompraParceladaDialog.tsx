import { useState } from "react"
import { Trash2 } from "lucide-react"
import { ConfirmDialog } from "@/shared/ui/confirm-dialog"
import { mensagemDeErro } from "@/features/lancamentos/lib/mensagemDeErro"
import type { ContagemParcelas } from "@/features/lancamentos/lib/contarParcelasPorCompra"
import { useCancelarLancamentoParcelado } from "@/features/lancamentos/hooks/useCancelarLancamentoParcelado"
import type { LancamentoResponse } from "@/features/lancamentos/types"

type CancelarCompraParceladaDialogProps = {
  // Parcela a partir da qual o usuario pediu o cancelamento (null = fechado).
  lancamento: LancamentoResponse | null
  // Contagem so para informar o usuario; o resultado real vem do backend.
  contagem: ContagemParcelas
  onFechar: () => void
}

function pluralizar(quantidade: number, singular: string, plural: string): string {
  return `${quantidade} ${quantidade === 1 ? singular : plural}`
}

// Item 17: cancelar exclui as parcelas PENDENTE da compra inteira; as PAGO
// ficam intocadas (fato historico, sem estorno).
export function CancelarCompraParceladaDialog({
  lancamento,
  contagem,
  onFechar,
}: CancelarCompraParceladaDialogProps) {
  const [erro, setErro] = useState<string | null>(null)
  const { mutate: cancelar, isPending } = useCancelarLancamentoParcelado()

  function fechar() {
    setErro(null)
    onFechar()
  }

  function confirmar() {
    if (!lancamento?.compraParceladaId) return

    cancelar(
      { contaId: lancamento.contaId, compraParceladaId: lancamento.compraParceladaId },
      {
        onSuccess: fechar,
        onError: (error) => {
          console.error("Falha ao cancelar compra parcelada", error)
          setErro(mensagemDeErro(error, "Nao foi possivel cancelar a compra. Tente novamente."))
        },
      },
    )
  }

  const textoPendentes =
    contagem.pendentes === 1
      ? "A parcela pendente sera excluida."
      : `As ${contagem.pendentes} parcelas pendentes serao excluidas.`
  const textoPagas =
    contagem.pagas === 0
      ? ""
      : contagem.pagas === 1
        ? " A parcela ja paga continua."
        : ` As ${pluralizar(contagem.pagas, "parcela", "parcelas")} ja pagas continuam.`

  return (
    <ConfirmDialog
      open={lancamento !== null}
      title={`Cancelar ${lancamento?.descricao ?? "compra"}?`}
      description={
        <>
          <p>
            {textoPendentes}
            {textoPagas}
          </p>
          {erro && <p className="mt-2 text-negativo">{erro}</p>}
        </>
      }
      icon={<Trash2 className="size-4" strokeWidth={1.6} />}
      confirmLabel={isPending ? "Cancelando..." : "Cancelar compra"}
      cancelLabel="Voltar"
      destructive
      loading={isPending}
      onConfirm={confirmar}
      onCancel={fechar}
    />
  )
}
