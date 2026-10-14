import type { ReactNode } from "react"
import { ArrowUpRight, Banknote, History, Landmark, Pencil, Plus, Power } from "lucide-react"
import { cn } from "@/shared/lib/utils"
import { BottomSheet } from "@/shared/ui/bottom-sheet"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { ROTULO_TIPO_ATIVO } from "@/features/investimentos/lib/rotularTipoAtivo"
import type { AtivoResponse } from "@/features/investimentos/types"

export type AcaoAtivo = "aporte" | "dividendo" | "valorAtual" | "historico" | "conta" | "desativar"

type AcoesAtivoSheetProps = {
  ativo: AtivoResponse | null
  onAcao: (acao: AcaoAtivo, ativo: AtivoResponse) => void
  onFechar: () => void
}

type LinhaAcaoProps = {
  icone: ReactNode
  rotulo: string
  destrutiva?: boolean
  onClick: () => void
}

function LinhaAcao({ icone, rotulo, destrutiva, onClick }: LinhaAcaoProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex items-center gap-3 rounded-xl bg-muted p-3.5 text-left outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
    >
      <span className={cn("shrink-0", destrutiva ? "text-negativo" : "text-text-muted")}>{icone}</span>
      <span className={cn("text-sm", destrutiva ? "text-negativo" : "text-text-body")}>{rotulo}</span>
    </button>
  )
}

// Folha de acoes do ativo (desktop e mobile): aporte, dividendo, valor atual,
// historico, vincular/trocar conta e desativar.
export function AcoesAtivoSheet({ ativo, onAcao, onFechar }: AcoesAtivoSheetProps) {
  const titulo = ativo?.nome ?? "Acoes"

  return (
    <BottomSheet open={ativo !== null} onClose={onFechar} title={titulo} hideHeader>
      {ativo && (
        <>
          <div className="mx-auto h-1 w-9 rounded-full bg-border md:hidden" aria-hidden="true" />
          <div>
            <h2 className="text-base font-medium text-text-primary">{ativo.nome}</h2>
            <div className="mt-0.5 text-xs text-text-faint">
              {ROTULO_TIPO_ATIVO[ativo.tipo]} · {formatarMoeda(ativo.valorAtual)}
            </div>
          </div>

          <div className="flex flex-col gap-2.5">
            <LinhaAcao
              icone={<Plus className="size-[18px]" aria-hidden="true" />}
              rotulo="Novo aporte"
              onClick={() => onAcao("aporte", ativo)}
            />
            <LinhaAcao
              icone={<Banknote className="size-[18px]" aria-hidden="true" />}
              rotulo="Registrar dividendo"
              onClick={() => onAcao("dividendo", ativo)}
            />
            <LinhaAcao
              icone={<Pencil className="size-[18px]" aria-hidden="true" />}
              rotulo="Editar valor atual"
              onClick={() => onAcao("valorAtual", ativo)}
            />
            <LinhaAcao
              icone={<History className="size-[18px]" aria-hidden="true" />}
              rotulo="Ver historico de aportes"
              onClick={() => onAcao("historico", ativo)}
            />
            <LinhaAcao
              icone={
                ativo.semConta ? (
                  <Landmark className="size-[18px]" aria-hidden="true" />
                ) : (
                  <ArrowUpRight className="size-[18px]" aria-hidden="true" />
                )
              }
              rotulo={ativo.semConta ? "Vincular conta" : "Trocar conta"}
              onClick={() => onAcao("conta", ativo)}
            />
            <LinhaAcao
              icone={<Power className="size-[18px]" aria-hidden="true" />}
              rotulo="Desativar ativo"
              destrutiva
              onClick={() => onAcao("desativar", ativo)}
            />
          </div>
        </>
      )}
    </BottomSheet>
  )
}
