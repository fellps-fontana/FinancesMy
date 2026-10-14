import { useState, type FormEvent } from "react"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import {
  CLASSE_BOTAO_CANCELAR,
  CLASSE_BOTAO_PRIMARIO,
  CLASSE_CAMPO,
  FormModal,
} from "@/shared/ui/FormModal"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"
import { SeletorIconeCor } from "@/features/contas/components/SeletorIconeCor"
import { validarNovaContaCartao } from "@/features/cartao/lib/validarNovaContaCartao"
import { useEditarConta } from "@/features/contas/hooks/useEditarConta"
import type { ContaResponse, SubtipoConta, TipoConta } from "@/features/contas/types"

type FormEditarContaProps = {
  conta: ContaResponse
  onFechar: () => void
}

const ROTULO_TIPO: Record<TipoConta, string> = {
  Banco: "Conta bancaria",
  Investimento: "Investimento",
  Cartao: "Cartao de credito",
}

const OPCOES_SUBTIPO: ReadonlyArray<{ valor: SubtipoConta; label: string }> = [
  { valor: "Corrente", label: "Conta corrente" },
  { valor: "Poupanca", label: "Poupança" },
  { valor: "DinheiroFisico", label: "Dinheiro físico" },
]

// Modal "Editar conta" (regra-de-negocio.md item 10.2): nome, subtipo (so
// Banco), icone e cor. O tipo e exibido como texto e nunca enviado. O modal
// abre com os valores atuais; erros 400/404 do backend aparecem no modal.
// Conta Cartao tambem edita dia de fechamento e vencimento (sempre enviados).
export function FormEditarConta({ conta, onFechar }: FormEditarContaProps) {
  const { mutate: editarConta, isPending } = useEditarConta()

  const [nome, setNome] = useState(conta.nome)
  const [subtipo, setSubtipo] = useState<SubtipoConta | "">(conta.subtipo ?? "")
  const [icone, setIcone] = useState<string | null>(conta.icone)
  const [cor, setCor] = useState<string | null>(conta.cor)
  const [diaFechamento, setDiaFechamento] = useState(String(conta.diaFechamento ?? ""))
  const [diaVencimento, setDiaVencimento] = useState(String(conta.diaVencimento ?? ""))
  const [erro, setErro] = useState<string | null>(null)

  const ehBanco = conta.tipo === "Banco"
  const ehCartao = conta.tipo === "Cartao"

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (nome.trim().length === 0) {
      setErro("Informe um nome para a conta.")
      return
    }

    if (ehCartao) {
      const erroDias = validarNovaContaCartao(nome, diaFechamento, diaVencimento)
      if (erroDias) {
        setErro(erroDias)
        return
      }
    }

    editarConta(
      {
        id: conta.id,
        request: {
          nome: nome.trim(),
          subtipo: ehBanco && subtipo !== "" ? subtipo : undefined,
          icone: icone ?? undefined,
          cor: cor ?? undefined,
          diaFechamento: ehCartao ? Number(diaFechamento) : undefined,
          diaVencimento: ehCartao ? Number(diaVencimento) : undefined,
        },
      },
      {
        onSuccess: onFechar,
        onError: (error) => {
          console.error("Falha ao editar conta", error)
          setErro(
            error instanceof ApiError ? error.message : "Nao foi possivel salvar a conta. Tente novamente.",
          )
        },
      },
    )
  }

  return (
    <FormModal title="Editar conta" onClose={onFechar}>
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {erro && (
          <Alert variant="destructive">
            <AlertDescription>{erro}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor={`nomeEditarConta-${conta.id}`}>Nome da conta</Label>
          <Input
            id={`nomeEditarConta-${conta.id}`}
            autoFocus
            required
            value={nome}
            onChange={(event) => setNome(event.target.value)}
            className={CLASSE_CAMPO}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <span className="text-[13px] font-medium text-text-muted">Tipo de conta</span>
          <div className="rounded-[10px] border border-border bg-background px-3.5 py-3 text-sm text-text-body">
            {ROTULO_TIPO[conta.tipo]}
          </div>
          <span className="text-xs text-text-faint">O tipo não pode ser alterado.</span>
        </div>

        {ehBanco && (
          <div className="flex flex-col gap-1.5">
            <Label htmlFor={`subtipoEditarConta-${conta.id}`}>Subtipo</Label>
            <select
              id={`subtipoEditarConta-${conta.id}`}
              value={subtipo}
              onChange={(event) => setSubtipo(event.target.value as SubtipoConta)}
              className="w-full min-w-0 rounded-[10px] border border-border bg-accent px-3.5 py-3 text-sm text-text-body transition-colors outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
            >
              {subtipo === "" && (
                <option value="" disabled>
                  Selecione o subtipo
                </option>
              )}
              {OPCOES_SUBTIPO.map((opcao) => (
                <option key={opcao.valor} value={opcao.valor}>
                  {opcao.label}
                </option>
              ))}
            </select>
          </div>
        )}

        {ehCartao && (
          <div className="flex flex-col gap-1.5">
            <div className="grid grid-cols-2 gap-2.5">
              <div className="flex min-w-0 flex-col gap-1.5">
                <Label htmlFor={`diaFechamentoEditarConta-${conta.id}`}>Dia de fechamento</Label>
                <Input
                  id={`diaFechamentoEditarConta-${conta.id}`}
                  type="number"
                  step="1"
                  min="1"
                  max="31"
                  inputMode="numeric"
                  required
                  value={diaFechamento}
                  onChange={(event) => setDiaFechamento(event.target.value)}
                  className={CLASSE_CAMPO}
                />
              </div>
              <div className="flex min-w-0 flex-col gap-1.5">
                <Label htmlFor={`diaVencimentoEditarConta-${conta.id}`}>Dia de vencimento</Label>
                <Input
                  id={`diaVencimentoEditarConta-${conta.id}`}
                  type="number"
                  step="1"
                  min="1"
                  max="31"
                  inputMode="numeric"
                  required
                  value={diaVencimento}
                  onChange={(event) => setDiaVencimento(event.target.value)}
                  className={CLASSE_CAMPO}
                />
              </div>
            </div>
            <span className="text-xs text-text-faint">Os novos dias valem a partir da próxima fatura.</span>
          </div>
        )}

        <SeletorIconeCor
          idPrefixo={`editar-conta-${conta.id}`}
          iconeSelecionado={icone}
          corSelecionada={cor}
          onIconeChange={setIcone}
          onCorChange={setCor}
        />

        <div className="mt-1 flex justify-end gap-2.5">
          <Button
            type="button"
            variant="outline"
            onClick={onFechar}
            disabled={isPending}
            className={CLASSE_BOTAO_CANCELAR}
          >
            Cancelar
          </Button>
          <Button type="submit" disabled={isPending} className={CLASSE_BOTAO_PRIMARIO}>
            {isPending ? "Salvando..." : "Salvar conta"}
          </Button>
        </div>
      </form>
    </FormModal>
  )
}
