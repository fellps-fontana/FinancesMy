import { useMemo } from "react"
import { SelectField, type SelectOption } from "@/shared/ui/select-field"
import { obterIconeCatalogo } from "@/shared/lib/catalogoIcones"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { montarOpcoesCategoria } from "@/features/categorias/lib/montarOpcoesCategoria"
import type { TipoCategoria } from "@/features/categorias/types"

type CategoriaCampoSelectProps = {
  tipo: TipoCategoria
  value: string | undefined
  onChange: (categoriaId: string) => void
  disabled?: boolean
  mostrarIcone?: boolean
}

function iconeDe(icone?: string) {
  const Icone = obterIconeCatalogo(icone)
  return <Icone className="size-4 text-accent-soft" strokeWidth={1.6} aria-hidden="true" />
}

// Campo de categoria no visual dos demais campos de formulario (SelectField).
// Unico ponto de selecao de categoria dos formularios; o tipo filtra
// Despesa/Receita no backend e arquivadas nunca viram opcao.
export function CategoriaCampoSelect({
  tipo,
  value,
  onChange,
  disabled,
  mostrarIcone = false,
}: CategoriaCampoSelectProps) {
  const { data: categorias, isLoading, isError } = useCategorias(tipo)
  const opcoes = useMemo<SelectOption<string>[]>(
    () =>
      montarOpcoesCategoria(categorias ?? []).map((opcao) => ({
        value: opcao.id,
        label: opcao.label,
        icon: mostrarIcone ? iconeDe(opcao.icone) : undefined,
      })),
    [categorias, mostrarIcone],
  )

  return (
    <SelectField
      label="Categoria"
      options={opcoes}
      value={value ?? null}
      onChange={onChange}
      disabled={disabled || isLoading}
      placeholder={isLoading ? "Carregando..." : isError ? "Nao foi possivel carregar" : "Selecione"}
    />
  )
}
