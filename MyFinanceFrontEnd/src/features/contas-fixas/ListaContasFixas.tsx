import { useMemo, useState } from "react"
import { Plus } from "lucide-react"
import { useContasFixas } from "@/features/contas-fixas/hooks/useContasFixas"
import { ContaFixaItem, GRADE_COLUNAS_CONTA_FIXA } from "@/features/contas-fixas/components/ContaFixaItem"
import { FormContaFixa } from "@/features/contas-fixas/FormContaFixa"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { construirMapaCategorias } from "@/features/categorias/lib/construirMapaCategorias"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { FormModal } from "@/shared/ui/FormModal"
import { Button } from "@/shared/ui/button"
import { Card, CardContent } from "@/shared/ui/card"
import { PageHeader } from "@/shared/ui/page-header"
import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { somarTotalFixoMensal } from "@/features/contas-fixas/lib/somarTotalFixoMensal"

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
// Duas chamadas (arquivada=false e arquivada=true; o backend so aceita um
// booleano por vez): uma ContaFixa antiga pode apontar para categoria ja
// arquivada (item 7 - "arquivar nao apaga o vinculo") e o nome precisa
// continuar resolvivel. Os resultados sao mesclados no mesmo mapa.
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

  const mapaCategorias = useMemo(
    () =>
      construirMapaCategorias([
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

  const totalFixoMensal = somarTotalFixoMensal(contasFixas ?? [])

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <PageHeader
        title="Contas fixas"
        action={
          <>
            <Button
              type="button"
              onClick={() => setCriandoContaFixa(true)}
              className="hidden h-auto gap-1.5 rounded-[9px] px-4 py-2.5 text-[13px] md:inline-flex"
            >
              <Plus className="size-3.5" strokeWidth={2} aria-hidden="true" />
              Nova conta fixa
            </Button>
            <Button
              type="button"
              onClick={() => setCriandoContaFixa(true)}
              aria-label="Nova conta fixa"
              className="size-8 rounded-[9px] p-0 md:hidden"
            >
              <Plus className="size-4" strokeWidth={2} aria-hidden="true" />
            </Button>
          </>
        }
      />

      <Card>
        <CardContent className="flex flex-col gap-1.5">
          <span className="text-[13px] text-text-muted">Total fixo mensal</span>
          <span className="text-[28px] leading-tight font-medium text-text-primary">
            {formatarMoeda(totalFixoMensal)}
          </span>
        </CardContent>
      </Card>

      <FormModal
        open={criandoContaFixa}
        onClose={() => setCriandoContaFixa(false)}
        title="Nova conta fixa"
      >
        <FormContaFixa
          onSalvar={() => setCriandoContaFixa(false)}
          onCancelar={() => setCriandoContaFixa(false)}
        />
      </FormModal>

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar as contas fixas</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : contasFixas && contasFixas.length > 0 ? (
        <div className="md:overflow-hidden md:rounded-2xl md:border-[0.5px] md:border-border md:bg-card">
          <div
            className={cn(
              "hidden border-b-[0.5px] border-border px-[22px] py-3.5 text-xs text-text-muted md:grid",
              GRADE_COLUNAS_CONTA_FIXA,
            )}
          >
            <span>Descricao</span>
            <span>Recorrencia</span>
            <span className="text-right">Valor</span>
            <span className="text-right">Ativa</span>
            <span />
          </div>
          <ul className="flex flex-col gap-2.5 md:block">
            {contasFixas.map((contaFixa) => (
              <ContaFixaItem
                key={contaFixa.id}
                contaFixa={contaFixa}
                categoriaNome={
                  contaFixa.categoriaId ? (mapaCategorias[contaFixa.categoriaId]?.nome ?? null) : null
                }
              />
            ))}
          </ul>
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhuma conta fixa cadastrada ainda.</p>
      )}
    </div>
  )
}
