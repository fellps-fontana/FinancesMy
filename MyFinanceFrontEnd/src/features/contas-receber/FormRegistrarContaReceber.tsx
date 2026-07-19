import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { cn } from "@/shared/lib/utils"
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
export function FormRegistrarContaReceber() {
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
          onSuccess: limparFormulario,
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
        onSuccess: limparFormulario,
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
      className="flex flex-col gap-4 rounded-xl border border-border bg-card px-4 py-4"
    >
      {erroFormulario && (
        <Alert variant="destructive">
          <AlertDescription>{erroFormulario}</AlertDescription>
        </Alert>
      )}

      {/* Segmented control: nao ha Tabs/Toggle pronto no projeto, dois Button
          com variant condicional (default = ativo, outline = inativo) cobre
          o mesmo proposito sem introduzir componente novo - ver
          shared/ui/button.tsx pras variantes disponiveis. */}
      <div className="flex gap-2" role="group" aria-label="Tipo de conta a receber">
        <Button
          type="button"
          variant={tipo === "RECEBIVEL" ? "default" : "outline"}
          onClick={() => handleTrocarTipo("RECEBIVEL")}
          disabled={isSubmitting}
          className="flex-1"
        >
          Recebivel
        </Button>
        <Button
          type="button"
          variant={tipo === "EMPRESTIMO" ? "default" : "outline"}
          onClick={() => handleTrocarTipo("EMPRESTIMO")}
          disabled={isSubmitting}
          className="flex-1"
        >
          Emprestimo
        </Button>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="descricaoContaReceber">Descricao</Label>
        <Input
          id="descricaoContaReceber"
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
          <Label htmlFor="contaOrigemContaReceber">Conta de origem</Label>
          <select
            id="contaOrigemContaReceber"
            name="contaOrigemId"
            required
            disabled={carregandoContasOrigem}
            value={contaOrigemId}
            onChange={(event) => setContaOrigemId(event.target.value)}
            className={cn(
              "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30",
            )}
          >
            <option value="" disabled>
              {carregandoContasOrigem ? "Carregando contas..." : "Selecione a conta de origem"}
            </option>
            {contasOrigem?.map((conta) => (
              <option key={conta.id} value={conta.id}>
                {conta.nome}
              </option>
            ))}
          </select>
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
          name="dataPrevista"
          type="date"
          value={dataPrevista}
          onChange={(event) => setDataPrevista(event.target.value)}
        />
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={limparFormulario} disabled={isSubmitting}>
          Limpar
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Salvando..." : "Salvar"}
        </Button>
      </div>
    </form>
  )
}
