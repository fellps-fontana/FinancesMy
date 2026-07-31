import { useSearchParams } from "react-router-dom"
import { useGastoVsLimiteTodasCategorias } from "@/features/limite-gasto/hooks/useGastoVsLimiteTodasCategorias"
import { ItemComparativoLimite } from "@/features/limite-gasto/components/ItemComparativoLimite"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Card, CardContent } from "@/shared/ui/card"
import { Button } from "@/shared/ui/button"

// Mes calendario corrente (regra-de-negocio.md item 9) - sem seletor de
// mes/ano nesta leva, mesmo padrao ja usado em DashboardPage.tsx e
// LancamentosPage.tsx.
const hoje = new Date()
const anoAtual = hoje.getFullYear()
const mesAtual = hoje.getMonth() + 1

/**
 * Comparativo limite vs. realizado por categoria (regra-de-negocio.md item
 * 14, secao "Onde aparece" - "Relatorio por categoria: comparativo limite vs.
 * realizado"). Rota propria (/limites-gasto), SEPARADA do relatorio de
 * cartao (removido de features/cartao): aquele somava so compra de cartao
 * (item 12); este soma TODO lancamento DEBIT da categoria (avulso e cartao,
 * regime de competencia) contra o valor_limite cadastrado.
 *
 * Layout (mockup 10 - Relatorio por Categoria): lista de categorias em um
 * unico card, cada linha com marcador de cor + nome + barra de progresso +
 * percentual + valor - mesma linguagem visual usada em
 * dashboard/components/LimiteGastoIndicador.tsx (o widget resumido desta
 * mesma tela), so que aqui em pagina cheia. O mockup usa grafico de pizza +
 * lista porque mede a distribuicao de UM total entre categorias; aqui nao ha
 * "total" (cada categoria tem seu proprio orcamento independente, item 14),
 * entao so a lista faz sentido de dominio - o pizza foi propositalmente
 * deixado de fora.
 *
 * Suporte a `?categoriaId=` (deep-link de dashboard/LimiteGastoIndicador ou
 * de tarefa futura): filtro CLIENT-SIDE sobre a lista que
 * useGastoVsLimiteTodasCategorias ja retorna inteira - o endpoint ja traz
 * todas as categorias com limite, entao filtrar aqui evita round-trip extra
 * ao backend so para destacar uma categoria.
 *
 * Container: nao calcula nada de dominio - gastoRealizado, valorLimite,
 * percentualUtilizado e estourado ja vem prontos do backend
 * (useGastoVsLimiteTodasCategorias/TASK-058). Este componente so decide qual
 * estado exibir (carregando/erro/vazio/lista/filtro) e delega a apresentacao
 * de cada categoria a ItemComparativoLimite.
 */
export function ComparativoLimiteGastoPage() {
  const { data: itens, isLoading, error } = useGastoVsLimiteTodasCategorias(anoAtual, mesAtual)
  const [searchParams, setSearchParams] = useSearchParams()
  const categoriaIdFiltro = searchParams.get("categoriaId")

  // Log com contexto antes do aviso generico ao usuario - ver clean-code.md
  // "Tratamento de erro".
  if (error) {
    console.error("Falha ao carregar comparativo de limite de gasto", error)
  }

  const itensExibidos = categoriaIdFiltro
    ? (itens ?? []).filter((item) => item.categoriaId === categoriaIdFiltro)
    : itens
  const categoriaFiltrada = categoriaIdFiltro
    ? itens?.find((item) => item.categoriaId === categoriaIdFiltro)
    : undefined

  function limparFiltro() {
    setSearchParams((atuais) => {
      const proximos = new URLSearchParams(atuais)
      proximos.delete("categoriaId")
      return proximos
    })
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

      {categoriaIdFiltro && (
        <div className="flex items-center justify-between gap-3 rounded-[10px] border border-accent-deep bg-accent-deep/20 px-3 py-2">
          <span className="text-[13px] text-accent-soft">
            Filtrando por {categoriaFiltrada?.categoriaNome ?? "categoria selecionada"}
          </span>
          <Button variant="ghost" size="xs" onClick={limparFiltro}>
            Ver todas
          </Button>
        </div>
      )}

      {error ? (
        <Alert variant="destructive">
          <AlertTitle>Nao foi possivel carregar o comparativo</AlertTitle>
          <AlertDescription>Verifique sua conexao e tente novamente.</AlertDescription>
        </Alert>
      ) : isLoading ? (
        <p className="text-sm text-text-muted">Carregando...</p>
      ) : itens && itens.length > 0 ? (
        itensExibidos && itensExibidos.length > 0 ? (
          <Card>
            <CardContent>
              <ul className="flex flex-col gap-1">
                {itensExibidos.map((item) => (
                  <ItemComparativoLimite
                    key={item.categoriaId}
                    item={item}
                    destacado={item.categoriaId === categoriaIdFiltro}
                  />
                ))}
              </ul>
            </CardContent>
          </Card>
        ) : (
          <p className="text-sm text-text-muted">
            Essa categoria nao tem limite cadastrado neste mes.
          </p>
        )
      ) : (
        <p className="text-sm text-text-muted">Nenhum limite cadastrado ainda.</p>
      )}
    </div>
  )
}
