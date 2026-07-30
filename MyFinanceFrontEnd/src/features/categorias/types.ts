// Categoria e do usuario, nao do Pierre (regra-de-negocio.md item 7).
// Subcategoria via auto-relacionamento (parentId) e pode ser arquivada
// (arquivada = true), nunca deletada.
//
// Serializacao real do backend (CategoriaResponse.cs + Program.cs, que
// registra JsonStringEnumConverter sem naming policy customizada): o enum
// TipoCategoria (Domain/TipoCategoria.cs) tem membros "Despesa"/"Receita" e
// serializa em PascalCase — NAO em caixa alta ("DESPESA"/"RECEITA"), apesar
// do valor de storage no banco usar caixa alta internamente
// (TipoCategoriaExtensions.ToStorageValue). Quem consumir esse tipo em
// CampoLimiteGasto.tsx (que espera caixa alta) precisa converter — conversao
// fica fora do escopo desta camada de dados.
export type TipoCategoria = "Despesa" | "Receita"

// `icone` (adicionado na TASK-137, backend): id de um icone do catalogo fixo
// (shared/lib/catalogoIcones.ts), nao uma URL/upload livre. Opcional -
// categoria existente sem icone escolhido fica `undefined`, sem quebra
// (mesmo espirito de campo aditivo do resto do dominio, ex: `parentId`).
export type CategoriaResponse = {
  id: string
  nome: string
  tipo: TipoCategoria
  parentId?: string
  subcategorias: CategoriaResponse[]
  arquivada: boolean
  icone?: string
}

export type CriarCategoriaRequest = {
  nome: string
  tipo: TipoCategoria
  parentId?: string
  icone?: string
}

// Tipo e imutavel apos a criacao da categoria (regra-de-negocio.md item 7)
// — por isso EditarCategoriaRequest nao tem campo tipo, so o backend
// (EditarCategoriaRequest.cs) tambem nao aceita.
export type EditarCategoriaRequest = {
  nome: string
  parentId?: string
  icone?: string
}
