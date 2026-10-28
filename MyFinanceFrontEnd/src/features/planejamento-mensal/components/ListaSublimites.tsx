import { useState } from "react"
import { ChevronDown, ChevronRight, RotateCcw, Trash2 } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { formatarMoeda } from "@/shared/lib/formatarMoeda"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import {
  classificarEstadoLimite,
  larguraBarraUso,
  rotuloUso,
  type EstadoLimite,
} from "@/features/limite-gasto/lib/estadoLimite"
import { formatadorPercentual } from "@/features/planejamento-mensal/lib/percentual"
import { agruparPorPai } from "@/features/planejamento-mensal/lib/agruparPorPai"
import {
  excedeCem,
  somaPercentuais,
  type ItemSublimitePreview,
  type PlanoPreview,
} from "@/features/planejamento-mensal/lib/rascunho"

const CONFIG_ESTADO: Record<
  EstadoLimite,
  { dot: string; barra: string; texto: string; badge: string; rotulo: string }
> = {
  estourado: {
    dot: "bg-negativo",
    barra: "bg-negativo",
    texto: "text-negativo",
    badge: "bg-negativo/15 text-negativo",
    rotulo: "Estourado",
  },
  perto: {
    dot: "bg-alerta",
    barra: "bg-alerta",
    texto: "text-alerta",
    badge: "bg-alerta/15 text-alerta",
    rotulo: "Perto do limite",
  },
  ok: {
    dot: "bg-positivo",
    barra: "bg-positivo",
    texto: "text-positivo",
    badge: "bg-positivo/15 text-positivo",
    rotulo: "Dentro do limite",
  },
}

function Selo({ texto, classes }: { texto: string; classes: string }) {
  return (
    <span
      className={cn(
        "inline-flex shrink-0 items-center rounded-[5px] px-2 py-0.5 text-[12px] font-medium",
        classes,
      )}
    >
      {texto}
    </span>
  )
}

type LinhaSublimiteItemProps = {
  item: ItemSublimitePreview
  filho: boolean
  paiTemSublimite: boolean
  somenteLeitura: boolean
  destacado: boolean
  quantidadeSubcategorias: number
  expandido: boolean
  onAlternarExpansao?: () => void
  onAlterarPercentual: (categoriaId: string, valor: string) => void
  onAlterarValor: (categoriaId: string, valor: string) => void
  onRemover: (categoriaId: string) => void
}

function LinhaSublimiteItem({
  item,
  filho,
  paiTemSublimite,
  somenteLeitura,
  destacado,
  quantidadeSubcategorias,
  expandido,
  onAlternarExpansao,
  onAlterarPercentual,
  onAlterarValor,
  onRemover,
}: LinhaSublimiteItemProps) {
  const estado = classificarEstadoLimite(item.fracaoUtilizada, item.estourado)
  const config = CONFIG_ESTADO[estado]
  const bloqueadoPorPai = filho && !paiTemSublimite && !item.categoriaArquivada
  const larguraBarra = larguraBarraUso(item.fracaoUtilizada, item.estourado)

  return (
    <li
      id={`categoria-${item.categoriaId}`}
      className={cn(
        "flex flex-col gap-2.5 px-4 py-3.5 transition-colors",
        filho && "bg-bg-surface-alt/25 pl-8",
        destacado && "bg-accent-deep/25 ring-1 ring-inset ring-accent",
      )}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex min-w-0 flex-1 items-center gap-2">
          <span
            className={cn(
              "h-2.5 w-2.5 shrink-0 rounded-[3px]",
              item.temSublimite ? config.dot : "bg-muted-foreground/30",
            )}
            aria-hidden="true"
          />
          <span className="truncate text-sm font-medium text-text-body">{item.nome}</span>
          {item.categoriaArquivada && (
            <Selo texto="Arquivada" classes="bg-alerta/15 text-alerta" />
          )}
          {item.temSublimite ? (
            <Selo texto={config.rotulo} classes={config.badge} />
          ) : (
            <Selo texto="Sem sublimite" classes="bg-muted text-text-faint" />
          )}
          {!filho && quantidadeSubcategorias > 0 && onAlternarExpansao && (
            <Button
              type="button"
              variant="ghost"
              size="xs"
              onClick={onAlternarExpansao}
              className="gap-1 text-[12px] text-text-muted"
              aria-expanded={expandido}
            >
              {expandido ? <ChevronDown className="size-3.5" /> : <ChevronRight className="size-3.5" />}
              Subcategorias ({quantidadeSubcategorias})
            </Button>
          )}
        </div>

        {somenteLeitura ? (
          <div className="flex items-center gap-2 text-[13px] text-text-muted">
            {item.temSublimite ? (
              <>
                <span>
                  {formatadorPercentual.format(item.percentual)}%{filho ? " do pai" : ""}
                </span>
                <span>·</span>
                <span className="font-medium text-text-primary">{formatarMoeda(item.valor)}</span>
              </>
            ) : (
              <span className="text-text-faint">0% · {formatarMoeda(0)}</span>
            )}
          </div>
        ) : item.categoriaArquivada ? (
          <div className="flex items-center gap-2">
            <span className="text-[13px] text-text-muted">
              {formatadorPercentual.format(item.percentual)}% · {formatarMoeda(item.valor)}
            </span>
            <Button
              type="button"
              variant="outline"
              size="xs"
              onClick={() => onRemover(item.categoriaId)}
              aria-label={`Remover sublimite de ${item.nome}`}
              className="text-negativo hover:text-negativo"
            >
              <Trash2 className="size-3.5" />
              Remover
            </Button>
          </div>
        ) : (
          <div className="flex flex-wrap items-center gap-2">
            <div className="flex items-center gap-1">
              <Input
                inputMode="decimal"
                value={item.percentualTexto}
                disabled={bloqueadoPorPai}
                onChange={(e) => onAlterarPercentual(item.categoriaId, e.target.value)}
                aria-label={`Percentual de ${item.nome}`}
                className="h-8 w-20 text-right text-sm"
              />
              <span className="text-[12px] text-text-muted">{filho ? "% do pai" : "%"}</span>
            </div>
            <div className="flex items-center gap-1">
              <span className="text-[12px] text-text-muted">R$</span>
              <Input
                inputMode="decimal"
                value={item.valorTexto}
                disabled={bloqueadoPorPai}
                onChange={(e) => onAlterarValor(item.categoriaId, e.target.value)}
                aria-label={`Valor em reais de ${item.nome}`}
                className="h-8 w-24 text-right text-sm"
              />
            </div>
            {item.percentual > 0 && (
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                onClick={() => onRemover(item.categoriaId)}
                title="Zerar sublimite"
                aria-label={`Zerar sublimite de ${item.nome}`}
              >
                <RotateCcw className="size-3.5" />
              </Button>
            )}
          </div>
        )}
      </div>

      {bloqueadoPorPai && (
        <p className="text-[12px] text-text-faint">
          Defina um sublimite maior que 0% em {filho ? "sua categoria-pai" : item.nome} para
          habilitar as subcategorias.
        </p>
      )}

      {item.categoriaArquivada && !somenteLeitura && (
        <p className="w-fit rounded-[5px] bg-alerta/15 px-2 py-0.5 text-[12px] text-alerta">
          Categoria arquivada: não pode ser reenviada ao salvar. Remova este sublimite antes de
          salvar o plano.
        </p>
      )}

      {item.temSublimite ? (
        <div className="flex flex-col gap-1.5">
          <div
            className="h-1.5 w-full overflow-hidden rounded-full bg-muted"
            role="progressbar"
            aria-label={`Uso do sublimite de ${item.nome}`}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={Math.round(larguraBarra)}
          >
            <div
              className={cn("h-full rounded-full transition-all", config.barra)}
              style={{ width: `${larguraBarra}%` }}
            />
          </div>
          <div className="flex items-center justify-between text-[12px] text-text-faint">
            <span>
              Gasto variável: {formatarMoeda(item.gastoRealizado)} de {formatarMoeda(item.valor)}
            </span>
            <span className={cn("font-medium", config.texto)}>
              {rotuloUso(item.fracaoUtilizada, item.estourado)}
            </span>
          </div>
        </div>
      ) : (
        <div className="flex items-center justify-between text-[12px] text-text-faint">
          <span>Gasto variável no mês: {formatarMoeda(item.gastoRealizado)}</span>
          <span>Sem sublimite definido</span>
        </div>
      )}
    </li>
  )
}

function RodapeSomaPercentuais({ rotulo, soma }: { rotulo: string; soma: number }) {
  const excedeu = excedeCem(soma)
  return (
    <div className="flex items-center justify-between bg-bg-surface-alt/20 px-4 py-2 text-[12px]">
      <span className="text-text-muted">{rotulo}</span>
      <span
        className={cn(
          "rounded-[5px] px-2 py-0.5 font-medium",
          excedeu ? "bg-negativo/15 text-negativo" : "text-text-faint",
        )}
      >
        {formatadorPercentual.format(soma)}% de 100%{excedeu ? " · acima de 100%" : ""}
      </span>
    </div>
  )
}

function CartaoApoio({
  rotulo,
  valor,
  detalhe,
}: {
  rotulo: string
  valor: string
  detalhe: string
}) {
  return (
    <div className="flex flex-col gap-0.5 rounded-[12px] border-[0.5px] border-border bg-card p-3.5">
      <span className="text-[13px] text-text-muted">{rotulo}</span>
      <span className="text-sm font-medium text-text-primary">{valor}</span>
      <span className="text-[12px] text-text-faint">{detalhe}</span>
    </div>
  )
}

type ListaSublimitesProps = {
  plano: PlanoPreview
  categoriaIdDestaque: string | null
  onAlterarPercentual: (categoriaId: string, valor: string) => void
  onAlterarValor: (categoriaId: string, valor: string) => void
  onRemover: (categoriaId: string) => void
}

export function ListaSublimites({
  plano,
  categoriaIdDestaque,
  onAlterarPercentual,
  onAlterarValor,
  onRemover,
}: ListaSublimitesProps) {
  const grupos = agruparPorPai(plano.itens)
  const [paisExpandidos, setPaisExpandidos] = useState<Record<string, boolean>>({})

  function estaExpandido(paiId: string, filhos: ItemSublimitePreview[]): boolean {
    if (categoriaIdDestaque && filhos.some((f) => f.categoriaId === categoriaIdDestaque)) {
      return paisExpandidos[paiId] ?? true
    }
    if (paisExpandidos[paiId] !== undefined) {
      return paisExpandidos[paiId]
    }
    return filhos.some((f) => f.temSublimite || f.percentual > 0)
  }

  function alternarPai(paiId: string, filhos: ItemSublimitePreview[]) {
    const atual = estaExpandido(paiId, filhos)
    setPaisExpandidos((prev) => ({ ...prev, [paiId]: !atual }))
  }

  const somaPrimeiroNivel = somaPercentuais(plano.itens, null)

  return (
    <section className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-sm font-medium text-text-primary">
          Sublimites e gasto variável por categoria
        </h2>
        <span className="text-[12px] text-text-faint">
          {formatadorPercentual.format(somaPrimeiroNivel)}% da sobra alocado
        </span>
      </div>

      {grupos.length === 0 ? (
        <p className="text-sm text-text-muted">Nenhuma categoria de despesa disponível neste mês.</p>
      ) : (
        <div className="divide-y-[0.5px] divide-border overflow-hidden rounded-[14px] border-[0.5px] border-border bg-card">
          {grupos.map(({ pai, filhos }) => {
            const expandido = estaExpandido(pai.categoriaId, filhos)
            const somaFilhos = somaPercentuais(plano.itens, pai.categoriaId)

            return (
              <ul key={pai.categoriaId} className="divide-y-[0.5px] divide-border">
                <LinhaSublimiteItem
                  item={pai}
                  filho={false}
                  paiTemSublimite={pai.temSublimite}
                  somenteLeitura={plano.somenteLeitura}
                  destacado={pai.categoriaId === categoriaIdDestaque}
                  quantidadeSubcategorias={filhos.length}
                  expandido={expandido}
                  onAlternarExpansao={
                    filhos.length > 0 ? () => alternarPai(pai.categoriaId, filhos) : undefined
                  }
                  onAlterarPercentual={onAlterarPercentual}
                  onAlterarValor={onAlterarValor}
                  onRemover={onRemover}
                />
                {expandido &&
                  filhos.map((filho) => (
                    <LinhaSublimiteItem
                      key={filho.categoriaId}
                      item={filho}
                      filho
                      paiTemSublimite={pai.temSublimite}
                      somenteLeitura={plano.somenteLeitura}
                      destacado={filho.categoriaId === categoriaIdDestaque}
                      quantidadeSubcategorias={0}
                      expandido={false}
                      onAlterarPercentual={onAlterarPercentual}
                      onAlterarValor={onAlterarValor}
                      onRemover={onRemover}
                    />
                  ))}
                {expandido && filhos.length > 0 && (
                  <li>
                    <RodapeSomaPercentuais
                      rotulo={`Subcategorias de ${pai.nome}`}
                      soma={somaFilhos}
                    />
                  </li>
                )}
              </ul>
            )
          })}
          <RodapeSomaPercentuais
            rotulo="Total distribuído no 1º nível"
            soma={somaPrimeiroNivel}
          />
        </div>
      )}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <CartaoApoio
          rotulo="Não alocado"
          valor={formatarMoeda(plano.naoAlocado)}
          detalhe="Sobra ainda sem sublimite no 1º nível"
        />
        <CartaoApoio
          rotulo="Gasto sem sublimite"
          valor={formatarMoeda(plano.gastoSemSublimite)}
          detalhe="Gasto variável em categorias com 0% ou sem categoria"
        />
      </div>
    </section>
  )
}
