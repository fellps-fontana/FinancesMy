import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { formatarPercentual } from "@/features/investimentos/lib/formatarPercentual"
import type { CarteiraDonut as CarteiraDonutDados } from "@/features/investimentos/lib/calcularFatiasCarteira"

// Tons do acento roxo por fatia (ciclam se houver mais ativos que tons):
// distinguem o ativo, nao carregam estado. Traco e legenda usam o mesmo indice.
const TRACO_POR_INDICE = ["stroke-primary", "stroke-primary/75", "stroke-primary/55", "stroke-primary/35"]
const FUNDO_POR_INDICE = ["bg-primary", "bg-primary/75", "bg-primary/55", "bg-primary/35"]

// Circulo de raio 15,915 tem circunferencia 100: os valores de percentual
// (0-100) viram comprimento de traco direto.
const RAIO = 15.915

type CarteiraDonutProps = {
  carteira: CarteiraDonutDados
  className?: string
}

// Apresentacao pura do bloco "Carteira" (mockup s_investimentos): donut SVG
// com o total no centro e legenda ativo / percentual.
export function CarteiraDonut({ carteira, className }: CarteiraDonutProps) {
  return (
    <div
      className={`flex flex-col items-center justify-center gap-3.5 rounded-2xl border-[0.5px] border-border bg-card p-6 ${className ?? ""}`}
    >
      <div className="relative size-[186px] shrink-0">
        <svg viewBox="0 0 42 42" className="size-full -rotate-90" role="img" aria-label="Distribuicao da carteira">
          <circle cx="21" cy="21" r={RAIO} fill="none" strokeWidth="6" className="stroke-accent" />
          {carteira.fatias.map((fatia, indice) => (
            <circle
              key={fatia.ativoId}
              cx="21"
              cy="21"
              r={RAIO}
              fill="none"
              strokeWidth="6"
              className={TRACO_POR_INDICE[indice % TRACO_POR_INDICE.length]}
              strokeDasharray={`${fatia.percentual} ${100 - fatia.percentual}`}
              strokeDashoffset={-fatia.inicio}
            />
          ))}
        </svg>
        <div className="absolute inset-0 m-auto flex size-[118px] flex-col items-center justify-center rounded-full bg-card">
          <span className="text-xs text-text-muted">Carteira</span>
          <span className="mt-0.5 text-base font-medium whitespace-nowrap text-text-primary">
            {formatarMoeda(carteira.total)}
          </span>
        </div>
      </div>

      {carteira.fatias.length === 0 ? (
        <p className="text-[13px] text-text-muted">Nenhum ativo na carteira.</p>
      ) : (
        <ul className="flex w-full flex-col gap-2">
          {carteira.fatias.map((fatia, indice) => (
            <li key={fatia.ativoId} className="flex items-center gap-2">
              <span
                className={`size-[9px] shrink-0 rounded-[3px] ${FUNDO_POR_INDICE[indice % FUNDO_POR_INDICE.length]}`}
                aria-hidden="true"
              />
              <span className="min-w-0 flex-1 truncate text-[13px] text-text-body">{fatia.nome}</span>
              <span className="text-xs text-text-muted">{formatarPercentual(fatia.percentual)}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
