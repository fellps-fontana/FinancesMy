import { useMemo, useState } from "react"
import { useAssinaturasCartao } from "@/features/assinaturas-cartao/hooks/useAssinaturasCartao"
import { AssinaturaCartaoItem } from "@/features/assinaturas-cartao/components/AssinaturaCartaoItem"
import { FormAssinaturaCartao } from "@/features/assinaturas-cartao/FormAssinaturaCartao"
import { useCategorias } from "@/features/categorias/hooks/useCategorias"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Modal } from "@/shared/ui/Modal"
import type { CategoriaResponse } from "@/features/categorias/types"

// Mesmo achatamento categoria + subcategorias de ListaContasFixas.tsx
// (features/contas-fixas/ListaContasFixas.tsx) - duplicado aqui em vez de
// promovido a helper compartilhado pelo mesmo motivo: cada feature resolve
// esse lookup de forma isolada hoje, mover para shared/ e uma task a parte.
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

type AssinaturasCartaoSectionProps = {
  contaId: string
}

// Container embutido em ContaCartaoPage (mesmo espirito de FaturasSection,
// features/cartao/components/FaturasSection.tsx): assinatura de cartao so
// existe no contexto de uma conta CARTAO ja selecionada, entao a secao
// recebe contaId pronto em vez de ter select proprio de conta.
export function AssinaturasCartaoSection({ contaId }: AssinaturasCartaoSectionProps) {
  const { data: assinaturas, isLoading, error } = useAssinaturasCartao(contaId)
  const { data: categoriasNaoArquivadas } = useCategorias("Despesa", false)
  const { data: categoriasArquivadas } = useCategorias("Despesa", true)
  const [criandoAssinatura, setCriandoAssinatura] = useState(false)

  const mapaNomeCategoria = useMemo(
    () =>
      construirMapaNomeCategoria([
        ...(categoriasNaoArquivadas ?? []),
        ...(categoriasArquivadas ?? []),
      ]),
    [categoriasNaoArquivadas, categoriasArquivadas],
  )

  if (error) {
    console.error("Falha ao carregar assinaturas do cartao", error)
  }

  return (
    <section className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-[19px] font-medium text-text-primary">Assinaturas</h2>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={() => setCriandoAssinatura((aberto) => !aberto)}
        >
          {criandoAssinatura ? "Cancelar" : "Nova assinatura"}
        </Button>
      </div>

      <Modal
        open={criandoAssinatura}
        onClose={() => setCriandoAssinatura(false)}
        title="Nova assinatura"
      >
        <FormAssinaturaCartao contaId={contaId} onSalvar={() => setCriandoAssinatura(false)} />
      </Modal>

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar as assinaturas</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando assinaturas...</p>
      ) : assinaturas && assinaturas.length > 0 ? (
        <div className="flex flex-col gap-3">
          {assinaturas.map((assinatura) => (
            <AssinaturaCartaoItem
              key={assinatura.id}
              assinatura={assinatura}
              categoriaNome={
                assinatura.categoriaId ? (mapaNomeCategoria[assinatura.categoriaId] ?? null) : null
              }
            />
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhuma assinatura cadastrada ainda.</p>
      )}
    </section>
  )
}
