import { useState, type FormEvent } from "react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Chip } from "@/shared/ui/chip"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { SelectField } from "@/shared/ui/select-field"
import { Switch } from "@/shared/ui/switch"
import { cn } from "@/shared/lib/utils"
import { dataDeHoje, extrairAnoMes } from "@/shared/lib/datas"
import { CLASSE_CAMPO } from "@/shared/ui/FormModal"
import { AvisoLimiteGasto } from "@/features/lancamentos/components/AvisoLimiteGasto"
import { CategoriaCampoSelect } from "@/features/categorias/components/CategoriaCampoSelect"
import { FormLancamentoParcelado } from "@/features/lancamentos/components/FormLancamentoParcelado"
import { mensagemDeErro } from "@/features/lancamentos/lib/mensagemDeErro"
import { useCriarLancamento } from "@/features/lancamentos/hooks/useCriarLancamento"
import { useCriarLancamentoParcelado } from "@/features/lancamentos/hooks/useCriarLancamentoParcelado"
import { useEditarLancamento } from "@/features/lancamentos/hooks/useEditarLancamento"
// Contas MANUAL banco/investimento, sem cartao (item 12: compra de cartao tem
// fluxo proprio e nunca e lancamento avulso; item 18 repete a restricao na
// troca de conta). Mesmo hook reaproveitado por FormContaFixa.
import { useContasParaSelecao } from "@/features/contas-receber/hooks/useContasParaSelecao"
import {
  converterValorParaNumero,
  validarValor,
} from "@/features/lancamentos/lib/validarValorLancamento"
import type { LancamentoResponse, TipoLancamento } from "@/features/lancamentos/types"

// SUGERIDO nunca e opcao manual (item 5): nasce da conciliacao Open Finance (v2).
type StatusFormLancamento = "PENDENTE" | "PAGO"

const OPCOES_TIPO: { value: TipoLancamento; label: string }[] = [
  { value: "DEBIT", label: "Despesa" },
  { value: "CREDIT", label: "Receita" },
]

const OPCOES_STATUS: { value: StatusFormLancamento; label: string }[] = [
  { value: "PAGO", label: "Pago" },
  { value: "PENDENTE", label: "Pendente" },
]

const PARCELAS_PADRAO = "2"

function formatarValorParaCampo(valor: number): string {
  return String(valor).replace(".", ",")
}

function validarFormulario(
  contaId: string,
  descricao: string,
  valor: string,
  data: string,
  parcelas: string | null,
): string | null {
  if (contaId.trim().length === 0) return "Selecione uma conta."
  if (descricao.trim().length === 0) return "Informe uma descricao."

  const erroValor = validarValor(valor)
  if (erroValor) return erroValor

  if (data.trim().length === 0) return "Informe a data."

  if (parcelas !== null) {
    const quantidade = Number(parcelas)
    if (!Number.isInteger(quantidade) || quantidade < 2) {
      return "Informe 2 ou mais parcelas."
    }
  }

  return null
}

type FormLancamentoProps = {
  lancamentoParaEditar?: LancamentoResponse
  onSalvar?: () => void
  onCancelar?: () => void
}

// Presenca de `lancamentoParaEditar` decide criar vs editar (item 18: so
// avulso edita; descricao, valor, data, categoria, conta, tipo e status).
// No modo criar, despesa pode ser parcelada (item 17): o toggle "Parcelar"
// troca o envio para useCriarLancamentoParcelado.
export function FormLancamento({ lancamentoParaEditar, onSalvar, onCancelar }: FormLancamentoProps) {
  const modoEdicao = lancamentoParaEditar !== undefined

  const [tipo, setTipo] = useState<TipoLancamento>(lancamentoParaEditar?.tipo ?? "DEBIT")
  const [descricao, setDescricao] = useState(lancamentoParaEditar?.descricao ?? "")
  const [parcelar, setParcelar] = useState(false)
  const [parcelas, setParcelas] = useState(PARCELAS_PADRAO)
  const [valor, setValor] = useState(
    lancamentoParaEditar ? formatarValorParaCampo(lancamentoParaEditar.valor) : "",
  )
  const [data, setData] = useState(lancamentoParaEditar?.data ?? dataDeHoje())
  const [categoriaId, setCategoriaId] = useState<string | undefined>(
    lancamentoParaEditar?.categoriaId ?? undefined,
  )
  const [contaId, setContaId] = useState(lancamentoParaEditar?.contaId ?? "")
  const [status, setStatus] = useState<StatusFormLancamento>(
    lancamentoParaEditar?.status === "PAGO" ? "PAGO" : "PENDENTE",
  )
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutate: criarLancamento, isPending: criando } = useCriarLancamento()
  const { mutate: criarParcelado, isPending: criandoParcelado } = useCriarLancamentoParcelado()
  const { mutate: editarLancamento, isPending: editando } = useEditarLancamento()
  const {
    data: contas,
    isLoading: carregandoContas,
    error: erroContas,
  } = useContasParaSelecao()

  const isSubmitting = criando || criandoParcelado || editando
  const despesa = tipo === "DEBIT"
  const parcelando = parcelar && despesa && !modoEdicao
  const { ano, mes } = extrairAnoMes(data)
  const valorTotal = validarValor(valor) === null ? converterValorParaNumero(valor) : null
  const opcoesConta = (contas ?? []).map((conta) => ({ value: conta.id, label: conta.nome }))

  // Trocar o tipo muda o universo de categorias (Despesa x Receita, itens 7 e
  // 18): a categoria escolhida zera para nunca salvar tipo incompativel.
  function handleTrocarTipo(novoTipo: TipoLancamento) {
    if (novoTipo === tipo) return
    setTipo(novoTipo)
    setCategoriaId(undefined)
    if (novoTipo === "CREDIT") setParcelar(false)
    setErroFormulario(null)
  }

  function aoFalhar(contexto: string, fallback: string) {
    return (error: unknown) => {
      console.error(contexto, error)
      setErroFormulario(mensagemDeErro(error, fallback))
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarFormulario(contaId, descricao, valor, data, parcelando ? parcelas : null)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    const dadosBase = {
      descricao: descricao.trim(),
      categoriaId,
      status,
    }
    const valorNumerico = converterValorParaNumero(valor)
    const aoSalvar = () => {
      setErroFormulario(null)
      onSalvar?.()
    }

    if (modoEdicao && lancamentoParaEditar) {
      editarLancamento(
        {
          contaId: lancamentoParaEditar.contaId,
          lancamentoId: lancamentoParaEditar.id,
          request: {
            ...dadosBase,
            valor: valorNumerico,
            tipo,
            data,
            contaId: contaId !== lancamentoParaEditar.contaId ? contaId : undefined,
          },
        },
        {
          onSuccess: aoSalvar,
          onError: aoFalhar(
            "Falha ao editar lancamento",
            "Nao foi possivel salvar o lancamento. Tente novamente.",
          ),
        },
      )
      return
    }

    if (parcelando) {
      criarParcelado(
        {
          contaId,
          request: {
            ...dadosBase,
            valorTotal: valorNumerico,
            quantidadeParcelas: Number(parcelas),
            dataPrimeiraParcela: data,
          },
        },
        {
          onSuccess: aoSalvar,
          onError: aoFalhar(
            "Falha ao criar lancamento parcelado",
            "Nao foi possivel criar o lancamento parcelado. Tente novamente.",
          ),
        },
      )
      return
    }

    criarLancamento(
      { contaId, request: { ...dadosBase, valor: valorNumerico, tipo, data } },
      {
        onSuccess: aoSalvar,
        onError: aoFalhar(
          "Falha ao criar lancamento",
          "Nao foi possivel criar o lancamento. Tente novamente.",
        ),
      },
    )
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {erroFormulario && (
        <Alert variant="destructive">
          <AlertDescription>{erroFormulario}</AlertDescription>
        </Alert>
      )}

      {/* Tipo (item 2, CRITICA): o sinal vem sempre de `tipo`, nunca do valor. */}
      <div className="flex gap-2" role="group" aria-label="Tipo de lancamento">
        {OPCOES_TIPO.map((opcao) => (
          <button
            key={opcao.value}
            type="button"
            aria-pressed={tipo === opcao.value}
            disabled={isSubmitting}
            onClick={() => handleTrocarTipo(opcao.value)}
            className={cn(
              "flex-1 rounded-[9px] py-2.5 text-[13px] transition-colors outline-none focus-visible:ring-3 focus-visible:ring-ring/50 disabled:opacity-50",
              tipo === opcao.value
                ? "bg-accent-deep text-accent-soft"
                : "bg-muted text-text-muted hover:text-text-body",
            )}
          >
            {opcao.label}
          </button>
        ))}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="descricaoLancamento">Descricao</Label>
        <Input
          id="descricaoLancamento"
          name="descricao"
          autoFocus={!modoEdicao}
          required
          value={descricao}
          onChange={(event) => setDescricao(event.target.value)}
          className={CLASSE_CAMPO}
        />
      </div>

      {despesa && !modoEdicao && (
        <Switch
          checked={parcelar}
          onCheckedChange={setParcelar}
          disabled={isSubmitting}
          label="Parcelar"
          description="Divide o valor em parcelas mensais"
        />
      )}

      <div className="grid grid-cols-2 gap-2.5">
        <div className="flex min-w-0 flex-col gap-1.5">
          <Label htmlFor="valorLancamento">{parcelando ? "Valor total" : "Valor"}</Label>
          <Input
            id="valorLancamento"
            name="valor"
            inputMode="decimal"
            placeholder="0,00"
            required
            value={valor}
            onChange={(event) => setValor(event.target.value)}
            className={CLASSE_CAMPO}
          />
        </div>
        <div className="flex min-w-0 flex-col gap-1.5">
          <Label htmlFor="dataLancamento">{parcelando ? "Data da 1a parcela" : "Data"}</Label>
          <Input
            id="dataLancamento"
            name="data"
            type="date"
            required
            value={data}
            onChange={(event) => setData(event.target.value)}
            className={CLASSE_CAMPO}
          />
        </div>
      </div>

      {parcelando && (
        <FormLancamentoParcelado
          valorTotal={valorTotal}
          parcelas={parcelas}
          onParcelasChange={setParcelas}
          disabled={isSubmitting}
        />
      )}

      <div className="grid grid-cols-2 gap-2.5">
        <div className="min-w-0">
          <CategoriaCampoSelect
            mostrarIcone
            tipo={despesa ? "Despesa" : "Receita"}
            value={categoriaId}
            onChange={setCategoriaId}
            disabled={isSubmitting}
          />
        </div>
        <div className="min-w-0">
          <SelectField
            label="Conta"
            options={opcoesConta}
            value={contaId || null}
            onChange={setContaId}
            disabled={carregandoContas || isSubmitting}
            placeholder={carregandoContas ? "Carregando..." : "Selecione"}
          />
        </div>
      </div>
      {erroContas && (
        <span className="text-[12px] text-alerta">
          Nao foi possivel carregar as contas. Tente novamente.
        </span>
      )}

      {/* Aviso de limite (item 14): so despesa com categoria; nunca bloqueia. */}
      {despesa && categoriaId && <AvisoLimiteGasto categoriaId={categoriaId} ano={ano} mes={mes} />}

      <div className="flex flex-col gap-1.5">
        <div className="flex items-center justify-between">
          <span className="text-[13px] text-text-muted">Status</span>
          <div className="flex gap-1.5" role="group" aria-label="Status do lancamento">
            {OPCOES_STATUS.map((opcao) => (
              <Chip
                key={opcao.value}
                selected={status === opcao.value}
                disabled={isSubmitting}
                onClick={() => setStatus(opcao.value)}
                className={cn(
                  status !== opcao.value && opcao.value === "PAGO" && "text-positivo",
                  status !== opcao.value && opcao.value === "PENDENTE" && "text-alerta",
                )}
              >
                {opcao.label}
              </Chip>
            ))}
          </div>
        </div>
        {parcelando && (
          <span className="text-xs text-text-faint">
            Vale para a 1a parcela. As demais nascem pendentes.
          </span>
        )}
      </div>

      <div className="mt-1 flex justify-end gap-2.5">
        <Button type="button" variant="outline" size="lg" onClick={onCancelar} disabled={isSubmitting}>
          Cancelar
        </Button>
        <Button type="submit" size="lg" disabled={isSubmitting}>
          {isSubmitting ? "Salvando..." : modoEdicao ? "Salvar alteracoes" : "Salvar lancamento"}
        </Button>
      </div>
    </form>
  )
}
