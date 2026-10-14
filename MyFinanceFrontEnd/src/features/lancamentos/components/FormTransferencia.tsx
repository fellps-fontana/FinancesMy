import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { SelectField } from "@/shared/ui/select-field"
import { dataDeHoje } from "@/shared/lib/datas"
import { CLASSE_CAMPO } from "@/shared/ui/FormModal"
// Nao ha hook de "listar contas" combinado dentro de features/lancamentos
// (mesma situacao ja registrada em FormContaFixa.tsx). useContasParaSelecao
// ja resolve exatamente "contas MANUAL disponiveis pra origem/destino,
// excluindo cartao" (busca banco + investimento em paralelo) - reaproveitado
// aqui em vez de duplicar a chamada combinada.
import { useContasParaSelecao } from "@/shared/hooks/useContasParaSelecao"
import { useCriarTransferencia } from "@/features/lancamentos/hooks/useCriarTransferencia"
import {
  converterValorParaNumero,
  validarValor,
} from "@/features/lancamentos/lib/validarValorLancamento"

// Validacao pura do formulario de transferencia (regra-de-negocio.md itens 3
// e 12: movimentacao entre contas do proprio usuario, sem categoria/tipo -
// o backend e quem resolve a natureza da transferencia). Origem e destino
// diferentes e checagem de UX (evitar transferencia sem sentido pra mesma
// conta), nao uma regra de negocio formal documentada.
function validarFormulario(
  contaOrigemId: string,
  contaDestinoId: string,
  valor: string,
  data: string,
): string | null {
  if (contaOrigemId.length === 0) {
    return "Selecione a conta de origem."
  }

  if (contaDestinoId.length === 0) {
    return "Selecione a conta de destino."
  }

  if (contaOrigemId === contaDestinoId) {
    return "A conta de origem e a conta de destino devem ser diferentes."
  }

  const erroValor = validarValor(valor)
  if (erroValor) {
    return erroValor
  }

  if (data.trim().length === 0) {
    return "Informe a data."
  }

  return null
}

type FormTransferenciaProps = {
  onSalvar?: () => void
  onCancelar?: () => void
}

// Transferencia entre contas de mesma titularidade (regra-de-negocio.md item
// 3): SEM campo tipo/status visivel ao usuario - a duas-pernas (saida na
// origem, entrada no destino, ambas fora do calculo de gasto/receita) e
// resolvida inteiramente pelo backend a partir de contaOrigemId/
// contaDestinoId. O front so coleta os dados da movimentacao.
export function FormTransferencia({ onSalvar, onCancelar }: FormTransferenciaProps) {
  const [contaOrigemId, setContaOrigemId] = useState("")
  const [contaDestinoId, setContaDestinoId] = useState("")
  const [valor, setValor] = useState("")
  const [data, setData] = useState(dataDeHoje())
  const [descricao, setDescricao] = useState("")
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutate: criarTransferencia, isPending: isSubmitting } = useCriarTransferencia()

  // Uma unica chamada, reaproveitada nos dois selects (origem e destino) -
  // ambos escolhem dentro do mesmo universo de contas MANUAL elegiveis.
  const {
    data: contas,
    isLoading: carregandoContas,
    error: erroContas,
  } = useContasParaSelecao()

  if (erroContas) {
    console.error("Falha ao carregar contas para transferencia", erroContas)
  }

  function limparFormulario() {
    setContaOrigemId("")
    setContaDestinoId("")
    setValor("")
    setData(dataDeHoje())
    setDescricao("")
    setErroFormulario(null)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarFormulario(contaOrigemId, contaDestinoId, valor, data)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    criarTransferencia(
      {
        contaOrigemId,
        contaDestinoId,
        valor: converterValorParaNumero(valor),
        data,
        descricao: descricao.trim() || undefined,
      },
      {
        onSuccess: () => {
          limparFormulario()
          onSalvar?.()
        },
        onError: (error) => {
          console.error("Falha ao criar transferencia", error)
          setErroFormulario(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel registrar a transferencia. Tente novamente.",
          )
        },
      },
    )
  }

  const opcoesConta = (contas ?? []).map((conta) => ({ value: conta.id, label: conta.nome }))

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {erroFormulario && (
        <Alert variant="destructive">
          <AlertDescription>{erroFormulario}</AlertDescription>
        </Alert>
      )}

      <SelectField
        label="Conta de origem"
        options={opcoesConta}
        value={contaOrigemId || null}
        onChange={setContaOrigemId}
        disabled={carregandoContas || isSubmitting}
        placeholder={carregandoContas ? "Carregando contas..." : "Selecione a conta de origem"}
      />

      <SelectField
        label="Conta de destino"
        options={opcoesConta}
        value={contaDestinoId || null}
        onChange={setContaDestinoId}
        disabled={carregandoContas || isSubmitting}
        placeholder={carregandoContas ? "Carregando contas..." : "Selecione a conta de destino"}
      />

      {erroContas && (
        <span className="text-[12px] text-alerta">
          Nao foi possivel carregar as contas. Tente novamente.
        </span>
      )}

      <div className="grid grid-cols-2 gap-2.5">
        <div className="flex min-w-0 flex-col gap-1.5">
          <Label htmlFor="valorTransferencia">Valor</Label>
          <Input
            id="valorTransferencia"
            name="valor"
            type="number"
            step="0.01"
            min="0.01"
            inputMode="decimal"
            required
            value={valor}
            onChange={(event) => setValor(event.target.value)}
            className={CLASSE_CAMPO}
          />
        </div>

        <div className="flex min-w-0 flex-col gap-1.5">
          <Label htmlFor="dataTransferencia">Data</Label>
          <Input
            id="dataTransferencia"
            name="data"
            type="date"
            required
            value={data}
            onChange={(event) => setData(event.target.value)}
            className={CLASSE_CAMPO}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="descricaoTransferencia">Descricao (opcional)</Label>
        <Input
          id="descricaoTransferencia"
          name="descricao"
          placeholder="Ex: Reserva para viagem"
          value={descricao}
          onChange={(event) => setDescricao(event.target.value)}
          className={CLASSE_CAMPO}
        />
      </div>

      <div className="mt-1 flex justify-end gap-2.5">
        <Button type="button" variant="outline" size="lg" onClick={onCancelar} disabled={isSubmitting}>
          Cancelar
        </Button>
        <Button type="submit" size="lg" disabled={isSubmitting}>
          {isSubmitting ? "Salvando..." : "Salvar transferencia"}
        </Button>
      </div>
    </form>
  )
}
