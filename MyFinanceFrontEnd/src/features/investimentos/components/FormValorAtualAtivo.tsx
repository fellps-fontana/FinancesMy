import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
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
import { useAtualizarValorAtualAtivo } from "@/features/investimentos/hooks/useAtualizarValorAtualAtivo"
import { validarValorAtual } from "@/features/investimentos/lib/validarValorAtual"
import { converterValorParaNumero } from "@/features/investimentos/lib/validarValorPositivo"
import type { AtivoResponse } from "@/features/investimentos/types"

type FormValorAtualAtivoProps = {
  ativo: AtivoResponse
  onFechar: () => void
}

// Modal "Editar valor atual" (item 8.1: valor atual e 100% manual).
export function FormValorAtualAtivo({ ativo, onFechar }: FormValorAtualAtivoProps) {
  const { mutate: atualizarValor, isPending } = useAtualizarValorAtualAtivo()
  const [novoValor, setNovoValor] = useState(String(ativo.valorAtual))
  const [erro, setErro] = useState<string | null>(null)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarValorAtual(novoValor)
    if (erroValidacao) {
      setErro(erroValidacao)
      return
    }

    atualizarValor(
      { id: ativo.id, request: { novoValorAtual: converterValorParaNumero(novoValor) } },
      {
        onSuccess: onFechar,
        onError: (error) => {
          console.error("Falha ao atualizar valor atual do ativo", error)
          setErro(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel salvar o valor. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <FormModal title={`Valor atual - ${ativo.nome}`} onClose={onFechar}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {erro && (
          <Alert variant="destructive">
            <AlertDescription>{erro}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="valorAtualAtivo">Valor atual</Label>
          <Input
            id="valorAtualAtivo"
            type="number"
            step="0.01"
            min="0.01"
            inputMode="decimal"
            autoFocus
            required
            value={novoValor}
            onChange={(event) => setNovoValor(event.target.value)}
            className={CLASSE_CAMPO}
          />
        </div>

        <div className="mt-1 flex justify-end gap-2.5">
          <Button
            type="button"
            variant="outline"
            onClick={onFechar}
            disabled={isPending}
            className={CLASSE_BOTAO_CANCELAR}
          >
            Cancelar
          </Button>
          <Button type="submit" disabled={isPending} className={CLASSE_BOTAO_PRIMARIO}>
            {isPending ? "Salvando..." : "Salvar valor"}
          </Button>
        </div>
      </form>
    </FormModal>
  )
}
