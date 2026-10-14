import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { CLASSE_BOTAO_CANCELAR, CLASSE_BOTAO_PRIMARIO, CLASSE_CAMPO } from "@/shared/ui/FormModal"
import { SegmentedTabs } from "@/shared/ui/segmented-tabs"
import { SelectField } from "@/shared/ui/select-field"
import { dataDeHoje } from "@/features/cartao/lib/formatarData"
import { useCriarEmprestimo } from "@/features/contas-receber/hooks/useCriarEmprestimo"
import { useCriarRecebivel } from "@/features/contas-receber/hooks/useCriarRecebivel"
import { useContasParaSelecao } from "@/features/contas-receber/hooks/useContasParaSelecao"
import {
  converterValorTotalParaNumero,
  validarEmprestimo,
  validarRecebivel,
} from "@/features/contas-receber/lib/validarContaReceber"

type TipoContaReceber = "RECEBIVEL" | "EMPRESTIMO"

const OPCOES_TIPO: ReadonlyArray<{ value: TipoContaReceber; label: string }> = [
  { value: "RECEBIVEL", label: "Recebivel" },
  { value: "EMPRESTIMO", label: "Emprestimo" },
]


type FormRegistrarContaReceberProps = {
  onRegistrado: () => void
  onCancelar: () => void
}

// Estado deste formulario (inputs + qual toggle esta ativo) vive dentro do
// proprio componente: e um formulario auto-contido, sem tela intermediaria
// de listagem/estado a coordenar - diferente do par
// FormCriarContaInvestimento/ListaContasInvestimento, que separa em
// container + apresentacao porque o container tambem gerencia a lista em si.
// Aqui nao ha lista a gerenciar, entao a divisao adicional so adicionaria
// arquivo sem separar responsabilidade real (clean-code.md: "nao introduza
// padrao de projeto sem necessidade real"). Estado de servidor continua
// isolado do estado de UI via React Query - os hooks de mutation prontos
// (useCriarRecebivel/useCriarEmprestimo) e useContasParaSelecao (compartilhado
// com FormRegistrarRecebimento) para a lista de contas de origem; nenhum
// fetch cru dentro de handlers.
export function FormRegistrarContaReceber({ onRegistrado, onCancelar }: FormRegistrarContaReceberProps) {
  const [tipo, setTipo] = useState<TipoContaReceber>("RECEBIVEL")
  const [descricao, setDescricao] = useState("")
  const [valorTotal, setValorTotal] = useState("")
  const [dataRegistro, setDataRegistro] = useState(dataDeHoje())
  const [dataPrevista, setDataPrevista] = useState("")
  const [pessoa, setPessoa] = useState("")
  const [contaOrigemId, setContaOrigemId] = useState("")
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutate: criarRecebivel, isPending: criandoRecebivel } = useCriarRecebivel()
  const { mutate: criarEmprestimo, isPending: criandoEmprestimo } = useCriarEmprestimo()

  const {
    data: contasOrigem,
    isLoading: carregandoContasOrigem,
    error: erroContasOrigem,
  } = useContasParaSelecao({ enabled: tipo === "EMPRESTIMO" })

  if (erroContasOrigem) {
    console.error("Falha ao carregar contas de origem para emprestimo", erroContasOrigem)
  }

  const isSubmitting = criandoRecebivel || criandoEmprestimo

  function limparFormulario() {
    setDescricao("")
    setValorTotal("")
    setDataRegistro(dataDeHoje())
    setDataPrevista("")
    setPessoa("")
    setContaOrigemId("")
    setErroFormulario(null)
  }

  function concluirRegistro() {
    limparFormulario()
    onRegistrado()
  }

  function handleTrocarTipo(novoTipo: TipoContaReceber) {
    setTipo(novoTipo)
    setErroFormulario(null)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (tipo === "RECEBIVEL") {
      const erroValidacao = validarRecebivel(descricao, valorTotal)
      if (erroValidacao) {
        setErroFormulario(erroValidacao)
        return
      }

      criarRecebivel(
        {
          descricao: descricao.trim(),
          valorTotal: converterValorTotalParaNumero(valorTotal),
          dataRegistro,
          dataPrevista: dataPrevista || undefined,
        },
        {
          onSuccess: concluirRegistro,
          onError: (error) => {
            console.error("Falha ao registrar recebivel", error)
            setErroFormulario(
              error instanceof ApiError
                ? error.message
                : "Nao foi possivel registrar o recebivel. Tente novamente.",
            )
          },
        },
      )
      return
    }

    const erroValidacao = validarEmprestimo(descricao, pessoa, valorTotal, contaOrigemId)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    criarEmprestimo(
      {
        descricao: descricao.trim(),
        pessoa: pessoa.trim(),
        valorTotal: converterValorTotalParaNumero(valorTotal),
        contaOrigemId,
        dataRegistro,
        dataPrevista: dataPrevista || undefined,
      },
      {
        onSuccess: concluirRegistro,
        onError: (error) => {
          console.error("Falha ao registrar emprestimo", error)
          setErroFormulario(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel registrar o emprestimo. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="flex flex-col gap-4"
    >
      {erroFormulario && (
        <Alert variant="destructive">
          <AlertDescription>{erroFormulario}</AlertDescription>
        </Alert>
      )}

      <SegmentedTabs
        options={OPCOES_TIPO}
        value={tipo}
        onChange={handleTrocarTipo}
        ariaLabel="Tipo de conta a receber"
      />

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="descricaoContaReceber">Descricao</Label>
        <Input
          id="descricaoContaReceber"
          className={CLASSE_CAMPO}
          name="descricao"
          placeholder={tipo === "RECEBIVEL" ? "Ex: Reembolso da viagem" : "Ex: Emprestimo para o carro"}
          autoFocus
          required
          value={descricao}
          onChange={(event) => setDescricao(event.target.value)}
        />
      </div>

      {tipo === "EMPRESTIMO" && (
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="pessoaContaReceber">Pessoa</Label>
          <Input
            id="pessoaContaReceber"
          className={CLASSE_CAMPO}
            name="pessoa"
            placeholder="Nome de quem recebeu o emprestimo"
            required
            value={pessoa}
            onChange={(event) => setPessoa(event.target.value)}
          />
        </div>
      )}

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="valorTotalContaReceber">Valor total</Label>
          <Input
            id="valorTotalContaReceber"
          className={CLASSE_CAMPO}
            name="valorTotal"
            type="number"
            step="0.01"
            min="0.01"
            inputMode="decimal"
            required
            value={valorTotal}
            onChange={(event) => setValorTotal(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="dataRegistroContaReceber">Data de registro</Label>
          <Input
            id="dataRegistroContaReceber"
          className={CLASSE_CAMPO}
            name="dataRegistro"
            type="date"
            required
            value={dataRegistro}
            onChange={(event) => setDataRegistro(event.target.value)}
          />
        </div>
      </div>

      {tipo === "EMPRESTIMO" && (
        <div className="flex flex-col gap-1.5">
          <SelectField
            label="Conta de origem"
            placeholder={carregandoContasOrigem ? "Carregando contas..." : "Selecione a conta de origem"}
            disabled={carregandoContasOrigem}
            options={(contasOrigem ?? []).map((conta) => ({ value: conta.id, label: conta.nome }))}
            value={contaOrigemId || null}
            onChange={setContaOrigemId}
          />
          {erroContasOrigem && (
            <span className="text-[12px] text-alerta">
              Nao foi possivel carregar as contas de origem. Tente novamente.
            </span>
          )}
        </div>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="dataPrevistaContaReceber">Data prevista (opcional)</Label>
        <Input
          id="dataPrevistaContaReceber"
          className={CLASSE_CAMPO}
          name="dataPrevista"
          type="date"
          value={dataPrevista}
          onChange={(event) => setDataPrevista(event.target.value)}
        />
      </div>

      <div className="mt-1.5 flex justify-end gap-2.5">
        <Button
          type="button"
          variant="outline"
          onClick={onCancelar}
          disabled={isSubmitting}
          className={CLASSE_BOTAO_CANCELAR}
        >
          Cancelar
        </Button>
        <Button type="submit" disabled={isSubmitting} className={CLASSE_BOTAO_PRIMARIO}>
          {isSubmitting ? "Salvando..." : "Salvar"}
        </Button>
      </div>
    </form>
  )
}
