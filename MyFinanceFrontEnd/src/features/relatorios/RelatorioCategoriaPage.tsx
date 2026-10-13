import { ChevronLeft, ChevronRight } from "lucide-react"
import { useSearchParams } from "react-router-dom"
import { PageHeader } from "@/shared/ui/page-header"
import { Button } from "@/shared/ui/button"
import { useRelatorioGastoPorCategoria } from "@/features/relatorios/hooks/useRelatorioGastoPorCategoria"
import { mesAnterior, proximoMes, rotuloMes, type AnoMes } from "@/features/relatorios/lib/navegarMes"
import { LinhaCategoriaRelatorio } from "@/features/relatorios/components/LinhaCategoriaRelatorio"
import { ResumoTotalGasto } from "@/features/relatorios/components/ResumoTotalGasto"

// Mes/ano vivem na query string (?ano=2026&mes=7); ausente/invalido = mes atual.
export function lerMesDaQuery(params: URLSearchParams): AnoMes {
  const hoje = new Date()
  const ano = Number(params.get("ano"))
  const mes = Number(params.get("mes"))
  const valido = Number.isInteger(ano) && ano > 1900 && Number.isInteger(mes) && mes >= 1 && mes <= 12
  return valido ? { ano, mes } : { ano: hoje.getFullYear(), mes: hoje.getMonth() + 1 }
}

export function RelatorioCategoriaPage() {
  const [params, setParams] = useSearchParams()
  const referencia = lerMesDaQuery(params)
  const { data, isLoading, isError } = useRelatorioGastoPorCategoria(referencia.ano, referencia.mes)

  const irPara = (alvo: AnoMes) => setParams({ ano: String(alvo.ano), mes: String(alvo.mes) })
  const query = `?ano=${referencia.ano}&mes=${referencia.mes}`

  return (
    <div className="flex flex-col gap-5">
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
        <div className="grid gap-5 lg:grid-cols-[1fr_1.3fr] lg:items-start">
          <ResumoTotalGasto totalGasto={data.totalGasto} variacaoPercentual={data.variacaoPercentual} />
          <div className="flex flex-col gap-3 rounded-2xl border-[0.5px] border-border bg-card p-5">
            {data.linhas.map((linha) => (
              <LinhaCategoriaRelatorio
                key={linha.categoriaId ?? "sem-categoria"}
                linha={linha}
                to={`/relatorio/${linha.categoriaId ?? "sem-categoria"}${query}`}
              />
            ))}
          </div>
        </div>
      ) : null}
    </div>
  )
}
