import { Link, useParams, useSearchParams } from "react-router-dom"
import { cn } from "@/shared/lib/utils"
import { ProgressBar } from "@/shared/ui/progress-bar"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { useRelatorioCategoriaDetalhe } from "@/features/relatorios/hooks/useRelatorioCategoriaDetalhe"
import { rotuloMes } from "@/features/relatorios/lib/navegarMes"
import { CHAVE_SEM_CATEGORIA, larguraDoLimite } from "@/features/relatorios/lib/fatiasRelatorio"
import { formatarPercentualRelatorio, limiteEstourado } from "@/features/relatorios/lib/regrasRelatorio"
import {
  GRADE_COLUNAS_LANCAMENTO_RELATORIO,
  LancamentoRelatorioItem,
} from "@/features/relatorios/components/LancamentoRelatorioItem"
import { lerMesDaQuery } from "@/features/relatorios/lib/lerMesDaQuery"

// Detalhe da categoria no mes (mockup s_relcat): breadcrumb, card com o total
// e o limite (quando existe) e tabela de lancamentos. Valores e percentuais
// vem do backend; so a largura das barras e derivada em lib/.
export function RelatorioCategoriaDetalhePage() {
  const { categoriaId } = useParams<{ categoriaId?: string }>()
  const [params] = useSearchParams()
  const referencia = lerMesDaQuery(params)
  // Rota /relatorio/sem-categoria (sem id) => categoriaId null no backend.
  const idBackend = categoriaId && categoriaId !== CHAVE_SEM_CATEGORIA ? categoriaId : null
  const { data, isLoading, isError, error } = useRelatorioCategoriaDetalhe(referencia.ano, referencia.mes, idBackend)

  if (error) {
    console.error("Falha ao carregar detalhe da categoria", error)
  }

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <nav aria-label="Navegacao" className="flex flex-wrap items-center gap-1.5 text-[13px]">
        <Link
          to={`/relatorio?ano=${referencia.ano}&mes=${referencia.mes}`}
          className="text-text-muted outline-none hover:text-text-body focus-visible:ring-2 focus-visible:ring-ring"
        >
          Relatorio por categoria
        </Link>
        <span className="text-text-faint" aria-hidden="true">
          /
        </span>
        <span className="text-text-body">
          {data ? `${data.nome} - ${rotuloMes(referencia)}` : rotuloMes(referencia)}
        </span>
      </nav>

      {isLoading ? <p className="text-[13px] text-text-muted">Carregando...</p> : null}
      {isError ? <p className="text-[13px] text-negativo">Nao foi possivel carregar o detalhe.</p> : null}

      {data ? (
        <>
          <div
            className={cn(
              "grid gap-5 rounded-2xl border-[0.5px] border-border bg-card p-5 md:p-[22px]",
              data.limite && "md:grid-cols-2 md:items-center md:gap-8",
            )}
          >
            <div>
              <span className="text-[13px] text-text-muted">Total na categoria</span>
              <div className="mt-1.5 text-[28px] leading-tight font-medium text-text-primary">
                {formatarMoeda(data.total)}
              </div>
              <div className="mt-1 text-xs text-text-faint">
                {formatarPercentualRelatorio(data.percentualDoTotal)}% do total gasto
              </div>
            </div>
            {data.limite ? (
              <div className="min-w-0">
                <div className="mb-1.5 flex justify-between gap-2 text-xs text-text-muted">
                  <span>Limite de gasto</span>
                  <span className="whitespace-nowrap">
                    {formatarMoeda(data.limite.gastoRealizado)} de {formatarMoeda(data.limite.valorLimite)}
                  </span>
                </div>
                <ProgressBar
                  percent={larguraDoLimite(data.limite.gastoRealizado, data.limite.valorLimite)}
                  exceeded={limiteEstourado(data.limite.gastoRealizado, data.limite.valorLimite)}
                />
              </div>
            ) : null}
          </div>

          {data.lancamentos.length === 0 ? (
            <div className="rounded-2xl border-[0.5px] border-border bg-card p-8 text-center text-sm text-text-muted">
              Nenhum lancamento neste mes.
            </div>
          ) : (
            <div className="min-w-0 overflow-hidden rounded-2xl border-[0.5px] border-border bg-card">
              <div
                className={cn(
                  "hidden border-b-[0.5px] border-border px-[22px] py-3.5 text-xs text-text-muted md:grid",
                  GRADE_COLUNAS_LANCAMENTO_RELATORIO,
                )}
              >
                <span>Descricao</span>
                <span>Data</span>
                <span>Conta</span>
                <span className="text-right">Valor</span>
              </div>
              <ul>
                {data.lancamentos.map((lancamento) => (
                  <LancamentoRelatorioItem key={lancamento.id} lancamento={lancamento} />
                ))}
              </ul>
            </div>
          )}
        </>
      ) : null}
    </div>
  )
}
