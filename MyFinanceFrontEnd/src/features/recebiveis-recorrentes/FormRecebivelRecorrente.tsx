import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { cn } from "@/shared/lib/utils"
import { CategoriaSelect } from "@/features/categorias/components/CategoriaSelect"
import { useCriarRecebivelRecorrente } from "@/features/recebiveis-recorrentes/hooks/useCriarRecebivelRecorrente"
import { useEditarRecebivelRecorrente } from "@/features/recebiveis-recorrentes/hooks/useEditarRecebivelRecorrente"
import {
  MESES_DO_ANO,
  nomeDiaDaSemana,
} from "@/features/recebiveis-recorrentes/lib/formatarRecorrencia"
import {
  converterInteiroParaNumero,
  converterValorParaNumero,
  validarCriarRecebivelRecorrente,
  validarEditarRecebivelRecorrente,
} from "@/features/recebiveis-recorrentes/lib/validarRecebivelRecorrente"
import type {
  DiaDaSemana,
  PeriodicidadeRecebivelRecorrente,
  RecebivelRecorrenteResponse,
} from "@/features/recebiveis-recorrentes/types"

// Segmented control reutiliza o padrao ja usado em FormRegistrarContaReceber
// / FormContaFixa (dois ou mais Button com variant condicional - nao ha
// Tabs/Toggle pronto no projeto).
const PERIODICIDADES: ReadonlyArray<{ valor: PeriodicidadeRecebivelRecorrente; label: string }> = [
  { valor: "MENSAL", label: "Mensal" },
  { valor: "ANUAL", label: "Anual" },
  { valor: "SEMANAL", label: "Semanal" },
]

const DIAS_DA_SEMANA: readonly DiaDaSemana[] = ["SEG", "TER", "QUA", "QUI", "SEX", "SAB", "DOM"]

const CLASSE_SELECT =
  "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30"

type FormRecebivelRecorrenteProps = {
  // Presenca de `recebivelParaEditar` define o modo: ausente -> CRIAR;
  // presente -> EDITAR. EditarRecebivelRecorrenteRequest nao aceita descricao
  // (types.ts), entao no modo edicao a descricao e so exibida (disabled),
  // nunca reenviada.
  recebivelParaEditar?: RecebivelRecorrenteResponse
  onSalvar?: () => void
}

// Recebivel recorrente e sempre entrada/receita (item 15, familia CREDIT do
// item 13) - a categoria vinculada e sempre do tipo Receita.
export function FormRecebivelRecorrente({
  recebivelParaEditar,
  onSalvar,
}: FormRecebivelRecorrenteProps) {
  const modoEdicao = recebivelParaEditar !== undefined

  const [descricao, setDescricao] = useState(recebivelParaEditar?.descricao ?? "")
  const [valor, setValor] = useState(
    recebivelParaEditar ? String(recebivelParaEditar.valor) : "",
  )
  const [periodicidade, setPeriodicidade] = useState<PeriodicidadeRecebivelRecorrente>(
    recebivelParaEditar?.periodicidade ?? "MENSAL",
  )
  const [diaVencimento, setDiaVencimento] = useState(
    recebivelParaEditar?.diaVencimento != null ? String(recebivelParaEditar.diaVencimento) : "",
  )
  const [mesReferencia, setMesReferencia] = useState(
    recebivelParaEditar?.mesReferencia != null ? String(recebivelParaEditar.mesReferencia) : "",
  )
  const [diaDaSemana, setDiaDaSemana] = useState<string>(recebivelParaEditar?.diaDaSemana ?? "")
  const [categoriaId, setCategoriaId] = useState<string | undefined>(
    recebivelParaEditar?.categoriaId ?? undefined,
  )
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutate: criarRecebivel, isPending: criando } = useCriarRecebivelRecorrente()
  const { mutate: editarRecebivel, isPending: editando } = useEditarRecebivelRecorrente()

  const isSubmitting = criando || editando

  function restaurarValoresIniciais() {
    setDescricao(recebivelParaEditar?.descricao ?? "")
    setValor(recebivelParaEditar ? String(recebivelParaEditar.valor) : "")
    setPeriodicidade(recebivelParaEditar?.periodicidade ?? "MENSAL")
    setDiaVencimento(
      recebivelParaEditar?.diaVencimento != null ? String(recebivelParaEditar.diaVencimento) : "",
    )
    setMesReferencia(
      recebivelParaEditar?.mesReferencia != null ? String(recebivelParaEditar.mesReferencia) : "",
    )
    setDiaDaSemana(recebivelParaEditar?.diaDaSemana ?? "")
    setCategoriaId(recebivelParaEditar?.categoriaId ?? undefined)
    setErroFormulario(null)
  }

  // Monta SO os campos de data que a periodicidade escolhida usa (item 15) -
  // o backend recorta o resto para null de qualquer forma, mas enviar so o
  // relevante deixa o payload honesto.
  function camposDaPeriodicidade() {
    if (periodicidade === "MENSAL") {
      return { diaVencimento: converterInteiroParaNumero(diaVencimento) }
    }
    if (periodicidade === "ANUAL") {
      return {
        diaVencimento: converterInteiroParaNumero(diaVencimento),
        mesReferencia: converterInteiroParaNumero(mesReferencia),
      }
    }
    return { diaDaSemana: diaDaSemana as DiaDaSemana }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (modoEdicao && recebivelParaEditar) {
      const erroValidacao = validarEditarRecebivelRecorrente(
        valor,
        periodicidade,
        diaVencimento,
        mesReferencia,
        diaDaSemana,
      )
      if (erroValidacao) {
        setErroFormulario(erroValidacao)
        return
      }

      editarRecebivel(
        {
          id: recebivelParaEditar.id,
          request: {
            valor: converterValorParaNumero(valor),
            periodicidade,
            categoriaId,
            ...camposDaPeriodicidade(),
          },
        },
        {
          onSuccess: () => {
            setErroFormulario(null)
            onSalvar?.()
          },
          onError: (error) => {
            console.error("Falha ao editar recebivel recorrente", error)
            setErroFormulario(
              error instanceof ApiError
                ? error.message
                : "Nao foi possivel salvar o recebivel recorrente. Tente novamente.",
            )
          },
        },
      )
      return
    }

    const erroValidacao = validarCriarRecebivelRecorrente(
      descricao,
      valor,
      periodicidade,
      diaVencimento,
      mesReferencia,
      diaDaSemana,
    )
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    criarRecebivel(
      {
        descricao: descricao.trim(),
        valor: converterValorParaNumero(valor),
        periodicidade,
        categoriaId,
        ...camposDaPeriodicidade(),
      },
      {
        onSuccess: () => {
          restaurarValoresIniciais()
          onSalvar?.()
        },
        onError: (error) => {
          console.error("Falha ao criar recebivel recorrente", error)
          setErroFormulario(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel criar o recebivel recorrente. Tente novamente.",
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

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="descricaoRecebivelRecorrente">Descrição</Label>
        <Input
          id="descricaoRecebivelRecorrente"
          name="descricao"
          placeholder="Ex: Salário"
          autoFocus={!modoEdicao}
          required
          disabled={modoEdicao}
          value={descricao}
          onChange={(event) => setDescricao(event.target.value)}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label>Periodicidade</Label>
        <div className="flex gap-2" role="group" aria-label="Periodicidade do recebível recorrente">
          {PERIODICIDADES.map(({ valor: opcao, label }) => (
            <Button
              key={opcao}
              type="button"
              variant={periodicidade === opcao ? "default" : "outline"}
              onClick={() => setPeriodicidade(opcao)}
              disabled={isSubmitting}
              className="flex-1"
            >
              {label}
            </Button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="valorRecebivelRecorrente">Valor</Label>
          <Input
            id="valorRecebivelRecorrente"
            name="valor"
            type="number"
            step="0.01"
            min="0.01"
            inputMode="decimal"
            required
            value={valor}
            onChange={(event) => setValor(event.target.value)}
          />
        </div>

        {/* MENSAL e ANUAL pedem dia de vencimento (item 15). */}
        {periodicidade !== "SEMANAL" && (
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="diaVencimentoRecebivelRecorrente">Dia de vencimento</Label>
            <Input
              id="diaVencimentoRecebivelRecorrente"
              name="diaVencimento"
              type="number"
              step="1"
              min="1"
              max="31"
              inputMode="numeric"
              required
              value={diaVencimento}
              onChange={(event) => setDiaVencimento(event.target.value)}
            />
          </div>
        )}
      </div>

      {/* ANUAL tambem pede o mes de referencia, com nomes de mes (item 15). */}
      {periodicidade === "ANUAL" && (
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="mesReferenciaRecebivelRecorrente">Mês de referência</Label>
          <select
            id="mesReferenciaRecebivelRecorrente"
            name="mesReferencia"
            required
            value={mesReferencia}
            onChange={(event) => setMesReferencia(event.target.value)}
            className={cn(CLASSE_SELECT)}
          >
            <option value="" disabled>
              Selecione o mês
            </option>
            {MESES_DO_ANO.map((mes) => (
              <option key={mes.valor} value={mes.valor}>
                {mes.nome}
              </option>
            ))}
          </select>
        </div>
      )}

      {/* SEMANAL pede o dia da semana, rotulo em portugues (item 15). */}
      {periodicidade === "SEMANAL" && (
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="diaDaSemanaRecebivelRecorrente">Dia da semana</Label>
          <select
            id="diaDaSemanaRecebivelRecorrente"
            name="diaDaSemana"
            required
            value={diaDaSemana}
            onChange={(event) => setDiaDaSemana(event.target.value)}
            className={cn(CLASSE_SELECT)}
          >
            <option value="" disabled>
              Selecione o dia da semana
            </option>
            {DIAS_DA_SEMANA.map((dia) => (
              <option key={dia} value={dia}>
                {nomeDiaDaSemana(dia)}
              </option>
            ))}
          </select>
        </div>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="categoriaRecebivelRecorrente">Categoria</Label>
        <CategoriaSelect tipo="Receita" value={categoriaId} onChange={setCategoriaId} />
      </div>

      <div className="flex justify-end gap-2">
        <Button
          type="button"
          variant="ghost"
          onClick={restaurarValoresIniciais}
          disabled={isSubmitting}
        >
          {modoEdicao ? "Desfazer" : "Limpar"}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Salvando..." : "Salvar"}
        </Button>
      </div>
    </form>
  )
}
