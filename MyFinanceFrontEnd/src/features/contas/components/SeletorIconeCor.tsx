import { cn } from "@/shared/lib/utils"
import { ICONE_POR_NOME } from "@/features/contas/lib/obterIconeConta"

// Catalogo fixo de icones, derivado de ICONE_POR_NOME (lib/obterIconeConta.ts),
// a mesma fonte que resolve `conta.icone` no resto da tela.
const CATALOGO_ICONES = Object.entries(ICONE_POR_NOME).map(([nome, Icone]) => ({ nome, Icone }))

// Paleta FIXA e curta (identidade-visual.md: "sem cor so por enfeite" e
// briefing desta tarefa: "JAMAIS um color-picker livre"). `cor` e
// personalizacao da conta escolhida pelo usuario, nao um estado de dominio -
// por isso os hex aqui sao DIFERENTES dos tokens semanticos reservados
// (positivo/negativo/alerta continuam significando entrada/saida/pendente em
// todo o resto do app, ver ContaIcone.tsx e identidade-visual.md; reusar os
// mesmos tons aqui confundiria o significado). O primeiro tom repete o
// accent/roxo da propria identidade, como opcao "neutra" alinhada ao tema.
const PALETA_CORES: { nome: string; hex: string }[] = [
  { nome: "Roxo", hex: "#7F77DD" },
  { nome: "Azul", hex: "#5B8DEF" },
  { nome: "Verde-água", hex: "#4FB89E" },
  { nome: "Âmbar", hex: "#D9A441" },
  { nome: "Coral", hex: "#D97767" },
  { nome: "Rosa", hex: "#C77DB0" },
  { nome: "Cinza-azulado", hex: "#6E7A91" },
]

type SeletorIconeCorProps = {
  /** Prefixo dos ids dos rotulos (ex: id da conta), para ficarem unicos na pagina. */
  idPrefixo: string
  iconeSelecionado: string | null
  corSelecionada: string | null
  onIconeChange: (value: string) => void
  onCorChange: (value: string) => void
}

// Seletor de icone e cor da conta, compartilhado entre "Nova conta" e "Editar
// conta". Apresentacao pura sobre o catalogo e a paleta fixos acima.
export function SeletorIconeCor({
  idPrefixo,
  iconeSelecionado,
  corSelecionada,
  onIconeChange,
  onCorChange,
}: SeletorIconeCorProps) {
  const idRotuloIcone = `${idPrefixo}-icone-label`
  const idRotuloCor = `${idPrefixo}-cor-label`

  return (
    <>
      <div className="flex flex-col gap-1.5">
        <span id={idRotuloIcone} className="flex items-center gap-2 text-[13px] font-medium text-text-muted">
          Ícone
        </span>
        <div className="flex flex-wrap gap-2" role="group" aria-labelledby={idRotuloIcone}>
          {CATALOGO_ICONES.map(({ nome: nomeIcone, Icone }) => {
            const selecionado = iconeSelecionado === nomeIcone
            return (
              <button
                key={nomeIcone}
                type="button"
                aria-pressed={selecionado}
                aria-label={`Ícone ${nomeIcone}`}
                onClick={() => onIconeChange(nomeIcone)}
                className={cn(
                  "flex h-[34px] w-[34px] shrink-0 items-center justify-center rounded-[10px] transition-colors",
                  selecionado
                    ? "bg-accent-deep ring-2 ring-primary"
                    : "bg-accent hover:bg-accent-deep/60",
                )}
              >
                <Icone
                  className={cn("h-4 w-4", selecionado ? "text-accent-soft" : "text-text-muted")}
                  strokeWidth={1.6}
                />
              </button>
            )
          })}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <span id={idRotuloCor} className="flex items-center gap-2 text-[13px] font-medium text-text-muted">
          Cor
        </span>
        <div className="flex flex-wrap gap-2" role="group" aria-labelledby={idRotuloCor}>
          {PALETA_CORES.map((cor) => {
            const selecionada = corSelecionada === cor.hex
            return (
              <button
                key={cor.hex}
                type="button"
                aria-pressed={selecionada}
                aria-label={`Cor ${cor.nome}`}
                onClick={() => onCorChange(cor.hex)}
                style={{ backgroundColor: cor.hex }}
                className={cn(
                  "h-[26px] w-[26px] shrink-0 rounded-full transition-shadow",
                  selecionada && "ring-2 ring-offset-2 ring-offset-card ring-primary",
                )}
              />
            )
          })}
        </div>
      </div>
    </>
  )
}
