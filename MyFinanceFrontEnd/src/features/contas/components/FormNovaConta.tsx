import type { FormEvent } from "react"
import { X } from "lucide-react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { cn } from "@/shared/lib/utils"
import { aplicarMascaraMoeda } from "@/features/contas/lib/mascaraMoeda"
import { ICONE_POR_NOME } from "@/features/contas/lib/obterIconeConta"
import type { TipoContaFormulario } from "@/features/contas/types"

type FormNovaContaProps = {
  nome: string
  tipoSelecionado: TipoContaFormulario
  saldoInicialMascarado: string
  iconeSelecionado: string | null
  corSelecionada: string | null
  isSubmitting: boolean
  errorMessage: string | null
  onNomeChange: (value: string) => void
  onTipoChange: (value: TipoContaFormulario) => void
  onSaldoInicialChange: (valorMascarado: string) => void
  onIconeChange: (value: string) => void
  onCorChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onFechar: () => void
}

type OpcaoTipoConta = {
  valor: TipoContaFormulario
  label: string
}

// So 4 opcoes visiveis (regra-de-negocio.md item 10: subtipo so existe para
// Conta tipo Banco). Cartao fica fora - tem fluxo proprio em /cartao. O
// mapeamento de cada opcao pro par (tipo, subtipo) enviado ao back mora em
// lib/obterTipoESubtipoConta.ts, nao aqui.
const OPCOES_TIPO_CONTA: OpcaoTipoConta[] = [
  { valor: "CONTA_CORRENTE", label: "Conta corrente" },
  { valor: "POUPANCA", label: "Poupança" },
  { valor: "DINHEIRO_FISICO", label: "Dinheiro físico" },
  { valor: "INVESTIMENTO", label: "Investimento" },
]

// Catalogo FIXO de icones: DERIVADO de ICONE_POR_NOME (lib/obterIconeConta.ts),
// a mesma fonte que resolve `conta.icone` no resto da tela - correcao do
// style (Bloco G/Contas) elimina a segunda lista nome->icone que vivia
// duplicada aqui. Nao existe input de texto livre nem upload, so este
// conjunto curado, mesmo espirito da paleta de cor abaixo.
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

// Componente de apresentacao (burro): formulario "Nova conta" do mockup "03
// Contas" (modal desktop/mobile), estendido com seletor de icone/cor
// (TASK-127 no back). Estado e submissao vivem no container (ContasPage) -
// clean-code.md "Organizacao (React)". Mesmo padrao visual de overlay do
// ModalNovoAtivo (features/investimentos) - fundo fixo com dialog
// centralizado.
export function FormNovaConta({
  nome,
  tipoSelecionado,
  saldoInicialMascarado,
  iconeSelecionado,
  corSelecionada,
  isSubmitting,
  errorMessage,
  onNomeChange,
  onTipoChange,
  onSaldoInicialChange,
  onIconeChange,
  onCorChange,
  onSubmit,
  onFechar,
}: FormNovaContaProps) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4"
      role="presentation"
      onClick={onFechar}
    >
      <form
        role="dialog"
        aria-modal="true"
        aria-label="Nova conta"
        onClick={(event) => event.stopPropagation()}
        onSubmit={onSubmit}
        className="flex w-full max-w-sm flex-col gap-4 rounded-xl border border-border bg-card px-5 py-5"
      >
        <div className="flex items-center justify-between">
          <h2 className="text-[19px] font-medium text-text-primary">Nova conta</h2>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            onClick={onFechar}
            aria-label="Fechar formulario"
          >
            <X className="size-4" aria-hidden="true" />
          </Button>
        </div>

        {errorMessage && (
          <Alert variant="destructive">
            <AlertDescription>{errorMessage}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="nomeNovaConta">Nome da conta</Label>
          <Input
            id="nomeNovaConta"
            placeholder="Ex: Banco Inter"
            autoFocus
            required
            value={nome}
            onChange={(event) => onNomeChange(event.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="tipoNovaConta">Tipo de conta</Label>
          <select
            id="tipoNovaConta"
            required
            value={tipoSelecionado}
            onChange={(event) => onTipoChange(event.target.value as TipoContaFormulario)}
            className={cn(
              "h-8 w-full min-w-0 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 md:text-sm dark:bg-input/30",
            )}
          >
            {OPCOES_TIPO_CONTA.map((opcao) => (
              <option key={opcao.valor} value={opcao.valor}>
                {opcao.label}
              </option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="saldoInicialNovaConta">Saldo inicial</Label>
          <Input
            id="saldoInicialNovaConta"
            // Sem inputMode="numeric": saldo aceita "-" (regra-de-negocio.md
            // item 10, saldo_manual pode ser negativo) e o teclado numerico
            // do mobile normalmente esconde a tecla de menos.
            required
            value={saldoInicialMascarado}
            onChange={(event) => onSaldoInicialChange(aplicarMascaraMoeda(event.target.value))}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <span id="iconeNovaContaLabel" className="flex items-center gap-2 text-[13px] font-medium text-text-muted">
            Ícone
          </span>
          <div className="flex flex-wrap gap-2" role="group" aria-labelledby="iconeNovaContaLabel">
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
                      : "bg-muted hover:bg-accent-deep/60",
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
          <span id="corNovaContaLabel" className="flex items-center gap-2 text-[13px] font-medium text-text-muted">
            Cor
          </span>
          <div className="flex flex-wrap gap-2" role="group" aria-labelledby="corNovaContaLabel">
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

        <div className="flex justify-end gap-2">
          <Button type="button" variant="ghost" onClick={onFechar} disabled={isSubmitting}>
            Cancelar
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Salvando..." : "Salvar conta"}
          </Button>
        </div>
      </form>
    </div>
  )
}
