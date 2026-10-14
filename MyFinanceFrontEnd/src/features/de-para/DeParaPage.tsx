import { useState } from "react"
import { Plus, Trash2 } from "lucide-react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { FormModal } from "@/shared/ui/FormModal"
import { Button } from "@/shared/ui/button"
import { ConfirmDialog } from "@/shared/ui/confirm-dialog"
import { PageHeader } from "@/shared/ui/page-header"
import { DeParaItem } from "@/features/de-para/components/DeParaItem"
import { FormDePara } from "@/features/de-para/components/FormDePara"
import { useDeParaCategorias } from "@/features/de-para/hooks/useDeParaCategorias"
import { useRemoverDePara } from "@/features/de-para/hooks/useRemoverDePara"
import type { DeParaCategoriaResponse } from "@/features/de-para/types"

type EstadoFormulario = { modo: "criar" } | { modo: "editar"; vinculo: DeParaCategoriaResponse } | null

// Container roteado. v1 (regra-de-negocio.md item 7 / Escopo v1 vs v2): so a
// lista de vinculos cadastrados; abas Pendentes/Sugerido ficam para a v2.
export function DeParaPage() {
  const [formulario, setFormulario] = useState<EstadoFormulario>(null)
  const [paraRemover, setParaRemover] = useState<DeParaCategoriaResponse | null>(null)
  const [erroRemocao, setErroRemocao] = useState<string | null>(null)

  const { data: vinculos, isLoading, error } = useDeParaCategorias()
  const { mutate: remover, isPending: removendo } = useRemoverDePara()

  function fecharFormulario() {
    setFormulario(null)
  }

  function confirmarRemocao() {
    if (!paraRemover) return
    setErroRemocao(null)
    remover(paraRemover.id, {
      onSuccess: () => setParaRemover(null),
      onError: (e) => {
        setParaRemover(null)
        setErroRemocao(e instanceof ApiError ? e.message : "Nao foi possivel remover o vinculo.")
      },
    })
  }

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <div className="flex flex-col gap-1">
        <PageHeader
          title="De-para de categoria"
          action={
            <>
              <Button
                type="button"
                onClick={() => setFormulario({ modo: "criar" })}
                className="hidden h-auto gap-1.5 rounded-[9px] px-4 py-2.5 text-[13px] md:inline-flex"
              >
                <Plus className="size-3.5" strokeWidth={2} aria-hidden="true" />
                Novo vinculo
              </Button>
              <Button
                type="button"
                onClick={() => setFormulario({ modo: "criar" })}
                aria-label="Novo vinculo"
                className="size-8 rounded-[9px] p-0 md:hidden"
              >
                <Plus className="size-4" strokeWidth={2} aria-hidden="true" />
              </Button>
            </>
          }
        />
        <p className="text-[13px] text-text-muted">
          Ajuste como as categorias importadas do banco viram categorias do app
        </p>
      </div>

      {erroRemocao ? (
        <Alert variant="destructive">
          <AlertDescription>{erroRemocao}</AlertDescription>
        </Alert>
      ) : null}

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar os vinculos</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : vinculos && vinculos.length > 0 ? (
        <div className="md:overflow-hidden md:rounded-2xl md:border-[0.5px] md:border-border md:bg-card">
          <div className="hidden grid-cols-[2fr_0.4fr_2fr_1fr_auto] gap-4 border-b-[0.5px] border-border px-[22px] py-3.5 md:grid">
            <span className="text-xs text-text-muted">Categoria importada</span>
            <span />
            <span className="text-xs text-text-muted">Categoria do app</span>
            <span className="text-right text-xs text-text-muted">Status</span>
            <span className="w-16" aria-hidden="true" />
          </div>
          <ul className="flex flex-col gap-2.5 md:block">
            {vinculos.map((vinculo) => (
              <DeParaItem
                key={vinculo.id}
                vinculo={vinculo}
                onEditar={(v) => setFormulario({ modo: "editar", vinculo: v })}
                onRemover={setParaRemover}
              />
            ))}
          </ul>
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhum vinculo cadastrado ainda.</p>
      )}

      {formulario ? (
        <FormModal
          onClose={fecharFormulario}
          title={formulario.modo === "criar" ? "Novo vinculo" : "Editar vinculo"}
        >
          <FormDePara
            vinculoParaEditar={formulario.modo === "editar" ? formulario.vinculo : undefined}
            onSalvar={fecharFormulario}
            onCancelar={fecharFormulario}
          />
        </FormModal>
      ) : null}

      <ConfirmDialog
        open={paraRemover !== null}
        title="Remover vinculo?"
        description={
          <>
            <strong className="font-medium text-text-body">{paraRemover?.categoriaPierre}</strong> deixa de ser
            mapeada automaticamente; novos lancamentos importados ficam sem categoria.
          </>
        }
        icon={<Trash2 className="size-[18px]" />}
        confirmLabel="Remover"
        destructive
        loading={removendo}
        onConfirm={confirmarRemocao}
        onCancel={() => setParaRemover(null)}
      />
    </div>
  )
}
