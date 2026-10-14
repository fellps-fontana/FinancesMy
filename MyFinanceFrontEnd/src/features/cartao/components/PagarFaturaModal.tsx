import type { FormEvent } from "react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import {
  CLASSE_BOTAO_CANCELAR,
  CLASSE_BOTAO_PRIMARIO,
  CLASSE_CAMPO,
  FormModal,
} from "@/shared/ui/FormModal"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import type { ContaResponse } from "@/features/cartao/types"

type PagarFaturaModalProps = {
  valorPendente: number
  contaOrigemId: string
  contasBanco: ContaResponse[]
  carregandoContasBanco: boolean
  valor: string
  data: string
  isSubmitting: boolean
  errorMessage: string | null
  onContaOrigemIdChange: (value: string) => void
  onValorChange: (value: string) => void
  onDataChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onFechar: () => void
}

/**
 * Formulario de pagamento de fatura (regra de negocio item 12: o pagamento
 * pode ser ANTECIPADO - fatura ABERTA ou FUTURA - e PARCIAL, varios pagamentos
 * ate quitar o saldo pendente). O saldo pendente fica visivel no topo e o
 * campo "Valor a pagar" vem pre-preenchido com ele mas continua editavel
 * para um valor MENOR - e essa a UX que deixa claro que pagamento parcial e
 * permitido, preservando a intencao da branch de referencia.
 *
 * Puramente apresentacao: guarda so o estado transiente exibido; a mutation,
 * o estado de envio, o erro e a lista de contas BANCO (GET /api/contas?tipo=banco)
 * vem do container (FaturasSection).
 */
export function PagarFaturaModal({
  valorPendente,
  contaOrigemId,
  contasBanco,
  carregandoContasBanco,
  valor,
  data,
  isSubmitting,
  errorMessage,
  onContaOrigemIdChange,
  onValorChange,
  onDataChange,
  onSubmit,
  onFechar,
}: PagarFaturaModalProps) {
  return (
    <FormModal title="Pagar fatura" onClose={onFechar}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1 rounded-[10px] border-[0.5px] border-border bg-background px-3.5 py-2.5">
          <span className="text-[11px] text-text-faint">Saldo pendente</span>
          <span className="text-[19px] font-medium text-alerta">{formatarMoeda(valorPendente)}</span>
        </div>

        {errorMessage && (
          <Alert variant="destructive">
            <AlertDescription>{errorMessage}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="contaOrigemPagamento">Conta de origem</Label>
          {carregandoContasBanco ? (
            <span className="text-sm text-text-muted">Carregando contas...</span>
          ) : contasBanco.length === 0 ? (
            <span className="text-sm text-text-muted">Nenhuma conta banco cadastrada ainda.</span>
          ) : (
            <select
              id="contaOrigemPagamento"
              required
              value={contaOrigemId}
              onChange={(event) => onContaOrigemIdChange(event.target.value)}
              className="w-full rounded-[10px] border border-border bg-accent px-3.5 py-3 text-sm text-text-body outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
            >
              <option value="" disabled>
                Selecione a conta
              </option>
              {contasBanco.map((conta) => (
                <option key={conta.id} value={conta.id}>
                  {conta.nome}
                </option>
              ))}
            </select>
          )}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="dataPagamento">Data do pagamento</Label>
          <Input
            id="dataPagamento"
            type="date"
            required
            className={CLASSE_CAMPO}
            value={data}
            onChange={(event) => onDataChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="valorPagamento">Valor a pagar</Label>
          <Input
            id="valorPagamento"
            type="number"
            step="0.01"
            min="0.01"
            max={valorPendente}
            inputMode="decimal"
            required
            className={CLASSE_CAMPO}
            value={valor}
            onChange={(event) => onValorChange(event.target.value)}
          />
          <span className="text-[12px] text-text-faint">
            Pode ser menor que o saldo pendente - pagamento parcial e permitido, e a fatura
            continua em aberto pelo restante ate um novo pagamento.
          </span>
        </div>

        <div className="mt-1 flex justify-end gap-2.5">
          <Button type="button" variant="outline" onClick={onFechar} className={CLASSE_BOTAO_CANCELAR}>
            Cancelar
          </Button>
          <Button
            type="submit"
            disabled={isSubmitting || contasBanco.length === 0}
            className={CLASSE_BOTAO_PRIMARIO}
          >
            {isSubmitting ? "Pagando..." : "Pagar fatura"}
          </Button>
        </div>
      </form>
    </FormModal>
  )
}
