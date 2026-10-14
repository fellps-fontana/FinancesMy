import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { cn } from "@/shared/lib/utils"
import { obterIconeCatalogo } from "@/shared/lib/catalogoIcones"
import { montarOpcoesCategoria } from "@/features/categorias/lib/montarOpcoesCategoria"
import type { TipoCategoria } from "@/features/categorias/types"

type CategoriaSelectProps = {
  tipo: TipoCategoria
  value: string | undefined
  onChange: (categoriaId: string) => void
}

// Indentacao textual da subcategoria (regra-de-negocio.md item 7): usa
// espaco duro (nbsp) em vez de espaco comum porque o navegador colapsa
// espaco normal no inicio do texto de <option>, o que apagaria a
// indentacao visual na hora de renderizar o dropdown nativo.
const INDENTACAO_SUBCATEGORIA = "  "

// Dropdown reutilizavel de categoria, achatando categoria + subcategorias
// (regra-de-negocio.md item 7) num unico nivel selecionavel. Pensado pra ser
// consumido tanto pela propria feature categorias quanto, futuramente, por
// lancamentos - por isso a prop `tipo` e obrigatoria e o contrato e generico
// (sem nada especifico de outra feature importado aqui; a dependencia e no
// sentido inverso, lancamentos -> categorias).
//
// Uma unica chamada de rede: useCategorias(tipo) ja filtra por tipo no
// backend: o achatamento da hierarquia acontece so no client, sem endpoint
// extra.
export function CategoriaSelect({ tipo, value, onChange }: CategoriaSelectProps) {
  const { data: categorias, isLoading, isError } = useCategorias(tipo)

  const opcoes = montarOpcoesCategoria(categorias ?? [])

  // <option> nativa nao renderiza SVG/icone (limitacao do elemento, nao do
  // design) - por isso o icone da categoria selecionada aparece como preview
  // ao lado do dropdown, no mesmo quadrado 34px usado em CategoriaItem, em
  // vez de um icone por linha dentro da lista nativa.
  const opcaoSelecionada = opcoes.find((opcao) => opcao.id === value)
  const Icone = obterIconeCatalogo(opcaoSelecionada?.icone)

  return (
    <div className="flex items-center gap-2">
      <div className="flex size-[34px] shrink-0 items-center justify-center rounded-lg bg-accent-deep">
        <Icone className="size-4 text-accent-soft" strokeWidth={1.6} aria-hidden="true" />
      </div>
      <select
        aria-label="Categoria"
        value={value ?? ""}
        disabled={isLoading}
        onChange={(event) => onChange(event.target.value)}
        className={cn(
          "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm dark:bg-input/30",
        )}
      >
        <option value="" disabled>
          {isLoading
            ? "Carregando categorias..."
            : isError
              ? "Nao foi possivel carregar as categorias"
              : "Selecione uma categoria"}
        </option>
        {opcoes.map((opcao) => (
          <option key={opcao.id} value={opcao.id}>
            {opcao.subcategoria ? `${INDENTACAO_SUBCATEGORIA}${opcao.label}` : opcao.label}
          </option>
        ))}
      </select>
    </div>
  )
}
