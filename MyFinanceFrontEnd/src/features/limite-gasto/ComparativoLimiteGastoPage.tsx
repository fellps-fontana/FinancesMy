import { useGastoVsLimiteTodasCategorias } from "@/features/limite-gasto/hooks/useGastoVsLimiteTodasCategorias"
import { ItemComparativoLimite } from "@/features/limite-gasto/components/ItemComparativoLimite"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"

const hoje = new Date()
const anoAtual = hoje.getFullYear()
const mesAtual = hoje.getMonth() + 1

/**
 * Comparativo limite vs. realizado por categoria (regra-de-negocio.md item
 * 14, secao "Onde aparece" - "Relatorio por categoria: comparativo limite vs.
 * realizado"). Rota propria (/limites-gasto), SEPARADA do relatorio de
 * cartao (features/cartao/RelatorioCategoriaPage): aquele soma so compra de
 * cartao (item 12); este soma TODO lancamento DEBIT da categoria (avulso e
 * cartao, regime de competencia) contra o valor_limite cadastrado - relatorio
 * diferente, mesmo que o nome "relatorio por categoria" apareca nos dois.
 *
 * Container: nao calcula nada de dominio - gastoRealizado, valorLimite,
 * percentualUtilizado e estourado ja vem prontos do backend
 * (useGastoVsLimiteTodasCategorias/TASK-058). Este componente so decide qual
 * estado exibir (carregando/erro/vazio/lista) e delega a apresentacao de
 * cada categoria a ItemComparativoLimite.
 */
export function ComparativoLimiteGastoPage() {
  const { data: itens, isLoading, error } = useGastoVsLimiteTodasCategorias(anoAtual, mesAtual)

  // Log com contexto antes do aviso generico ao usuario - ver clean-code.md
  // "Tratamento de erro".
  if (error) {
    console.error("Falha ao carregar comparativo de limite de gasto", error)
  }

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-[19px] font-medium text-text-primary">Limite de gasto por categoria</h1>
        <p className="text-sm text-text-muted">
          Quanto voce ja gastou neste mes em cada categoria com limite cadastrado, comparado ao
          orcamento definido. So alerta visual - nenhum lancamento e bloqueado por estourar o
          limite.
        </p>
      </header>

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar o comparativo</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : itens && itens.length > 0 ? (
        <div className="flex flex-col gap-3">
          {itens.map((item) => (
            <ItemComparativoLimite key={item.categoriaId} item={item} />
          ))}
        </div>
      ) : (
        <p className="text-sm text-text-muted">Nenhum limite cadastrado ainda.</p>
      )}
    </div>
  )
}
