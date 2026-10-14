import type { FormEvent } from "react"
import { Link } from "react-router-dom"
import { Plus } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import {
  CLASSE_BOTAO_CANCELAR,
  CLASSE_BOTAO_PRIMARIO,
  CLASSE_CAMPO,
  FormModal,
} from "@/shared/ui/FormModal"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { SelectField } from "@/shared/ui/select-field"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { ROTULO_TIPO_ATIVO } from "@/features/investimentos/lib/rotularTipoAtivo"
import { paraOpcoesDeConta } from "@/features/investimentos/lib/filtrarContasInvestimento"
import type { ContaResponse } from "@/features/contas/types"
import type { TipoAtivo } from "@/features/investimentos/types"

type ModalNovoAtivoProps = {
  nome: string
  tipo: TipoAtivo
  quantidade: string
  precoUnitario: string
  dataCompra: string
  contaId: string
  /** Contas de investimento ativas (ja filtradas pelo container). */
  contasInvestimento: ContaResponse[]
  carregandoContas: boolean
  /** quantidade x preco unitario; null enquanto os campos estiverem incompletos. */
  totalCalculado: number | null
  isSubmitting: boolean
  errorMessage: string | null
  onNomeChange: (value: string) => void
  onTipoChange: (value: TipoAtivo) => void
  onQuantidadeChange: (value: string) => void
  onPrecoUnitarioChange: (value: string) => void
  onDataCompraChange: (value: string) => void
  onContaIdChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onFechar: () => void
}

const TIPOS: ReadonlyArray<TipoAtivo> = ["RendaFixa", "RendaVariavel"]

// Apresentacao pura do modal "Novo ativo" (mockup m_investimentos). Cadastrar
// um ativo e registrar o primeiro aporte (item 8.1): quantidade x preco
// unitario, com o total mostrado como previa (o backend e a fonte da verdade).
// A conta de investimento e obrigatoria (item 8.5): sem ela o botao fica
// desabilitado e o atalho leva a /contas para criar uma.
export function ModalNovoAtivo({
  nome,
  tipo,
  quantidade,
  precoUnitario,
  dataCompra,
  contaId,
  contasInvestimento,
  carregandoContas,
  totalCalculado,
  isSubmitting,
  errorMessage,
  onNomeChange,
  onTipoChange,
  onQuantidadeChange,
  onPrecoUnitarioChange,
  onDataCompraChange,
  onContaIdChange,
  onSubmit,
  onFechar,
}: ModalNovoAtivoProps) {
  const semContasDisponiveis = !carregandoContas && contasInvestimento.length === 0

  return (
    <FormModal title="Novo ativo" onClose={onFechar}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
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
            className={CLASSE_CAMPO}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label id="tipoNovoAtivoLabel">Tipo</Label>
          <div className="flex gap-2" role="group" aria-labelledby="tipoNovoAtivoLabel">
            {TIPOS.map((opcao) => (
              <button
                key={opcao}
                type="button"
                aria-pressed={tipo === opcao}
                onClick={() => onTipoChange(opcao)}
                className={cn(
                  "flex-1 rounded-[9px] px-3 py-2.5 text-[13px] transition-colors outline-none focus-visible:ring-3 focus-visible:ring-ring/50",
                  tipo === opcao ? "bg-accent-deep text-accent-soft" : "bg-accent text-muted-foreground",
                )}
              >
                {ROTULO_TIPO_ATIVO[opcao]}
              </button>
            ))}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-2.5">
          <div className="flex min-w-0 flex-col gap-1.5">
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
              className={CLASSE_CAMPO}
            />
          </div>
          <div className="flex min-w-0 flex-col gap-1.5">
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
              className={CLASSE_CAMPO}
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-2.5">
          <div className="flex min-w-0 flex-col gap-1.5">
            <Label htmlFor="dataCompraNovoAtivo">Data da compra</Label>
            <Input
              id="dataCompraNovoAtivo"
              type="date"
              required
              value={dataCompra}
              onChange={(event) => onDataCompraChange(event.target.value)}
              className={CLASSE_CAMPO}
            />
          </div>
          <div className="flex min-w-0 flex-col justify-center rounded-[10px] border-[0.5px] border-border bg-background px-3.5 py-2.5">
            <span className="text-[11px] text-text-faint">Total investido</span>
            <span className="mt-0.5 truncate text-[13px] text-text-body">
              {totalCalculado === null ? "—" : formatarMoeda(totalCalculado)}
            </span>
          </div>
        </div>

        <div className="flex flex-col gap-1">
          <SelectField
            label="Conta de investimento"
            options={paraOpcoesDeConta(contasInvestimento)}
            value={contaId || null}
            onChange={onContaIdChange}
            disabled={carregandoContas || semContasDisponiveis}
            placeholder={
              carregandoContas
                ? "Carregando contas..."
                : semContasDisponiveis
                  ? "Nenhuma conta de investimento"
                  : "Selecione a conta"
            }
          />
          <Link
            to="/contas"
            className="mt-0.5 flex items-center gap-1.5 self-start text-[13px] text-primary outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
          >
            <Plus className="size-3.5" aria-hidden="true" />
            Nova conta de investimento
          </Link>
        </div>

        <div className="mt-1 flex justify-end gap-2.5">
          <Button
            type="button"
            variant="outline"
            onClick={onFechar}
            disabled={isSubmitting}
            className={CLASSE_BOTAO_CANCELAR}
          >
            Cancelar
          </Button>
          <Button
            type="submit"
            disabled={isSubmitting || contaId.length === 0}
            className={CLASSE_BOTAO_PRIMARIO}
          >
            {isSubmitting ? "Salvando..." : "Salvar ativo"}
          </Button>
        </div>
      </form>
    </FormModal>
  )
}
