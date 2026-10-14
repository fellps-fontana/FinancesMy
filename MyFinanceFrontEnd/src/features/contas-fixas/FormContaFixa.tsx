import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { SegmentedTabs } from "@/shared/ui/segmented-tabs"
import { SelectField } from "@/shared/ui/select-field"
import { CategoriaCampoSelect } from "@/features/categorias/components/CategoriaCampoSelect"
import { CLASSE_BOTAO_CANCELAR, CLASSE_BOTAO_PRIMARIO, CLASSE_CAMPO } from "@/shared/ui/FormModal"
import { useCriarContaFixa } from "@/features/contas-fixas/hooks/useCriarContaFixa"
import { useEditarContaFixa } from "@/features/contas-fixas/hooks/useEditarContaFixa"
// Nao existe hook de "listar contas" generico em features/contas/ (feature
// ainda e so um placeholder - stack.md "Feature so com .gitkeep e placeholder
// de modulo nao implementado"). O unico hook que ja resolve "contas MANUAL
// disponiveis pra selecao de origem" e useContasParaSelecao, hoje dentro de
// contas-receber/hooks (busca banco + investimento em paralelo, ver comentario
// no proprio hook - o back nao tem endpoint combinado por tipo). Reaproveitado
// aqui em vez de duplicar a mesma chamada combinada: e o mesmo padrao de
// "conta de origem" do item 13 (emprestimo) aplicado a "conta de origem" do
// item 6 (conta fixa) - mesmo conjunto de contas MANUAL, mesma regra. Import
// cross-feature: candidato a shared/hooks/ (usado por 2+ features).
import { useContasParaSelecao } from "@/features/contas-receber/hooks/useContasParaSelecao"
import {
  converterDiaVencimentoParaNumero,
  converterValorParaNumero,
  validarCriarContaFixa,
  validarEditarContaFixa,
} from "@/features/contas-fixas/lib/validarContaFixa"
import type { ContaFixaResponse, PeriodicidadeContaFixa } from "@/features/contas-fixas/types"

type FormContaFixaProps = {
  // Presenca de `contaFixaParaEditar` define o modo do formulario: ausente ->
  // CRIAR (useCriarContaFixa, campo de conta de origem selecionavel);
  // presente -> EDITAR (useEditarContaFixa). EditarContaFixaRequest nao
  // aceita contaId nem descricao (types.ts) - a conta de origem e a
  // descricao nao sao ofertadas como campos editaveis neste modo, so exibidas
  // para dar contexto de qual conta fixa esta sendo editada.
  contaFixaParaEditar?: ContaFixaResponse
  onSalvar?: () => void
  onCancelar?: () => void
}

const OPCOES_RECORRENCIA = [
  { value: "MENSAL", label: "Mensal" },
  { value: "ANUAL", label: "Anual" },
] as const satisfies ReadonlyArray<{ value: PeriodicidadeContaFixa; label: string }>

// Categoria (categoriaId, regra-de-negocio.md item 6: "categoria vinculada",
// FK opcional) e periodicidade (item 6, revisao 2026-07-27: MENSAL/ANUAL)
// agora sao campos editaveis do formulario, em ambos os modos - a omissao
// anterior de categoria (TASK-063) valia so enquanto CategoriaSelect nao
// existia no projeto. Em modo edicao, os dois estados nascem do valor ja
// existente na ContaFixa e sao SEMPRE reenviados no payload (nunca omitidos)
// - ver ContaFixaService.EditarContaFixa no backend, que faz substituicao
// total (`contaFixa.CategoriaId = categoriaId`), nao merge; se o submit
// omitisse o campo, uma ContaFixa com categoria/periodicidade ja definida
// perderia esse valor so por editar valor/dia_vencimento.
export function FormContaFixa({ contaFixaParaEditar, onSalvar, onCancelar }: FormContaFixaProps) {
  const modoEdicao = contaFixaParaEditar !== undefined

  const [descricao, setDescricao] = useState(contaFixaParaEditar?.descricao ?? "")
  const [valor, setValor] = useState(
    contaFixaParaEditar ? String(contaFixaParaEditar.valor) : "",
  )
  const [diaVencimento, setDiaVencimento] = useState(
    contaFixaParaEditar ? String(contaFixaParaEditar.diaVencimento) : "",
  )
  // "MENSAL" e o padrao da regra de negocio (item 6) - mesmo valor usado
  // quando o backend nao recebe periodicidade num registro criado antes
  // desta feature.
  const [periodicidade, setPeriodicidade] = useState<PeriodicidadeContaFixa>(
    contaFixaParaEditar?.periodicidade ?? "MENSAL",
  )
  const [categoriaId, setCategoriaId] = useState<string | undefined>(
    contaFixaParaEditar?.categoriaId ?? undefined,
  )
  const [contaId, setContaId] = useState("")
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const { mutate: criarContaFixa, isPending: criando } = useCriarContaFixa()
  const { mutate: editarContaFixa, isPending: editando } = useEditarContaFixa()

  const {
    data: contasOrigem,
    isLoading: carregandoContasOrigem,
    error: erroContasOrigem,
  } = useContasParaSelecao()

  if (erroContasOrigem) {
    console.error("Falha ao carregar contas de origem para conta fixa", erroContasOrigem)
  }

  const isSubmitting = criando || editando

  function restaurarValoresIniciais() {
    setDescricao(contaFixaParaEditar?.descricao ?? "")
    setValor(contaFixaParaEditar ? String(contaFixaParaEditar.valor) : "")
    setDiaVencimento(contaFixaParaEditar ? String(contaFixaParaEditar.diaVencimento) : "")
    setPeriodicidade(contaFixaParaEditar?.periodicidade ?? "MENSAL")
    setCategoriaId(contaFixaParaEditar?.categoriaId ?? undefined)
    setContaId("")
    setErroFormulario(null)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (modoEdicao && contaFixaParaEditar) {
      const erroValidacao = validarEditarContaFixa(valor, diaVencimento)
      if (erroValidacao) {
        setErroFormulario(erroValidacao)
        return
      }

      editarContaFixa(
        {
          id: contaFixaParaEditar.id,
          request: {
            valor: converterValorParaNumero(valor),
            diaVencimento: converterDiaVencimentoParaNumero(diaVencimento),
            categoriaId,
            periodicidade,
          },
        },
        {
          onSuccess: () => {
            setErroFormulario(null)
            onSalvar?.()
          },
          onError: (error) => {
            console.error("Falha ao editar conta fixa", error)
            setErroFormulario(
              error instanceof ApiError
                ? error.message
                : "Nao foi possivel salvar a conta fixa. Tente novamente.",
            )
          },
        },
      )
      return
    }

    const erroValidacao = validarCriarContaFixa(descricao, valor, diaVencimento, contaId)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    criarContaFixa(
      {
        contaId,
        descricao: descricao.trim(),
        valor: converterValorParaNumero(valor),
        diaVencimento: converterDiaVencimentoParaNumero(diaVencimento),
        categoriaId,
        periodicidade,
      },
      {
        onSuccess: () => {
          restaurarValoresIniciais()
          onSalvar?.()
        },
        onError: (error) => {
          console.error("Falha ao criar conta fixa", error)
          setErroFormulario(
            error instanceof ApiError
              ? error.message
              : "Nao foi possivel criar a conta fixa. Tente novamente.",
          )
        },
      },
    )
  }

  const opcoesConta = (contasOrigem ?? []).map((conta) => ({ value: conta.id, label: conta.nome }))
  const contaSelecionada = modoEdicao ? (contaFixaParaEditar?.contaId ?? null) : contaId || null

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {erroFormulario && (
        <Alert variant="destructive">
          <AlertDescription>{erroFormulario}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="descricaoContaFixa" className="text-[13px] text-text-muted">
          Nome
        </Label>
        <Input
          id="descricaoContaFixa"
          name="descricao"
          placeholder="Ex: Aluguel"
          autoFocus={!modoEdicao}
          required
          disabled={modoEdicao}
          value={descricao}
          onChange={(event) => setDescricao(event.target.value)}
          className={CLASSE_CAMPO}
        />
      </div>

      <div className="grid grid-cols-2 gap-2.5">
        <div className="flex min-w-0 flex-col gap-1.5">
          <Label htmlFor="valorContaFixa" className="text-[13px] text-text-muted">
            Valor
          </Label>
          <Input
            id="valorContaFixa"
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
          <Label htmlFor="diaVencimentoContaFixa" className="text-[13px] text-text-muted">
            Dia de vencimento
          </Label>
          <Input
            id="diaVencimentoContaFixa"
            name="diaVencimento"
            type="number"
            step="1"
            min="1"
            max="31"
            inputMode="numeric"
            required
            value={diaVencimento}
            onChange={(event) => setDiaVencimento(event.target.value)}
            className={CLASSE_CAMPO}
          />
        </div>
      </div>

      {/* Conta fixa e sempre DEBIT, entao so categoria de Despesa
          (regra-de-negocio.md item 6). */}
      <CategoriaCampoSelect tipo="Despesa" value={categoriaId} onChange={setCategoriaId} />

      {/* Conta de pagamento: EditarContaFixaRequest nao aceita contaId
          (types.ts), entao em edicao o campo e so exibido, desabilitado. */}
      <div className="flex flex-col gap-1">
        <SelectField
          label="Conta de pagamento"
          options={opcoesConta}
          value={contaSelecionada}
          onChange={setContaId}
          disabled={modoEdicao || carregandoContasOrigem}
          placeholder={carregandoContasOrigem ? "Carregando contas..." : "Selecione a conta"}
        />
        {erroContasOrigem && (
          <span className="text-[12px] text-alerta">
            Nao foi possivel carregar as contas. Tente novamente.
          </span>
        )}
      </div>

      {/* Conta fixa nunca tem parcelamento (regra-de-negocio.md item 6). */}
      <div className="flex items-center justify-between gap-3">
        <span className="text-[13px] text-text-muted">Recorrencia</span>
        <SegmentedTabs
          ariaLabel="Recorrencia da conta fixa"
          options={OPCOES_RECORRENCIA}
          value={periodicidade}
          onChange={setPeriodicidade}
          className="w-48"
        />
      </div>

      {modoEdicao && (
        <p className="text-xs leading-normal text-text-faint">
          Mudar valor, dia ou categoria atualiza os lancamentos pendentes ja gerados. Os pagos nao
          mudam.
        </p>
      )}

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
          {isSubmitting ? "Salvando..." : modoEdicao ? "Salvar alteracoes" : "Salvar conta fixa"}
        </Button>
      </div>
    </form>
  )
}
