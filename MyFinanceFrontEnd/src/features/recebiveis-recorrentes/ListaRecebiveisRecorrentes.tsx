import { useMemo, useState } from "react"
import { useRecebiveisRecorrentes } from "@/features/recebiveis-recorrentes/hooks/useRecebiveisRecorrentes"
import { RecebivelRecorrenteItem } from "@/features/recebiveis-recorrentes/components/RecebivelRecorrenteItem"
import { FormRecebivelRecorrente } from "@/features/recebiveis-recorrentes/FormRecebivelRecorrente"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Modal } from "@/shared/ui/Modal"
import type { CategoriaResponse } from "@/features/categorias/types"

// Achata categoria + subcategorias (regra-de-negocio.md item 7) num mapa
// id -> nome. Mesmo helper de ListaContasFixas (duplicado, nao importado,
// porque ARQUIVOS PERMITIDOS restringe a escrita a esta feature) - a
// travessia da arvore vinda de GET /api/categorias e identica.
function construirMapaNomeCategoria(categorias: CategoriaResponse[]): Record<string, string> {
  const mapa: Record<string, string> = {}

  for (const categoria of categorias) {
    mapa[categoria.id] = categoria.nome
    for (const subcategoria of categoria.subcategorias) {
      mapa[subcategoria.id] = subcategoria.nome
    }
  }

  return mapa
}

// Container: le o estado de servidor (React Query) e decide qual estado
// exibir (carregando/erro/vazio/lista). Renderizacao pura de cada item fica
// em RecebivelRecorrenteItem - ver clean-code.md "Organizacao (React)".
// Sem filtro de `ativa`: a tela lista moldes ativos e inativos, distinguindo
// pelo badge de status (item 15).
//
// Categoria: RecebivelRecorrenteResponse so traz `categoriaId`. O nome e
// resolvido aqui via useCategorias("Receita", ...) - recebivel recorrente e
// sempre entrada (item 15), entao so categoria tipo Receita e relevante - e
// repassado pronto para o item, que so exibe. Duas chamadas (arquivada=false
// e arquivada=true) porque um molde antigo pode apontar para categoria ja
// arquivada (item 7) e o nome precisa continuar resolvivel; o backend so
// aceita um booleano por vez.
export function ListaRecebiveisRecorrentes() {
  const { data: recebiveis, isLoading, error } = useRecebiveisRecorrentes()
  const { data: categoriasNaoArquivadas, error: erroCategoriasNaoArquivadas } = useCategorias(
    "Receita",
    false,
  )
  const { data: categoriasArquivadas, error: erroCategoriasArquivadas } = useCategorias(
    "Receita",
    true,
  )
  const [criandoRecebivel, setCriandoRecebivel] = useState(false)

  const mapaNomeCategoria = useMemo(
    () =>
      construirMapaNomeCategoria([
        ...(categoriasNaoArquivadas ?? []),
        ...(categoriasArquivadas ?? []),
      ]),
    [categoriasNaoArquivadas, categoriasArquivadas],
  )

  // Log com contexto antes da mensagem generica (clean-code.md "Tratamento
  // de erro": falha nao pode ser silenciosa).
  if (error) {
    console.error("Falha ao carregar recebiveis recorrentes", error)
  }
  // Falha ao carregar categorias nao bloqueia a lista (o essencial da tela) -
  // so degrada a categoria dos itens afetados para "sem categoria visivel".
  if (erroCategoriasNaoArquivadas) {
    console.error(
      "Falha ao carregar categorias nao arquivadas para resolver nome na lista",
      erroCategoriasNaoArquivadas,
    )
  }
  if (erroCategoriasArquivadas) {
    console.error(
      "Falha ao carregar categorias arquivadas para resolver nome na lista",
      erroCategoriasArquivadas,
    )
  }

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-[19px] font-medium text-text-primary">Recebíveis Recorrentes</h1>
          <p className="text-sm text-text-muted">
            Moldes de entrada que geram contas a receber automaticamente.
          </p>
        </div>
        <Button type="button" onClick={() => setCriandoRecebivel((aberto) => !aberto)}>
          {criandoRecebivel ? "Cancelar" : "Novo recebível recorrente"}
        </Button>
      </header>

      <Modal
        open={criandoRecebivel}
        onClose={() => setCriandoRecebivel(false)}
        title="Novo recebível recorrente"
      >
        <FormRecebivelRecorrente onSalvar={() => setCriandoRecebivel(false)} />
      </Modal>

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Não foi possível carregar os recebíveis recorrentes</AlertTitle>
          <AlertDescription>Verifique sua conexão e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : recebiveis && recebiveis.length > 0 ? (
        <div className="flex flex-col gap-3">
          {recebiveis.map((recebivel) => (
            <RecebivelRecorrenteItem
              key={recebivel.id}
              recebivel={recebivel}
              categoriaNome={
                recebivel.categoriaId
                  ? (mapaNomeCategoria[recebivel.categoriaId] ?? null)
                  : null
              }
            />
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhum recebível recorrente cadastrado ainda.</p>
      )}
    </div>
  )
}
