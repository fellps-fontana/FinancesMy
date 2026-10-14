import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { CLASSE_BOTAO_CANCELAR, CLASSE_BOTAO_PRIMARIO, CLASSE_CAMPO } from "@/shared/ui/FormModal"
import { useEditarContaReceber } from "@/features/contas-receber/hooks/useEditarContaReceber"
import { podeEditarValorTotal } from "@/features/contas-receber/lib/podeEditarValorTotal"
import {
  converterValorTotalParaNumero,
  validarRecebivel,
} from "@/features/contas-receber/lib/validarContaReceber"
import type { ContaReceberResponse } from "@/features/contas-receber/types"

type FormEditarContaReceberProps = {
  contaReceber: ContaReceberResponse
  onSalvo: () => void
  onCancelar: () => void
}


const AJUDA_VALOR_TOTAL_BLOQUEADO =
  "O valor total so pode ser alterado em recebivel sem nenhum recebimento registrado."

// Edicao de conta a receber (regra-de-negocio.md item 13): descricao, pessoa
// (so EMPRESTIMO) e data prevista valem em qualquer status; valor total so em
// RECEBIVEL pendente. O campo e desabilitado via podeEditarValorTotal, mas a
// regra real e do backend: um 409 do PUT e exibido como veio na mensagem.
export function FormEditarContaReceber({ contaReceber, onSalvo, onCancelar }: FormEditarContaReceberProps) {
  const emprestimo = contaReceber.tipo === "EMPRESTIMO"
  const valorEditavel = podeEditarValorTotal(contaReceber)

  const [descricao, setDescricao] = useState(contaReceber.descricao)
  const [pessoa, setPessoa] = useState(contaReceber.pessoa ?? "")
  const [dataPrevista, setDataPrevista] = useState(contaReceber.dataPrevista ?? "")
  const [valorTotal, setValorTotal] = useState(String(contaReceber.valorTotal))
  const [erro, setErro] = useState<string | null>(null)

  const { mutate: editar, isPending } = useEditarContaReceber()

  function validar(): string | null {
    if (valorEditavel) return validarRecebivel(descricao, valorTotal)
    if (descricao.trim().length === 0) return "Informe uma descricao."
    return null
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validar() ?? (emprestimo && pessoa.trim().length === 0 ? "Informe a pessoa." : null)
    if (erroValidacao) {
      setErro(erroValidacao)
      return
    }

    setErro(null)
    editar(
      {
        id: contaReceber.id,
        request: {
          descricao: descricao.trim(),
          pessoa: emprestimo ? pessoa.trim() : undefined,
          dataPrevista: dataPrevista || null,
          valorTotal: valorEditavel ? converterValorTotalParaNumero(valorTotal) : undefined,
        },
      },
      {
        onSuccess: onSalvo,
        onError: (error) => {
          console.error("Falha ao editar conta a receber", error)
          setErro(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel salvar as alteracoes. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {erro && (
        <Alert variant="destructive">
          <AlertDescription>{erro}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="descricaoEditarContaReceber">Descricao</Label>
        <Input
          id="descricaoEditarContaReceber"
          className={CLASSE_CAMPO}
          required
          value={descricao}
          onChange={(event) => setDescricao(event.target.value)}
        />
      </div>

      {emprestimo && (
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="pessoaEditarContaReceber">Pessoa</Label>
          <Input
            id="pessoaEditarContaReceber"
            className={CLASSE_CAMPO}
            required
            value={pessoa}
            onChange={(event) => setPessoa(event.target.value)}
          />
        </div>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="dataPrevistaEditarContaReceber">Data prevista</Label>
        <Input
          id="dataPrevistaEditarContaReceber"
          className={CLASSE_CAMPO}
          type="date"
          value={dataPrevista}
          onChange={(event) => setDataPrevista(event.target.value)}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="valorTotalEditarContaReceber">Valor total</Label>
        <Input
          id="valorTotalEditarContaReceber"
          className={CLASSE_CAMPO}
          type="number"
          step="0.01"
          min="0.01"
          inputMode="decimal"
          disabled={!valorEditavel}
          aria-describedby={valorEditavel ? undefined : "ajudaValorTotalEditarContaReceber"}
          value={valorTotal}
          onChange={(event) => setValorTotal(event.target.value)}
        />
        {!valorEditavel && (
          <span
            id="ajudaValorTotalEditarContaReceber"
            className="text-xs leading-snug text-text-faint"
          >
            {AJUDA_VALOR_TOTAL_BLOQUEADO}
          </span>
        )}
      </div>

      <div className="mt-1.5 flex justify-end gap-2.5">
        <Button
          type="button"
          variant="outline"
          onClick={onCancelar}
          disabled={isPending}
          className={CLASSE_BOTAO_CANCELAR}
        >
          Cancelar
        </Button>
        <Button type="submit" disabled={isPending} className={CLASSE_BOTAO_PRIMARIO}>
          {isPending ? "Salvando..." : "Salvar alteracoes"}
        </Button>
      </div>
    </form>
  )
}
