import { ChevronLeft, ChevronRight } from "lucide-react"
import { useSearchParams } from "react-router-dom"
import { PageHeader } from "@/shared/ui/page-header"
import { Button } from "@/shared/ui/button"
import { useRelatorioGastoPorCategoria } from "@/features/relatorios/hooks/useRelatorioGastoPorCategoria"
import { mesAnterior, proximoMes, rotuloMes, type AnoMes } from "@/features/relatorios/lib/navegarMes"
import { lerMesDaQuery } from "@/features/relatorios/lib/lerMesDaQuery"
import { LinhaCategoriaRelatorio } from "@/features/relatorios/components/LinhaCategoriaRelatorio"
import { DonutGastoPorCategoria } from "@/features/relatorios/components/DonutGastoPorCategoria"
import { CHAVE_SEM_CATEGORIA, calcularFatiasDonut, chaveDaLinha, corDaFatia } from "@/features/relatorios/lib/fatiasRelatorio"

export function RelatorioCategoriaPage() {
  const [params, setParams] = useSearchParams()
  const referencia = lerMesDaQuery(params)
  const { data, isLoading, isError, error } = useRelatorioGastoPorCategoria(referencia.ano, referencia.mes)

  if (error) {
    console.error("Falha ao carregar relatorio por categoria", error)
  }

  const irPara = (alvo: AnoMes) => setParams({ ano: String(alvo.ano), mes: String(alvo.mes) })
  const query = `?ano=${referencia.ano}&mes=${referencia.mes}`

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <PageHeader
        title="Relatorio por categoria"
        action={
          <div className="flex items-center gap-1 rounded-[9px] border-[0.5px] border-border bg-card px-1 py-0.5">
            <Button variant="ghost" size="icon-sm" aria-label="Mes anterior" onClick={() => irPara(mesAnterior(referencia))}>
              <ChevronLeft />
            </Button>
            <span className="min-w-28 text-center text-[13px] text-foreground">{rotuloMes(referencia)}</span>
            <Button variant="ghost" size="icon-sm" aria-label="Proximo mes" onClick={() => irPara(proximoMes(referencia))}>
              <ChevronRight />
            </Button>
          </div>
        }
      />

      {isLoading ? <p className="text-[13px] text-muted-foreground">Carregando...</p> : null}
      {isError ? <p className="text-[13px] text-negativo">Nao foi possivel carregar o relatorio.</p> : null}

      {data && data.linhas.length === 0 ? (
        <div className="rounded-2xl border-[0.5px] border-border bg-card p-8 text-center text-[14px] text-muted-foreground">
          Nenhum gasto neste mes.
        </div>
      ) : null}

      {data && data.linhas.length > 0 ? (
        <div className="grid gap-4 lg:grid-cols-[1fr_1.3fr] lg:items-start lg:gap-6">
          <DonutGastoPorCategoria
            fatias={calcularFatiasDonut(data.linhas)}
            totalGasto={data.totalGasto}
            variacaoPercentual={data.variacaoPercentual}
          />
          <div className="flex min-w-0 flex-col gap-4 rounded-2xl border-[0.5px] border-border bg-card p-[22px]">
            {data.linhas.map((linha, indice) => (
              <LinhaCategoriaRelatorio
                key={chaveDaLinha(linha)}
                linha={linha}
                cor={corDaFatia(indice)}
                to={`/relatorio/${linha.categoriaId ?? CHAVE_SEM_CATEGORIA}${query}`}
              />
            ))}
          </div>
        </div>
      ) : null}
    </div>
  )
}
