import { TriangleAlert } from "lucide-react"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { formatadorPercentual } from "@/features/planejamento-mensal/lib/percentual"
import type {
  ExcessoPercentual,
  PlanoPreview,
} from "@/features/planejamento-mensal/lib/rascunho"

type AlertasPlanoProps = {
  plano: PlanoPreview
  excessosPercentuais?: ExcessoPercentual[]
}

// Alertas do item 20 atualizados em tempo real via preview live.
// Cor: alerta (ambar) = atencao; negativo = plano inviavel / estouro / soma > 100%.
export function AlertasPlano({ plano, excessosPercentuais = [] }: AlertasPlanoProps) {
  const estourados = plano.itens.filter((s) => s.estourado)

  return (
    <>
      {excessosPercentuais.length > 0 && (
        <Alert variant="destructive" className="border-negativo/40 bg-negativo/15">
          <TriangleAlert />
          <AlertTitle>Soma dos percentuais ultrapassa 100%</AlertTitle>
          <AlertDescription>
            <ul className="list-inside list-disc">
              {excessosPercentuais.map((excesso) => (
                <li key={excesso.parentId ?? "primeiro-nivel"}>
                  {excesso.nomePai === null
                    ? `Categorias de 1º nível somam ${formatadorPercentual.format(excesso.soma)}% (máximo 100%).`
                    : `Subcategorias de ${excesso.nomePai} somam ${formatadorPercentual.format(excesso.soma)}% (máximo 100% do pai).`}
                </li>
              ))}
            </ul>
          </AlertDescription>
        </Alert>
      )}
      {plano.planoNaoFecha && (
        <Alert variant="destructive" className="border-negativo/40 bg-negativo/15">
          <TriangleAlert />
          <AlertTitle>Plano não fecha</AlertTitle>
          <AlertDescription>
            As despesas fixas do mês superam a renda prevista. Investimento e sublimites ficam
            zerados.
          </AlertDescription>
        </Alert>
      )}
      {plano.investimentoReduzido && (
        <Alert className="border-alerta/40 bg-alerta/15 text-alerta">
          <TriangleAlert />
          <AlertTitle>
            Investimento reduzido de {formatarMoeda(plano.investimentoPlanejado)} para{" "}
            {formatarMoeda(plano.investimento)}
          </AlertTitle>
          <AlertDescription>
            A renda, descontadas as fixas, não comporta o investimento planejado.
          </AlertDescription>
        </Alert>
      )}
      {estourados.length > 0 && (
        <Alert variant="destructive" className="border-negativo/40 bg-negativo/15">
          <TriangleAlert />
          <AlertTitle>
            {estourados.length === 1
              ? "1 sublimite estourado"
              : `${estourados.length} sublimites estourados`}
          </AlertTitle>
          <AlertDescription>{estourados.map((s) => s.nome).join(", ")}</AlertDescription>
        </Alert>
      )}
    </>
  )
}
