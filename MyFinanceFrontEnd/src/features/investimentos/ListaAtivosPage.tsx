import { useMemo, useState, type FormEvent } from "react"
import { Plus, Power } from "lucide-react"
import { useContas } from "@/features/contas/hooks/useContas"
import { useAtivos } from "@/features/investimentos/hooks/useAtivos"
import { useResumoAtivos } from "@/features/investimentos/hooks/useResumoAtivos"
import { useTotalInvestido } from "@/features/investimentos/hooks/useTotalInvestido"
import { useCriarAtivo } from "@/features/investimentos/hooks/useCriarAtivo"
import { useDesativarAtivo } from "@/features/investimentos/hooks/useDesativarAtivo"
import { ResumoAtivosCards } from "@/features/investimentos/components/ResumoAtivosCards"
import { CarteiraDonut } from "@/features/investimentos/components/CarteiraDonut"
import { GraficoRendimentosPorTipo } from "@/features/investimentos/components/GraficoRendimentosPorTipo"
import { GraficoHistoricoAportes } from "@/features/investimentos/components/GraficoHistoricoAportes"
import { FiltroTipoAtivo } from "@/features/investimentos/components/FiltroTipoAtivo"
import { TabelaAtivos } from "@/features/investimentos/components/TabelaAtivos"
import { AcoesAtivoSheet, type AcaoAtivo } from "@/features/investimentos/components/AcoesAtivoSheet"
import { ModalNovoAtivo } from "@/features/investimentos/components/ModalNovoAtivo"
import { VincularContaAtivoModal } from "@/features/investimentos/components/VincularContaAtivoModal"
import { FormRegistrarAporte } from "@/features/investimentos/components/FormRegistrarAporte"
import { FormRegistrarDividendo } from "@/features/investimentos/components/FormRegistrarDividendo"
import { FormValorAtualAtivo } from "@/features/investimentos/components/FormValorAtualAtivo"
import { calcularFatiasCarteira } from "@/features/investimentos/lib/calcularFatiasCarteira"
import { calcularTotalNovoAtivo } from "@/features/investimentos/lib/calcularTotalNovoAtivo"
import { filtrarContasInvestimento } from "@/features/investimentos/lib/filtrarContasInvestimento"
import { validarNovoAtivo } from "@/features/investimentos/lib/validarNovoAtivo"
import { converterValorParaNumero } from "@/features/investimentos/lib/validarValorPositivo"
import {
  filtrarAtivosPorTipo,
  type FiltroTipoAtivo as FiltroTipoAtivoValue,
} from "@/features/investimentos/lib/filtrarAtivosPorTipo"
import { dataDeHoje } from "@/features/investimentos/lib/dataDeHoje"
import { ApiError } from "@/shared/api/client"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { ConfirmDialog } from "@/shared/ui/confirm-dialog"
import { FormModal } from "@/shared/ui/FormModal"
import { PageHeader } from "@/shared/ui/page-header"
import type { AtivoResponse, TipoAtivo } from "@/features/investimentos/types"

// Uma unica superficie aberta por vez, sobre um ativo escolhido na lista.
type Superficie =
  | { tipo: "acoes"; ativo: AtivoResponse }
  | { tipo: "aporte"; ativo: AtivoResponse }
  | { tipo: "dividendo"; ativo: AtivoResponse }
  | { tipo: "valorAtual"; ativo: AtivoResponse }
  | { tipo: "historico"; ativo: AtivoResponse }
  | { tipo: "conta"; ativo: AtivoResponse }
  | { tipo: "desativar"; ativo: AtivoResponse }
  | null

// Container: estado de servidor via React Query (ativos, resumo, total
// investido, contas) e estado de UI (filtro, superficie aberta, formulario de
// novo ativo). Percentuais, fatias do donut e total do formulario vem de
// lib/ ou do backend; a apresentacao fica nos componentes. Regra item 8/8.5:
// todo ativo pertence a uma conta de investimento.
export function ListaAtivosPage() {
  const { data: ativos, isLoading: carregandoAtivos, error: erroAtivos } = useAtivos()
  const { data: resumo, isLoading: carregandoResumo, error: erroResumo } = useResumoAtivos()
  const { data: totalInvestido, error: erroTotal } = useTotalInvestido()
  const { data: contas, isLoading: carregandoContas } = useContas()
  const { mutate: criarAtivo, isPending: criandoAtivo } = useCriarAtivo()
  const { mutate: desativarAtivo, isPending: desativando } = useDesativarAtivo()

  const [filtro, setFiltro] = useState<FiltroTipoAtivoValue>("Todos")
  const [superficie, setSuperficie] = useState<Superficie>(null)
  const [erroAcao, setErroAcao] = useState<string | null>(null)

  const [modalAberto, setModalAberto] = useState(false)
  const [nome, setNome] = useState("")
  const [tipo, setTipo] = useState<TipoAtivo>("RendaFixa")
  const [quantidade, setQuantidade] = useState("")
  const [precoUnitario, setPrecoUnitario] = useState("")
  const [dataCompra, setDataCompra] = useState(dataDeHoje)
  const [contaId, setContaId] = useState("")
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const erro = erroAtivos ?? erroResumo ?? erroTotal

  if (erroAtivos) console.error("Falha ao carregar ativos", erroAtivos)
  if (erroResumo) console.error("Falha ao carregar resumo de ativos", erroResumo)
  if (erroTotal) console.error("Falha ao carregar total investido", erroTotal)

  const contasInvestimento = useMemo(() => filtrarContasInvestimento(contas ?? []), [contas])
  const carteira = useMemo(() => calcularFatiasCarteira(ativos ?? []), [ativos])
  const ativosFiltrados = filtrarAtivosPorTipo(ativos ?? [], filtro)

  function fechar() {
    setSuperficie(null)
  }

  function abrirModal() {
    setNome("")
    setTipo("RendaFixa")
    setQuantidade("")
    setPrecoUnitario("")
    setDataCompra(dataDeHoje())
    // Conta unica disponivel ja vem selecionada; senao o usuario escolhe.
    setContaId(contasInvestimento.length === 1 ? contasInvestimento[0].id : "")
    setErroFormulario(null)
    setModalAberto(true)
  }

  function fecharModal() {
    setModalAberto(false)
    setErroFormulario(null)
  }

  function handleSubmitNovoAtivo(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarNovoAtivo(nome, contaId, quantidade, precoUnitario, dataCompra)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    criarAtivo(
      {
        nome: nome.trim(),
        tipo,
        contaId,
        quantidade: converterValorParaNumero(quantidade),
        precoUnitario: converterValorParaNumero(precoUnitario),
        dataCompra,
      },
      {
        onSuccess: fecharModal,
        onError: (error) => {
          console.error("Falha ao criar ativo", error)
          setErroFormulario(
            error instanceof ApiError ? error.message : "Nao foi possivel salvar o ativo. Tente novamente.",
          )
        },
      },
    )
  }

  function handleAcao(acao: AcaoAtivo, ativo: AtivoResponse) {
    setErroAcao(null)
    setSuperficie({ tipo: acao, ativo })
  }

  function handleConfirmarDesativar(ativo: AtivoResponse) {
    setErroAcao(null)
    desativarAtivo(ativo.id, {
      onSuccess: fechar,
      onError: (error) => {
        console.error("Falha ao desativar ativo", error)
        fechar()
        setErroAcao(
          error instanceof ApiError ? error.message : "Nao foi possivel desativar o ativo. Tente novamente.",
        )
      },
    })
  }

  const ativoDasAcoes = superficie?.tipo === "acoes" ? superficie.ativo : null
  const mensagemVazia =
    ativos && ativos.length > 0
      ? "Nenhum ativo desse tipo ainda."
      : 'Nenhum ativo cadastrado ainda. Use "Novo ativo" para comecar.'

  return (
    <div className="flex flex-col gap-4 md:gap-5">
      <PageHeader
        title="Investimentos"
        action={
          <>
            <Button
              type="button"
              onClick={abrirModal}
              className="hidden h-auto gap-1.5 rounded-[9px] px-4 py-2.5 text-[13px] md:inline-flex"
            >
              <Plus className="size-3.5" strokeWidth={2} aria-hidden="true" />
              Novo ativo
            </Button>
            <Button
              type="button"
              onClick={abrirModal}
              aria-label="Novo ativo"
              className="size-8 rounded-[9px] p-0 md:hidden"
            >
              <Plus className="size-4" strokeWidth={2} aria-hidden="true" />
            </Button>
          </>
        }
      />

      {erro ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar os investimentos</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : (
        <>
          <ResumoAtivosCards
            totalInvestido={totalInvestido?.totalInvestido}
            resumo={resumo}
            carregando={carregandoResumo}
          />

          {erroAcao && (
            <Alert variant="destructive">
              <AlertDescription>{erroAcao}</AlertDescription>
            </Alert>
          )}

          <FiltroTipoAtivo filtro={filtro} onFiltroChange={setFiltro} />

          {carregandoAtivos ? (
            <p className="text-sm text-text-muted">Carregando ativos...</p>
          ) : (
            <div className="flex flex-col gap-4 xl:grid xl:grid-cols-[minmax(240px,0.7fr)_minmax(0,1.7fr)] xl:items-start">
              <CarteiraDonut carteira={carteira} className="hidden md:flex" />
              <TabelaAtivos
                ativos={ativosFiltrados}
                mensagemVazia={mensagemVazia}
                onVincularConta={(ativo) => handleAcao("conta", ativo)}
                onAbrirAcoes={(ativo) => setSuperficie({ tipo: "acoes", ativo })}
              />
            </div>
          )}

          <GraficoRendimentosPorTipo />
        </>
      )}

      {modalAberto && (
        <ModalNovoAtivo
          nome={nome}
          tipo={tipo}
          quantidade={quantidade}
          precoUnitario={precoUnitario}
          dataCompra={dataCompra}
          contaId={contaId}
          contasInvestimento={contasInvestimento}
          carregandoContas={carregandoContas}
          totalCalculado={calcularTotalNovoAtivo(quantidade, precoUnitario)}
          isSubmitting={criandoAtivo}
          errorMessage={erroFormulario}
          onNomeChange={setNome}
          onTipoChange={setTipo}
          onQuantidadeChange={setQuantidade}
          onPrecoUnitarioChange={setPrecoUnitario}
          onDataCompraChange={setDataCompra}
          onContaIdChange={setContaId}
          onSubmit={handleSubmitNovoAtivo}
          onFechar={fecharModal}
        />
      )}

      <AcoesAtivoSheet ativo={ativoDasAcoes} onAcao={handleAcao} onFechar={fechar} />

      {superficie?.tipo === "aporte" && (
        <FormRegistrarAporte key={superficie.ativo.id} ativo={superficie.ativo} onFechar={fechar} />
      )}
      {superficie?.tipo === "dividendo" && (
        <FormRegistrarDividendo
          key={superficie.ativo.id}
          ativoId={superficie.ativo.id}
          ativoNome={superficie.ativo.nome}
          onFechar={fechar}
        />
      )}
      {superficie?.tipo === "valorAtual" && (
        <FormValorAtualAtivo key={superficie.ativo.id} ativo={superficie.ativo} onFechar={fechar} />
      )}
      {superficie?.tipo === "conta" && (
        <VincularContaAtivoModal
          key={superficie.ativo.id}
          ativo={superficie.ativo}
          contasInvestimento={contasInvestimento}
          onFechar={fechar}
        />
      )}
      {superficie?.tipo === "historico" && (
        <FormModal title={`Historico de aportes - ${superficie.ativo.nome}`} onClose={fechar}>
          <GraficoHistoricoAportes ativoId={superficie.ativo.id} />
        </FormModal>
      )}

      <ConfirmDialog
        open={superficie?.tipo === "desativar"}
        title={superficie?.tipo === "desativar" ? `Desativar ${superficie.ativo.nome}?` : "Desativar"}
        description="O ativo sai da carteira e do total investido."
        icon={<Power className="size-[18px]" />}
        destructive
        confirmLabel="Desativar"
        loading={desativando}
        onCancel={fechar}
        onConfirm={() => {
          if (superficie?.tipo === "desativar") handleConfirmarDesativar(superficie.ativo)
        }}
      />
    </div>
  )
}
