import { useMemo, useState } from "react"
import { Plus } from "lucide-react"
import { useRecebiveisRecorrentes } from "@/features/recebiveis-recorrentes/hooks/useRecebiveisRecorrentes"
import { GRADE_COLUNAS_RECEBIVEL, RecebivelRecorrenteItem } from "@/features/recebiveis-recorrentes/components/RecebivelRecorrenteItem"
import { FormRecebivelRecorrente } from "@/features/recebiveis-recorrentes/FormRecebivelRecorrente"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { construirMapaCategorias } from "@/features/categorias/lib/construirMapaCategorias"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { FormModal } from "@/shared/ui/FormModal"
import { PageHeader } from "@/shared/ui/page-header"
import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { somarTotalRecorrenteMensal } from "@/features/recebiveis-recorrentes/lib/somarTotalRecorrenteMensal"

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

  const mapaCategorias = useMemo(
    () =>
      construirMapaCategorias([
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

  const totalRecorrenteMensal = somarTotalRecorrenteMensal(recebiveis ?? [])

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <PageHeader
        title="Recebiveis recorrentes"
        action={
          <>
            <Button
              type="button"
              onClick={() => setCriandoRecebivel(true)}
              className="hidden h-auto gap-1.5 rounded-[9px] px-4 py-2.5 text-[13px] md:inline-flex"
            >
              <Plus className="size-3.5" strokeWidth={2} aria-hidden="true" />
              Novo recebível recorrente
            </Button>
            <Button
              type="button"
              onClick={() => setCriandoRecebivel(true)}
              aria-label="Novo recebível recorrente"
              className="size-8 rounded-[9px] p-0 md:hidden"
            >
              <Plus className="size-4" strokeWidth={2} aria-hidden="true" />
            </Button>
          </>
        }
      />

      <div className="rounded-2xl border-[0.5px] border-border bg-card p-5 md:p-[22px]">
        <span className="text-[13px] text-text-muted">Total recorrente mensal</span>
        <div className="mt-1.5 text-[28px] leading-tight font-medium text-text-primary">
          {formatarMoeda(totalRecorrenteMensal)}
        </div>
        <p className="mt-1 text-xs text-text-faint">
          Soma os recebiveis mensais ativos; anuais e semanais nao entram.
        </p>
      </div>

      <FormModal
        open={criandoRecebivel}
        onClose={() => setCriandoRecebivel(false)}
        title="Novo recebível recorrente"
      >
        <FormRecebivelRecorrente
          onSalvar={() => setCriandoRecebivel(false)}
          onCancelar={() => setCriandoRecebivel(false)}
        />
      </FormModal>

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Não foi possível carregar os recebíveis recorrentes</AlertTitle>
          <AlertDescription>Verifique sua conexão e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : recebiveis && recebiveis.length > 0 ? (
        <div className="min-w-0 md:overflow-hidden md:rounded-2xl md:border-[0.5px] md:border-border md:bg-card">
          <div
            className={cn(
              "hidden border-b-[0.5px] border-border px-[22px] py-3.5 text-xs text-text-muted md:grid",
              GRADE_COLUNAS_RECEBIVEL,
            )}
          >
            <span>Descricao</span>
            <span>Recorrencia</span>
            <span className="text-right">Valor</span>
            <span className="text-right">Ativa</span>
            <span />
          </div>
          <ul className="flex flex-col gap-2.5 md:block">
            {recebiveis.map((recebivel) => (
              <RecebivelRecorrenteItem
                key={recebivel.id}
                recebivel={recebivel}
                categoriaNome={
                  recebivel.categoriaId ? (mapaCategorias[recebivel.categoriaId]?.nome ?? null) : null
                }
              />
            ))}
          </ul>
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhum recebível recorrente cadastrado ainda.</p>
      )}
    </div>
  )
}
