import { ChevronsRight } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { Badge } from "@/shared/ui/badge"
import { Button } from "@/shared/ui/button"
import { IconTile } from "@/shared/ui/icon-tile"
import { obterIconeCatalogo } from "@/shared/lib/catalogoIcones"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { formatarDiaMes } from "@/shared/lib/datas"
import type { CompraResponse } from "@/features/cartao/types"

// Grade da tabela desktop (mockup s_cartao: Descricao / Data / Valor / acao).
// Exportada para o cabecalho da lista usar as mesmas colunas.
export const GRADE_COLUNAS_COMPRA = "md:grid-cols-[minmax(0,2fr)_minmax(110px,1fr)_minmax(110px,1fr)_44px]"

type CompraFaturaItemProps = {
  compra: CompraResponse
  /** Nome do icone do catalogo da categoria (null = icone padrao). */
  iconeCategoria: string | null
  /** Desktop: botao de acao da linha. Mobile: a linha abre o menu de acoes. */
  onAntecipar: (compra: CompraResponse) => void
  onAbrirAcoes: (compra: CompraResponse) => void
}

// Apresentacao de uma compra da fatura. Badge "n/N" e "antecipada" carregam o
// estado de dominio da parcela (regra item 12, Parcelamento/Antecipacao); a
// acao Antecipar so existe em compra parcelada (compraParceladaId presente).
export function CompraFaturaItem({
  compra,
  iconeCategoria,
  onAntecipar,
  onAbrirAcoes,
}: CompraFaturaItemProps) {
  const Icone = obterIconeCatalogo(iconeCategoria ?? undefined)
  const descricao = compra.descricao ?? "Sem descricao"
  const ehParcelada = compra.compraParceladaId !== null
  const ehEntrada = compra.tipo === "CREDIT"

  return (
    <li className="relative py-1.5 md:border-b-[0.5px] md:border-border md:px-[22px] md:py-3.5 md:last:border-b-0">
      <div className={cn("flex items-center gap-3 md:grid", GRADE_COLUNAS_COMPRA)}>
        <div className="flex min-w-0 flex-1 items-center gap-3">
          <IconTile>
            <Icone strokeWidth={1.6} className="size-4" aria-hidden="true" />
          </IconTile>
          <div className="flex min-w-0 flex-col">
            <div className="flex flex-wrap items-center gap-x-1.5 gap-y-1">
              <span className="truncate text-sm text-text-body">{descricao}</span>
              {compra.parcelaNumero !== null && compra.totalParcelas !== null && (
                <Badge variant="parcela">
                  {compra.parcelaNumero}/{compra.totalParcelas}
                </Badge>
              )}
              {compra.antecipada && <Badge variant="antecipada">antecipada</Badge>}
            </div>
            <span className="text-xs whitespace-nowrap text-text-faint md:hidden">{formatarDiaMes(compra.data)}</span>
          </div>
        </div>

        <span className="hidden text-[13px] whitespace-nowrap text-text-muted md:block">{formatarDiaMes(compra.data)}</span>

        <span className={cn("text-sm whitespace-nowrap md:text-right", ehEntrada ? "text-positivo" : "text-text-primary")}>
          {ehEntrada && "+"}
          {formatarMoeda(compra.valor)}
        </span>

        <div className="relative z-10 hidden items-center justify-end md:flex">
          {ehParcelada && (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              aria-label={`Antecipar parcelas de ${descricao}`}
              title="Antecipar parcelas"
              onClick={() => onAntecipar(compra)}
            >
              <ChevronsRight className="size-4 text-text-muted" aria-hidden="true" />
            </Button>
          )}
        </div>
      </div>

      {ehParcelada && (
        <button
          type="button"
          aria-label={`Acoes de ${descricao}`}
          onClick={() => onAbrirAcoes(compra)}
          className="absolute inset-0 md:hidden"
        />
      )}
    </li>
  )
}
