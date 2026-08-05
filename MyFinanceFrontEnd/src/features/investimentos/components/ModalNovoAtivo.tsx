import type { FormEvent } from "react"
import { X } from "lucide-react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import type { TipoAtivo } from "@/features/investimentos/types"

type ModalNovoAtivoProps = {
  nome: string
  tipo: TipoAtivo
  instituicao: string
  quantidade: string
  precoUnitario: string
  dataCompra: string
  isSubmitting: boolean
  errorMessage: string | null
  onNomeChange: (value: string) => void
  onTipoChange: (value: TipoAtivo) => void
  onInstituicaoChange: (value: string) => void
  onQuantidadeChange: (value: string) => void
  onPrecoUnitarioChange: (value: string) => void
  onDataCompraChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onFechar: () => void
}

// Componente de apresentacao (burro): formulario "Novo ativo" do mockup "11
// Investimentos". Instituicao e texto livre (nao ha catalogo de instituicoes
// cadastrado no backend - decisao registrada no briefing desta tarefa),
// diferente do dropdown do mockup. Tipo e um toggle Renda fixa/Renda
// variavel (unicos dois valores de TipoAtivo, regra-de-negocio.md item 8) em
// vez de <select>, seguindo o mockup. Mesmo padrao visual de overlay do
// PagarFaturaModal (features/cartao) - fundo fixo com dialog centralizado.
// Campos "Quantidade" + "Preco unitario" no lugar de um unico "Valor
// investido": cadastrar um ativo E registrar o primeiro aporte dele
// (regra-de-negocio.md item 8.1) - valor_investido = quantidade *
// preco_unitario, calculado no back (Services/AtivoService.CriarAtivo),
// nunca digitado direto.
export function ModalNovoAtivo({
  nome,
  tipo,
  instituicao,
  quantidade,
  precoUnitario,
  dataCompra,
  isSubmitting,
  errorMessage,
  onNomeChange,
  onTipoChange,
  onInstituicaoChange,
  onQuantidadeChange,
  onPrecoUnitarioChange,
  onDataCompraChange,
  onSubmit,
  onFechar,
}: ModalNovoAtivoProps) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4"
      role="presentation"
      onClick={onFechar}
    >
      <form
        role="dialog"
        aria-modal="true"
        aria-label="Novo ativo"
        onClick={(event) => event.stopPropagation()}
        onSubmit={onSubmit}
        className="flex w-full max-w-sm flex-col gap-4 rounded-xl border border-border bg-card px-5 py-5"
      >
        <div className="flex items-center justify-between">
          <h2 className="text-[19px] font-medium text-text-primary">Novo ativo</h2>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            onClick={onFechar}
            aria-label="Fechar formulario"
          >
            <X className="size-4" aria-hidden="true" />
          </Button>
        </div>

        {errorMessage && (
          <Alert variant="destructive">
            <AlertDescription>{errorMessage}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="nomeNovoAtivo">Nome do ativo</Label>
          <Input
            id="nomeNovoAtivo"
            placeholder="Ex: Tesouro IPCA+"
            autoFocus
            required
            value={nome}
            onChange={(event) => onNomeChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label id="tipoNovoAtivoLabel">Tipo</Label>
          <div className="flex gap-2" role="group" aria-labelledby="tipoNovoAtivoLabel">
            <Button
              type="button"
              className="flex-1"
              variant={tipo === "RendaFixa" ? "default" : "outline"}
              aria-pressed={tipo === "RendaFixa"}
              onClick={() => onTipoChange("RendaFixa")}
            >
              Renda fixa
            </Button>
            <Button
              type="button"
              className="flex-1"
              variant={tipo === "RendaVariavel" ? "default" : "outline"}
              aria-pressed={tipo === "RendaVariavel"}
              onClick={() => onTipoChange("RendaVariavel")}
            >
              Renda variavel
            </Button>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="quantidadeNovoAtivo">Quantidade</Label>
            <Input
              id="quantidadeNovoAtivo"
              type="number"
              step="0.000001"
              min="0.000001"
              inputMode="decimal"
              required
              value={quantidade}
              onChange={(event) => onQuantidadeChange(event.target.value)}
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="precoUnitarioNovoAtivo">Preco unitario</Label>
            <Input
              id="precoUnitarioNovoAtivo"
              type="number"
              step="0.01"
              min="0.01"
              inputMode="decimal"
              required
              value={precoUnitario}
              onChange={(event) => onPrecoUnitarioChange(event.target.value)}
            />
          </div>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="dataCompraNovoAtivo">Data da compra</Label>
          <Input
            id="dataCompraNovoAtivo"
            type="date"
            required
            value={dataCompra}
            onChange={(event) => onDataCompraChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="instituicaoNovoAtivo">Instituicao</Label>
          <Input
            id="instituicaoNovoAtivo"
            placeholder="Ex: Nubank"
            required
            value={instituicao}
            onChange={(event) => onInstituicaoChange(event.target.value)}
          />
        </div>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="ghost" onClick={onFechar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Salvando..." : "Salvar ativo"}
          </Button>
        </div>
      </form>
    </div>
  )
}
