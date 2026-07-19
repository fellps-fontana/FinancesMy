import { useState, type FormEvent } from "react"
import { Link } from "react-router-dom"
import { useAtivos } from "@/features/investimentos/hooks/useAtivos"
import { useResumoAtivos } from "@/features/investimentos/hooks/useResumoAtivos"
import { useCriarAtivo } from "@/features/investimentos/hooks/useCriarAtivo"
import { ResumoAtivosCards } from "@/features/investimentos/components/ResumoAtivosCards"
import { FiltroTipoAtivo } from "@/features/investimentos/components/FiltroTipoAtivo"
import { AtivoItem } from "@/features/investimentos/components/AtivoItem"
import { ModalNovoAtivo } from "@/features/investimentos/components/ModalNovoAtivo"
import { validarNovoAtivo } from "@/features/investimentos/lib/validarNovoAtivo"
import { converterValorParaNumero } from "@/features/investimentos/lib/validarValorPositivo"
import { filtrarAtivosPorTipo, type FiltroTipoAtivo as FiltroTipoAtivoValue } from "@/features/investimentos/lib/filtrarAtivosPorTipo"
import { dataDeHoje } from "@/features/investimentos/lib/dataDeHoje"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { ApiError } from "@/shared/api/client"
import type { TipoAtivo } from "@/features/investimentos/types"

// Container: le o estado de servidor (React Query - lista de ativos e resumo
// por tipo) e decide qual estado exibir. Renderizacao pura fica nos
// componentes de apresentacao chamados abaixo (ResumoAtivosCards,
// FiltroTipoAtivo, AtivoItem, ModalNovoAtivo) - ver clean-code.md
// "Organizacao (React)". Fiel ao mockup "11 Investimentos": tela inteira
// sobre Ativo (regra-de-negocio.md item 8), sem sidebar/shell novo - segue o
// mesmo padrao de coluna unica ja usado no resto do app (ver
// ListaContasSimplesPage.tsx, ContaCartaoPage.tsx).
export function ListaAtivosPage() {
  const { data: ativos, isLoading: carregandoAtivos, error: erroAtivos } = useAtivos()
  const { data: resumo, isLoading: carregandoResumo, error: erroResumo } = useResumoAtivos()
  const { mutate: criarAtivo, isPending: criandoAtivo } = useCriarAtivo()

  const [filtro, setFiltro] = useState<FiltroTipoAtivoValue>("Todos")

  const [modalAberto, setModalAberto] = useState(false)
  const [nome, setNome] = useState("")
  const [tipo, setTipo] = useState<TipoAtivo>("RendaFixa")
  const [instituicao, setInstituicao] = useState("")
  const [valorInvestido, setValorInvestido] = useState("")
  const [dataCompra, setDataCompra] = useState(dataDeHoje)
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)

  const erro = erroAtivos ?? erroResumo

  // Log com contexto (qual query falhou) antes de exibir a mensagem generica
  // ao usuario - ver clean-code.md "Tratamento de erro": falha nao pode ser
  // silenciosa, mesmo quando a UI so mostra um aviso generico.
  if (erroAtivos) {
    console.error("Falha ao carregar ativos", erroAtivos)
  }
  if (erroResumo) {
    console.error("Falha ao carregar resumo de ativos", erroResumo)
  }

  function abrirModal() {
    setNome("")
    setTipo("RendaFixa")
    setInstituicao("")
    setValorInvestido("")
    setDataCompra(dataDeHoje())
    setErroFormulario(null)
    setModalAberto(true)
  }

  function fecharModal() {
    setModalAberto(false)
    setErroFormulario(null)
  }

  function handleSubmitNovoAtivo(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const erroValidacao = validarNovoAtivo(nome, instituicao, valorInvestido, dataCompra)
    if (erroValidacao) {
      setErroFormulario(erroValidacao)
      return
    }

    criarAtivo(
      {
        nome: nome.trim(),
        tipo,
        instituicao: instituicao.trim(),
        valorInvestido: converterValorParaNumero(valorInvestido),
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

  const ativosFiltrados = filtrarAtivosPorTipo(ativos ?? [], filtro)

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-[19px] font-medium text-text-primary">Investimentos</h1>
          <p className="text-sm text-text-muted">
            Tesouro Selic, CDB, acoes e fundos - posicoes individuais, valor atual editado por
            voce.
          </p>
          <Link className="text-sm text-primary underline-offset-4 hover:underline" to="/contas">
            Ver contas simples (cofrinho, XP)
          </Link>
        </div>
        <Button type="button" onClick={abrirModal}>
          Novo ativo
        </Button>
      </header>

      {erro ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar os investimentos</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : (
        <>
          <ResumoAtivosCards resumo={resumo} carregando={carregandoResumo} />

          <FiltroTipoAtivo filtro={filtro} onFiltroChange={setFiltro} />

          {carregandoAtivos ? (
            <p className="text-sm text-text-muted">Carregando ativos...</p>
          ) : ativosFiltrados.length > 0 ? (
            <div className="flex flex-col gap-3">
              {ativosFiltrados.map((ativo) => (
                <AtivoItem key={ativo.id} ativo={ativo} />
              ))}
            </div>
          ) : (
            <p className="text-sm text-text-muted">
              {ativos && ativos.length > 0
                ? "Nenhum ativo desse tipo ainda."
                : "Nenhum ativo cadastrado ainda. Use \"Novo ativo\" para comecar."}
            </p>
          )}
        </>
      )}

      {modalAberto && (
        <ModalNovoAtivo
          nome={nome}
          tipo={tipo}
          instituicao={instituicao}
          valorInvestido={valorInvestido}
          dataCompra={dataCompra}
          isSubmitting={criandoAtivo}
          errorMessage={erroFormulario}
          onNomeChange={setNome}
          onTipoChange={setTipo}
          onInstituicaoChange={setInstituicao}
          onValorInvestidoChange={setValorInvestido}
          onDataCompraChange={setDataCompra}
          onSubmit={handleSubmitNovoAtivo}
          onFechar={fecharModal}
        />
      )}
    </div>
  )
}
