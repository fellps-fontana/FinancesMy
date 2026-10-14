import type { FormEvent } from "react"
import { CategoriaCampoSelect } from "@/features/categorias/components/CategoriaCampoSelect"
import {
  CLASSE_BOTAO_CANCELAR,
  CLASSE_BOTAO_PRIMARIO,
  CLASSE_CAMPO,
  FormModal,
} from "@/shared/ui/FormModal"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

type LancarCompraFormProps = {
  descricao: string
  valor: string
  data: string
  categoriaId: string
  numeroParcelas: string
  isSubmitting: boolean
  errorMessage: string | null
  onDescricaoChange: (value: string) => void
  onValorChange: (value: string) => void
  onDataChange: (value: string) => void
  onCategoriaIdChange: (value: string) => void
  onNumeroParcelasChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onFechar: () => void
}

/**
 * Formulario de lancamento de compra no cartao (regra de negocio item 12:
 * regime de COMPETENCIA - a compra muda o saldo calculado do cartao mas nao
 * entra no fluxo de caixa/lancamento geral). Puramente apresentacao: guarda
 * so o estado transiente dos campos; a mutation, o estado de envio e o erro
 * vem do container (ContaCartaoPage) - mesmo padrao de PagarFaturaModal.
 *
 * Categoria (regra de negocio item 7) usa o CategoriaCampoSelect do modulo
 * categorias, tipo Despesa - compra de cartao e sempre gasto (item 12).
 * Selecionar categoria e opcional: nao selecionar mantem `categoriaId: null`
 * no request (container decide isso, este componente so repassa o valor
 * transiente do select).
 *
 * Numero de parcelas (regra de negocio item 12, subsecao "Parcelamento") e
 * opcional: em branco ou 1 lanca compra a vista (mesmo fluxo de sempre);
 * >= 2 faz o container chamar o endpoint de compras-parceladas. O calculo do
 * valor de cada parcela e responsabilidade do backend - este form so coleta
 * a quantidade.
 */
// Resumo textual (sem calculo): o valor de cada parcela e definido pelo backend.
function montarResumo(numeroParcelas: string): string {
  const n = Number(numeroParcelas.trim())
  if (numeroParcelas.trim().length > 0 && Number.isInteger(n) && n >= 2) {
    return `Compra parcelada em ${n}x. O valor de cada parcela e calculado ao salvar e cada uma entra na fatura do seu vencimento.`
  }
  return "Compra a vista: entra inteira na fatura do periodo da data informada."
}

export function LancarCompraForm({
  descricao,
  valor,
  data,
  categoriaId,
  numeroParcelas,
  isSubmitting,
  errorMessage,
  onDescricaoChange,
  onValorChange,
  onDataChange,
  onCategoriaIdChange,
  onNumeroParcelasChange,
  onSubmit,
  onFechar,
}: LancarCompraFormProps) {
  return (
    <FormModal title="Nova compra" onClose={onFechar}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        {errorMessage && (
          <Alert variant="destructive">
            <AlertDescription>{errorMessage}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="descricaoCompra">Descricao</Label>
          <Input
            id="descricaoCompra"
            placeholder="Ex: Restaurante"
            required
            autoFocus
            className={CLASSE_CAMPO}
            value={descricao}
            onChange={(event) => onDescricaoChange(event.target.value)}
          />
        </div>

        <div className="flex gap-2.5">
          <div className="flex min-w-0 flex-1 flex-col gap-1.5">
            <Label htmlFor="valorCompra">Valor total</Label>
            <Input
              id="valorCompra"
              type="number"
              step="0.01"
              min="0.01"
              inputMode="decimal"
              placeholder="0,00"
              required
              className={CLASSE_CAMPO}
              value={valor}
              onChange={(event) => onValorChange(event.target.value)}
            />
          </div>
          <div className="flex min-w-0 flex-1 flex-col gap-1.5">
            <Label htmlFor="dataCompra">Data</Label>
            <Input
              id="dataCompra"
              type="date"
              required
              className={CLASSE_CAMPO}
              value={data}
              onChange={(event) => onDataChange(event.target.value)}
            />
          </div>
        </div>

        <div className="flex gap-2.5">
          <div className="flex min-w-0 flex-1 flex-col gap-1.5">
            <CategoriaCampoSelect tipo="Despesa" value={categoriaId || undefined} onChange={onCategoriaIdChange} />
          </div>
          <div className="flex min-w-0 flex-1 flex-col gap-1.5">
            <Label htmlFor="numeroParcelasCompra">
              Parcelas <span className="text-text-faint">· opcional</span>
            </Label>
            <Input
              id="numeroParcelasCompra"
              type="number"
              step="1"
              min="2"
              inputMode="numeric"
              placeholder="A vista"
              className={CLASSE_CAMPO}
              value={numeroParcelas}
              onChange={(event) => onNumeroParcelasChange(event.target.value)}
            />
          </div>
        </div>

        <div className="rounded-[10px] border-[0.5px] border-border bg-background px-3.5 py-2.5">
          <div className="text-[11px] text-text-faint">Resumo</div>
          <div className="mt-0.5 text-[13px] text-text-body">{montarResumo(numeroParcelas)}</div>
        </div>

        <div className="mt-1 flex justify-end gap-2.5">
          <Button type="button" variant="outline" onClick={onFechar} className={CLASSE_BOTAO_CANCELAR}>
            Cancelar
          </Button>
          <Button type="submit" disabled={isSubmitting} className={CLASSE_BOTAO_PRIMARIO}>
            {isSubmitting ? "Salvando..." : "Salvar compra"}
          </Button>
        </div>
      </form>
    </FormModal>
  )
}
