import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { cn } from "@/shared/lib/utils"
import type { CategoriaResponse, TipoCategoria } from "@/features/categorias/types"

type CategoriaSelectProps = {
  tipo: TipoCategoria
  value: string | undefined
  onChange: (categoriaId: string) => void
}

type CategoriaOpcao = {
  id: string
  label: string
}

// Indentacao textual da subcategoria (regra-de-negocio.md item 7): usa
// espaco duro (nbsp) em vez de espaco comum porque o navegador colapsa
// espaco normal no inicio do texto de <option>, o que apagaria a
// indentacao visual na hora de renderizar o dropdown nativo.
const INDENTACAO_SUBCATEGORIA = "  "

// Achata a arvore categoria/subcategoria (regra-de-negocio.md item 7) num
// unico nivel de opcoes, ja que <select> nao renderiza hierarquia real.
// Categoria-pai aparece sem indentacao; subcategoria ganha indentacao e o
// nome do pai como prefixo ("Pai > Sub"), pra nao perder a hierarquia ao
// virar lista plana. Categoria/subcategoria arquivada (item 7: "subcategoria
// pode ser arquivada, nao deletada") nunca vira opcao selecionavel -
// arquivar e o jeito do usuario aposentar uma categoria sem apagar o
// historico que ja aponta pra ela.
//
// GET /api/categorias (sem parentId na query) devolve TODAS as categorias no
// array raiz, incluindo subcategorias soltas ali (alem de ja aninhadas em
// `categoria.subcategorias` do pai). O loop externo so processa como
// raiz+subcategorias os itens SEM parentId (mesmo filtro de
// filtrarOpcoesDeCategoriaPai/categoriasTopLevel) - senao a subcategoria
// aparece duas vezes (aninhada + solta), com id duplicado.
function achatarCategorias(categorias: CategoriaResponse[]): CategoriaOpcao[] {
  const opcoes: CategoriaOpcao[] = []

  for (const categoria of categorias) {
    if (categoria.parentId) {
      continue
    }

    if (!categoria.arquivada) {
      opcoes.push({ id: categoria.id, label: categoria.nome })
    }

    for (const subcategoria of categoria.subcategorias) {
      if (!subcategoria.arquivada) {
        opcoes.push({
          id: subcategoria.id,
          label: `${INDENTACAO_SUBCATEGORIA}${categoria.nome} > ${subcategoria.nome}`,
        })
      }
    }
  }

  return opcoes
}

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

  const opcoes = achatarCategorias(categorias ?? [])

  return (
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
          {opcao.label}
        </option>
      ))}
    </select>
  )
}
