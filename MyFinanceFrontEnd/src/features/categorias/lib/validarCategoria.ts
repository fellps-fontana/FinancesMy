// Validacao pura do formulario de Categoria (regra-de-negocio.md item 7).
// Testavel isoladamente do componente - ver clean-code.md
// "Organizacao (React)".
import type { CategoriaResponse } from "@/features/categorias/types"

// Categoria-pai (parentId) so pode ser categoria de MESMO tipo e NIVEL 0 (sem
// parentId proprio) - regra-de-negocio.md item 7: subcategoria via
// auto-relacionamento, sem subcategoria de subcategoria prevista no dominio.
// A propria categoria em edicao nunca pode ser seu proprio pai. Funcao pura
// (sem hook, sem fetch) - por isso vive em lib/, nao no componente (ver
// stack.md, criterio objetivo lib/ vs hooks/ vs components/).
export function filtrarOpcoesDeCategoriaPai(
  categorias: CategoriaResponse[] | undefined,
  categoriaEmEdicaoId: string | undefined,
): CategoriaResponse[] {
  return (categorias ?? []).filter(
    (categoria) =>
      !categoria.parentId && !categoria.arquivada && categoria.id !== categoriaEmEdicaoId,
  )
}

// `opcoesDeCategoriaPai` e a mesma lista ja filtrada por
// `filtrarOpcoesDeCategoriaPai` (mesmo tipo da categoria em edicao/criacao,
// nivel 0 - sem parentId proprio - e nao arquivada). Validar aqui de novo
// contra essa lista evita que o usuario tente enviar um parentId que o
// backend recusaria (categoria de outro tipo, subcategoria de subcategoria
// ou categoria arquivada), sem duplicar a regra de filtragem em dois
// lugares.
export function validarCategoria(
  nome: string,
  parentId: string,
  opcoesDeCategoriaPai: CategoriaResponse[],
): string | null {
  if (nome.trim().length === 0) {
    return "Informe um nome."
  }

  if (parentId.length === 0) {
    return null
  }

  const parentValido = opcoesDeCategoriaPai.some((categoria) => categoria.id === parentId)
  if (!parentValido) {
    return "Selecione uma categoria-pai valida."
  }

  return null
}
