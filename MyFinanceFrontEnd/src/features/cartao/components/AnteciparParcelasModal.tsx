import { Info } from "lucide-react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import {
  CLASSE_BOTAO_CANCELAR,
  CLASSE_BOTAO_PRIMARIO,
  FormModal,
} from "@/shared/ui/FormModal"
import { Button } from "@/shared/ui/button"
import { Stepper } from "@/shared/ui/stepper"
import { montarPreviaAntecipacao } from "@/features/cartao/lib/montarPreviaAntecipacao"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import type { ParcelaElegivelResponse } from "@/features/cartao/types"

type AnteciparParcelasModalProps = {
  open: boolean
  descricao: string
  /** null enquanto carrega. */
  elegiveis: ParcelaElegivelResponse[] | null
  erroCarregamento: boolean
  quantidade: number
  isSubmitting: boolean
  errorMessage: string | null
  onQuantidadeChange: (value: number) => void
  onConfirmar: () => void
  onFechar: () => void
}

// Apresentacao pura: o maximo da quantidade e o numero de elegiveis devolvido
// pelo backend (a elegibilidade nao e decidida aqui). A previa so seleciona e
// soma as parcelas das ultimas para tras (montarPreviaAntecipacao).
export function AnteciparParcelasModal({
  open,
  descricao,
  elegiveis,
  erroCarregamento,
  quantidade,
  isSubmitting,
  errorMessage,
  onQuantidadeChange,
  onConfirmar,
  onFechar,
}: AnteciparParcelasModalProps) {
  const total = elegiveis?.length ?? 0
  const previa = montarPreviaAntecipacao(elegiveis ?? [], quantidade)

  return (
    <FormModal open={open} onClose={onFechar} title={`Antecipar parcelas - ${descricao}`}>
      {elegiveis === null && !erroCarregamento && (
        <p className="text-sm text-text-muted">Carregando parcelas...</p>
      )}
      {erroCarregamento && (
        <Alert variant="destructive">
          <AlertDescription>Nao foi possivel carregar as parcelas elegiveis.</AlertDescription>
        </Alert>
      )}
      {elegiveis !== null && total === 0 && (
        <p className="text-sm text-text-muted">Nao ha parcelas elegiveis para antecipar.</p>
      )}

      {elegiveis !== null && total > 0 && (
        <>
          <div className="flex items-center justify-between gap-3">
            <div>
              <div className="text-sm text-text-body">Quantidade</div>
              <div className="mt-0.5 text-[12px] text-text-faint">
                {total} {total === 1 ? "parcela disponivel" : "parcelas disponiveis"}
              </div>
            </div>
            <Stepper
              ariaLabel="Quantidade de parcelas a antecipar"
              value={quantidade}
              min={1}
              max={total}
              onChange={onQuantidadeChange}
              disabled={isSubmitting}
              className="[&_span]:text-[15px]"
            />
          </div>

          <div className="flex flex-col gap-2">
            <span className="text-[13px] text-text-muted">Vao para a fatura atual</span>
            <ul className="flex flex-col gap-px overflow-hidden rounded-[10px] border-[0.5px] border-border bg-border">
              {previa.parcelas.map((parcela) => (
                <li
                  key={parcela.lancamentoId}
                  className="flex justify-between bg-background px-3.5 py-2.5"
                >
                  <span className="text-[13px] text-text-body">
                    Parcela {parcela.parcelaNumero}/{parcela.totalParcelas}
                  </span>
                  <span className="text-[13px] text-text-primary">{formatarMoeda(parcela.valor)}</span>
                </li>
              ))}
              <li className="flex justify-between bg-background px-3.5 py-2.5">
                <span className="text-[13px] text-text-muted">Total antecipado</span>
                <span className="text-[13px] font-medium text-text-primary">
                  {formatarMoeda(previa.valorTotal)}
                </span>
              </li>
            </ul>
          </div>
        </>
      )}

      <div className="flex items-start gap-2.5 rounded-[10px] border-[0.5px] border-border px-3.5 py-3">
        <Info className="mt-0.5 size-4 shrink-0 text-alerta" aria-hidden="true" />
        <span className="text-[12px] leading-normal text-text-muted">
          Antecipar nao paga a fatura, so traz as parcelas para ela. Se o banco der desconto,
          lance um estorno na fatura.
        </span>
      </div>

      {errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <div className="mt-1 flex justify-end gap-2.5">
        <Button type="button" variant="outline" onClick={onFechar} className={CLASSE_BOTAO_CANCELAR}>
          Cancelar
        </Button>
        <Button
          type="button"
          onClick={onConfirmar}
          disabled={isSubmitting || total === 0}
          className={CLASSE_BOTAO_PRIMARIO}
        >
          {isSubmitting
            ? "Antecipando..."
            : `Antecipar ${previa.quantidade} ${previa.quantidade === 1 ? "parcela" : "parcelas"}`}
        </Button>
      </div>
    </FormModal>
  )
}
