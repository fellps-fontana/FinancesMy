import type { FormEvent } from "react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

type FormRegistrarVendaAtivoProps = {
  ativoId: string
  ticker: string
  quantidadeDisponivelFormatada: string
  quantidade: string
  precoUnitario: string
  data: string
  observacao: string
  isSubmitting: boolean
  errorMessage: string | null
  onQuantidadeChange: (value: string) => void
  onPrecoUnitarioChange: (value: string) => void
  onDataChange: (value: string) => void
  onObservacaoChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onCancelar: () => void
}

// Componente burro: so exibe estado e dispara callbacks - mesma divisao de
// responsabilidade de FormRegistrarCompraAtivo (estado do formulario e
// chamada da mutation ficam no container - AtivoLinha, em ListaAtivos.tsx).
// A quantidade em posicao e so referencia de UX (regra-de-negocio.md item
// 8.3): quem valida de verdade contra a posicao real e o backend
// (QuantidadeVendaInvalidaException, 422).
//
// `ativoId` compoe o id de cada input: este formulario e renderizado uma vez
// por ativo (dentro de AtivoLinha), e nada impede o usuario de abrir "Vender"
// em duas linhas ao mesmo tempo (mostrarFormularioVenda e estado local por
// linha, sem mutex entre elas) - sem o sufixo, os ids ficariam duplicados no
// DOM e o `label htmlFor` perderia a associacao com o input certo.
export function FormRegistrarVendaAtivo({
  ativoId,
  ticker,
  quantidadeDisponivelFormatada,
  quantidade,
  precoUnitario,
  data,
  observacao,
  isSubmitting,
  errorMessage,
  onQuantidadeChange,
  onPrecoUnitarioChange,
  onDataChange,
  onObservacaoChange,
  onSubmit,
  onCancelar,
}: FormRegistrarVendaAtivoProps) {
  const idQuantidade = `quantidadeVendaAtivo-${ativoId}`
  const idPrecoUnitario = `precoUnitarioVendaAtivo-${ativoId}`
  const idData = `dataVendaAtivo-${ativoId}`
  const idObservacao = `observacaoVendaAtivo-${ativoId}`

  return (
    <form
      onSubmit={onSubmit}
      className="flex flex-col gap-4 rounded-xl border border-border bg-card px-4 py-4"
    >
      {errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <p className="text-[12px] text-muted-foreground">
        Voce tem {quantidadeDisponivelFormatada} unidades de {ticker} nesta posicao.
      </p>

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor={idQuantidade}>Quantidade</Label>
          <Input
            id={idQuantidade}
            name="quantidade"
            type="number"
            step="any"
            min="0"
            inputMode="decimal"
            autoFocus
            required
            value={quantidade}
            onChange={(event) => onQuantidadeChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor={idPrecoUnitario}>Preco unitario</Label>
          <Input
            id={idPrecoUnitario}
            name="precoUnitario"
            type="number"
            step="0.01"
            min="0"
            inputMode="decimal"
            required
            value={precoUnitario}
            onChange={(event) => onPrecoUnitarioChange(event.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor={idData}>Data da venda</Label>
        <Input
          id={idData}
          name="data"
          type="date"
          required
          value={data}
          onChange={(event) => onDataChange(event.target.value)}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor={idObservacao}>Observacao (opcional)</Label>
        <Input
          id={idObservacao}
          name="observacao"
          placeholder="Ex: venda parcial para reequilibrar carteira"
          value={observacao}
          onChange={(event) => onObservacaoChange(event.target.value)}
        />
      </div>

      {/* regra-de-negocio.md item 8.3: venda e so registro interno da
          carteira - reduz a quantidade (ou desativa o ativo ao zerar) e nao
          gera lancamento em nenhuma outra conta. */}
      <p className="rounded-lg border border-alerta/30 bg-alerta/10 px-2.5 py-2 text-[12px] leading-snug text-alerta">
        A venda apenas reduz sua posicao neste ativo. Ela nao lanca nem transfere valor para
        nenhuma outra conta.
      </p>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onCancelar} disabled={isSubmitting}>
          Cancelar
        </Button>
        <Button type="submit" variant="destructive" disabled={isSubmitting}>
          {isSubmitting ? "Registrando..." : "Registrar venda"}
        </Button>
      </div>
    </form>
  )
}
