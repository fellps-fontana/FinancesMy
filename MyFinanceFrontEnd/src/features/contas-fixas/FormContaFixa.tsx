import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { cn } from "@/shared/lib/utils"
import { CategoriaSelect } from "@/features/categorias/components/CategoriaSelect"
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
// cross-feature nao e o ideal de organizacao (stack.md reserva shared/hooks/
// para hook usado por 2+ features), mas mover o hook esta fora do escopo
// desta task (ARQUIVOS PERMITIDOS restringe a so este componente + o
// validador) - registrado aqui para uma task futura promover
// useContasParaSelecao para shared/hooks/ quando tocar em contas-receber de
// novo.
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
}

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
export function FormContaFixa({ contaFixaParaEditar, onSalvar }: FormContaFixaProps) {
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
  } = useContasParaSelecao({ enabled: !modoEdicao })

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
        <Label htmlFor="descricaoContaFixa">Descricao</Label>
        <Input
          id="descricaoContaFixa"
          name="descricao"
          placeholder="Ex: Aluguel"
          autoFocus={!modoEdicao}
          required
          disabled={modoEdicao}
          value={descricao}
          onChange={(event) => setDescricao(event.target.value)}
        />
      </div>

      {/* Periodicidade (regra-de-negocio.md item 6, revisao 2026-07-27):
          editavel em ambos os modos (criar e editar), mesmo segmented control
          ja usado em FormRegistrarContaReceber.tsx (dois Button com variant
          condicional - nao ha Tabs/Toggle pronto no projeto). */}
      <div className="flex flex-col gap-1.5">
        <Label>Periodicidade</Label>
        <div className="flex gap-2" role="group" aria-label="Periodicidade da conta fixa">
          <Button
            type="button"
            variant={periodicidade === "MENSAL" ? "default" : "outline"}
            onClick={() => setPeriodicidade("MENSAL")}
            disabled={isSubmitting}
            className="flex-1"
          >
            Mensal
          </Button>
          <Button
            type="button"
            variant={periodicidade === "ANUAL" ? "default" : "outline"}
            onClick={() => setPeriodicidade("ANUAL")}
            disabled={isSubmitting}
            className="flex-1"
          >
            Anual
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="valorContaFixa">Valor</Label>
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
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="diaVencimentoContaFixa">Dia de vencimento</Label>
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
          />
        </div>
      </div>

      {/* Categoria (regra-de-negocio.md item 6: "categoria vinculada", FK
          opcional) - sempre tipo Despesa, porque conta fixa e sempre DEBIT
          (item 6: "nao existe conta fixa de recebimento"). Editavel em ambos
          os modos, igual periodicidade. */}
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="categoriaContaFixa">Categoria</Label>
        <CategoriaSelect tipo="Despesa" value={categoriaId} onChange={setCategoriaId} />
      </div>

      {/* Conta de origem so aparece no modo criar - EditarContaFixaRequest
          nao aceita contaId (types.ts), entao o campo nem e ofertado em
          edicao (nao so desabilitado). */}
      {!modoEdicao && (
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="contaOrigemContaFixa">Conta de origem</Label>
          <select
            id="contaOrigemContaFixa"
            name="contaId"
            required
            disabled={carregandoContasOrigem}
            value={contaId}
            onChange={(event) => setContaId(event.target.value)}
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

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={restaurarValoresIniciais} disabled={isSubmitting}>
          {modoEdicao ? "Desfazer" : "Limpar"}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Salvando..." : "Salvar"}
        </Button>
      </div>
    </form>
  )
}
