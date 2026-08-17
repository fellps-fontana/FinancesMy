import { useMemo, useState } from "react"
import { useContasFixas } from "@/features/contas-fixas/hooks/useContasFixas"
import { ContaFixaItem } from "@/features/contas-fixas/components/ContaFixaItem"
import { FormContaFixa } from "@/features/contas-fixas/FormContaFixa"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Modal } from "@/shared/ui/Modal"
import type { CategoriaResponse } from "@/features/categorias/types"

// Achata categoria + subcategorias (regra-de-negocio.md item 7) num mapa
// id -> nome, mesmo espirito de achatarCategorias em CategoriaSelect.tsx -
// so que aqui o resultado e um lookup, nao uma lista de opcoes de form.
// Duplicado em vez de importado porque ARQUIVOS PERMITIDOS desta task
// restringe a escrita a ListaContasFixas.tsx e ContaFixaItem.tsx (nao ha
// como promover isso para um lib/ compartilhado sem sair do escopo
// autorizado); ambas as funcoes fazem exatamente a mesma travessia da
// arvore vinda de GET /api/categorias.
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

// Container: le o estado de servidor (React Query, via useContasFixas e
// useCategorias) e decide qual estado exibir (carregando/erro/vazio/lista).
// Renderizacao pura de cada item fica em ContaFixaItem - ver clean-code.md
// "Organizacao (React)". Sem filtro de `ativa`: a tela lista tanto contas
// fixas ativas quanto inativas, distinguindo pelo badge de status (regra-de-
// negocio.md item 6).
//
// Categoria: ContaFixaResponse so tem `categoriaId` (types.ts), nunca o
// nome - resolvido aqui via useCategorias("Despesa") (conta fixa e sempre
// DEBIT, regra-de-negocio.md item 6, entao so categoria tipo Despesa e
// relevante) e repassado pronto para ContaFixaItem, que so exibe (sem fazer
// fetch nem lookup - permanece componente de apresentacao).
//
// Correcao de achado do style (revisao Bloco E / TASK-114): omitir o
// parametro `arquivada` NAO traz categorias arquivadas - CategoriaRepository.
// Listar (backend), quando `arquivada` nao e informado, aplica
// `query.Where(c => !c.Arquivada)` por default (so retorna as NAO
// arquivadas). Uma ContaFixa antiga pode apontar para uma categoria
// ja arquivada (regra-de-negocio.md item 7 - "arquivar nao apaga o vinculo
// existente") e o nome precisa continuar resolvivel mesmo assim - por isso
// duas chamadas, arquivada=false e arquivada=true (o backend so aceita um
// booleano por vez, nunca "os dois estados"), com os resultados mesclados
// no mesmo mapa antes de repassar para ContaFixaItem.
//
// Criacao: FormContaFixa (sem `contaFixaParaEditar`) e exibido dentro do
// Modal compartilhado (shared/ui/Modal.tsx) atras de um toggle - o
// formulario ja e o mesmo usado pra edicao (ver ContaFixaItem), so que em
// modo criar. onSalvar fecha o modal - a lista ja reflete o item novo
// sozinha via invalidacao de cache no proprio hook useCriarContaFixa.
export function ListaContasFixas() {
  const { data: contasFixas, isLoading, error } = useContasFixas()
  const { data: categoriasNaoArquivadas, error: erroCategoriasNaoArquivadas } = useCategorias(
    "Despesa",
    false,
  )
  const { data: categoriasArquivadas, error: erroCategoriasArquivadas } = useCategorias(
    "Despesa",
    true,
  )
  const [criandoContaFixa, setCriandoContaFixa] = useState(false)

  const mapaNomeCategoria = useMemo(
    () =>
      construirMapaNomeCategoria([
        ...(categoriasNaoArquivadas ?? []),
        ...(categoriasArquivadas ?? []),
      ]),
    [categoriasNaoArquivadas, categoriasArquivadas],
  )

  // Log com contexto antes de exibir a mensagem generica ao usuario - ver
  // clean-code.md "Tratamento de erro": falha nao pode ser silenciosa, mesmo
  // quando a UI so mostra um aviso generico.
  if (error) {
    console.error("Falha ao carregar contas fixas", error)
  }
  // Falha ao carregar categorias (qualquer uma das duas chamadas) nao
  // bloqueia a lista de contas fixas (o essencial da tela) - so degrada a
  // categoria dos itens afetados para "sem categoria visivel" (mapa fica
  // incompleto via useMemo acima), por isso so loga, sem Alert dedicado.
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
          <h1 className="text-[19px] font-medium text-text-primary">Contas Fixas</h1>
          <p className="text-sm text-text-muted">Despesas recorrentes geradas automaticamente todo mes.</p>
        </div>
        <Button type="button" onClick={() => setCriandoContaFixa((aberto) => !aberto)}>
          {criandoContaFixa ? "Cancelar" : "Nova conta fixa"}
        </Button>
      </header>

      <Modal
        open={criandoContaFixa}
        onClose={() => setCriandoContaFixa(false)}
        title="Nova conta fixa"
      >
        <FormContaFixa onSalvar={() => setCriandoContaFixa(false)} />
      </Modal>

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar as contas fixas</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : contasFixas && contasFixas.length > 0 ? (
        <div className="flex flex-col gap-3">
          {contasFixas.map((contaFixa) => (
            <ContaFixaItem
              key={contaFixa.id}
              contaFixa={contaFixa}
              categoriaNome={
                contaFixa.categoriaId ? (mapaNomeCategoria[contaFixa.categoriaId] ?? null) : null
              }
            />
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhuma conta fixa cadastrada ainda.</p>
      )}
    </div>
  )
}
