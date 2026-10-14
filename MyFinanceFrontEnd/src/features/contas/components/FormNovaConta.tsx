import type { FormEvent } from "react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import {
  CLASSE_BOTAO_CANCELAR,
  CLASSE_BOTAO_PRIMARIO,
  CLASSE_CAMPO,
  FormModal,
} from "@/shared/ui/FormModal"
import { Label } from "@/shared/ui/label"
import { SeletorIconeCor } from "@/features/contas/components/SeletorIconeCor"
import { aplicarMascaraMoeda } from "@/features/contas/lib/mascaraMoeda"
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
    <FormModal title="Nova conta" onClose={onFechar}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
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
            className={CLASSE_CAMPO}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="tipoNovaConta">Tipo de conta</Label>
          <select
            id="tipoNovaConta"
            required
            value={tipoSelecionado}
            onChange={(event) => onTipoChange(event.target.value as TipoContaFormulario)}
            className="w-full min-w-0 rounded-[10px] border border-border bg-accent px-3.5 py-3 text-sm text-text-body transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
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
            className={CLASSE_CAMPO}
          />
        </div>

        <SeletorIconeCor
          idPrefixo="nova-conta"
          iconeSelecionado={iconeSelecionado}
          corSelecionada={corSelecionada}
          onIconeChange={onIconeChange}
          onCorChange={onCorChange}
        />

        <div className="mt-1 flex justify-end gap-2.5">
          <Button
            type="button"
            variant="outline"
            onClick={onFechar}
            disabled={isSubmitting}
            className={CLASSE_BOTAO_CANCELAR}
          >
            Cancelar
          </Button>
          <Button type="submit" disabled={isSubmitting} className={CLASSE_BOTAO_PRIMARIO}>
            {isSubmitting ? "Salvando..." : "Salvar conta"}
          </Button>
        </div>
      </form>
    </FormModal>
  )
}
