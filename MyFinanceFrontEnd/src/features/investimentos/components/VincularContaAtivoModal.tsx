import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { CLASSE_BOTAO_CANCELAR, CLASSE_BOTAO_PRIMARIO, FormModal } from "@/shared/ui/FormModal"
import { SelectField } from "@/shared/ui/select-field"
import { useVincularContaAtivo } from "@/features/investimentos/hooks/useVincularContaAtivo"
import { paraOpcoesDeConta } from "@/features/investimentos/lib/filtrarContasInvestimento"
import type { ContaResponse } from "@/features/contas/types"
import type { AtivoResponse } from "@/features/investimentos/types"

type VincularContaAtivoModalProps = {
  ativo: AtivoResponse
  /** Somente contas tipo Investimento ativas (filtradas pelo container). */
  contasInvestimento: ContaResponse[]
  onFechar: () => void
}

// Define ou troca a conta de investimento do ativo (item 8.5). O ativo nunca
// volta a ficar sem conta: o botao so habilita com uma conta escolhida e o
// request sempre leva um contaId nao vazio. 400/404 do backend aparecem como
// mensagem no modal.
export function VincularContaAtivoModal({ ativo, contasInvestimento, onFechar }: VincularContaAtivoModalProps) {
  const { mutate: vincular, isPending } = useVincularContaAtivo()
  const [contaId, setContaId] = useState(ativo.contaId ?? "")
  const [erro, setErro] = useState<string | null>(null)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (contaId.length === 0) return

    vincular(
      { ativoId: ativo.id, contaId },
      {
        onSuccess: onFechar,
        onError: (error) => {
          console.error("Falha ao vincular conta ao ativo", error)
          setErro(
            error instanceof ApiError ? error.message : "Nao foi possivel vincular a conta. Tente novamente.",
          )
        },
      },
    )
  }

  const titulo = ativo.semConta ? "Vincular conta" : "Trocar conta"

  return (
    <FormModal title={`${titulo} - ${ativo.nome}`} onClose={onFechar}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {erro && (
          <Alert variant="destructive">
            <AlertDescription>{erro}</AlertDescription>
          </Alert>
        )}

        <SelectField
          label="Conta de investimento"
          options={paraOpcoesDeConta(contasInvestimento)}
          value={contaId || null}
          onChange={setContaId}
          disabled={contasInvestimento.length === 0}
          placeholder={contasInvestimento.length === 0 ? "Nenhuma conta de investimento" : "Selecione a conta"}
        />

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
          <Button type="submit" disabled={isPending || contaId.length === 0} className={CLASSE_BOTAO_PRIMARIO}>
            {isPending ? "Salvando..." : "Salvar conta"}
          </Button>
        </div>
      </form>
    </FormModal>
  )
}
