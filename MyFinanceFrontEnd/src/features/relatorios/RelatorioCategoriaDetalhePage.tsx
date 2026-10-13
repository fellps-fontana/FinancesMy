import { ChevronLeft } from "lucide-react"
import { Link, useParams, useSearchParams } from "react-router-dom"
import { IconTile } from "@/shared/ui/icon-tile"
import { ProgressBar } from "@/shared/ui/progress-bar"
import { obterIconeCatalogo } from "@/shared/lib/catalogoIcones"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { useRelatorioCategoriaDetalhe } from "@/features/relatorios/hooks/useRelatorioCategoriaDetalhe"
import { rotuloMes } from "@/features/relatorios/lib/navegarMes"
import { LancamentoRelatorioItem } from "@/features/relatorios/components/LancamentoRelatorioItem"
import { lerMesDaQuery } from "@/features/relatorios/RelatorioCategoriaPage"

const formatadorPercentual = new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 1 })

export function RelatorioCategoriaDetalhePage() {
  const { categoriaId } = useParams<{ categoriaId?: string }>()
  const [params] = useSearchParams()
  const referencia = lerMesDaQuery(params)
  // Rota /relatorio/sem-categoria (sem id) => categoriaId null no backend.
  const idBackend = categoriaId && categoriaId !== "sem-categoria" ? categoriaId : null
  const { data, isLoading, isError } = useRelatorioCategoriaDetalhe(referencia.ano, referencia.mes, idBackend)
  const Icone = obterIconeCatalogo(data?.icone)

  return (
    <div className="flex flex-col gap-4">
      <Link
        to={`/relatorio?ano=${referencia.ano}&mes=${referencia.mes}`}
        className="flex items-center gap-1 self-start text-[13px] text-muted-foreground hover:text-foreground"
      >
        <ChevronLeft className="size-4" aria-hidden="true" />
        Relatorio por categoria
      </Link>

      {isLoading ? <p className="text-[13px] text-muted-foreground">Carregando...</p> : null}
      {isError ? <p className="text-[13px] text-negativo">Nao foi possivel carregar o detalhe.</p> : null}

      {data ? (
        <>
          <div className="flex items-center gap-3">
            <IconTile>
              <Icone strokeWidth={1.6} aria-hidden="true" />
            </IconTile>
            <h1 className="text-[19px] font-medium text-text-primary">
              {data.nome} - {rotuloMes(referencia)}
            </h1>
          </div>

          <div className="flex flex-col gap-3.5 rounded-2xl border-[0.5px] border-border bg-card p-[18px]">
            <div>
              <div className="text-[28px] leading-tight font-medium text-text-primary">{formatarMoeda(data.total)}</div>
              <div className="mt-1 text-[12px] text-muted-foreground">
                {formatadorPercentual.format(data.percentualDoTotal)}% do total gasto
              </div>
            </div>
            {data.limite ? (
              <div>
                <div className="mb-1.5 flex justify-between text-[12px] text-muted-foreground">
                  <span>Limite de gasto</span>
                  <span>
                    {formatarMoeda(data.limite.gastoRealizado)} de {formatarMoeda(data.limite.valorLimite)}
                  </span>
                </div>
                <ProgressBar
                  percent={data.limite.valorLimite > 0 ? (data.limite.gastoRealizado / data.limite.valorLimite) * 100 : 0}
                  exceeded={data.limite.gastoRealizado > data.limite.valorLimite}
                />
              </div>
            ) : null}
          </div>

          {data.lancamentos.length === 0 ? (
            <div className="rounded-2xl border-[0.5px] border-border bg-card p-8 text-center text-[14px] text-muted-foreground">
              Nenhum lancamento neste mes.
            </div>
          ) : (
            <>
              <span className="text-[13px] text-muted-foreground">
                {data.lancamentos.length} {data.lancamentos.length === 1 ? "lancamento" : "lancamentos"}
              </span>
              <ul className="flex flex-col rounded-[14px] border-[0.5px] border-border bg-card">
                {data.lancamentos.map((lancamento) => (
                  <LancamentoRelatorioItem key={lancamento.id} lancamento={lancamento} />
                ))}
              </ul>
            </>
          )}
        </>
      ) : null}
    </div>
  )
}
