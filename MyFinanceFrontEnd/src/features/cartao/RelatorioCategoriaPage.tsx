import { useState } from "react"
import { Link } from "react-router-dom"
import { useContaCartaoAtual } from "@/features/cartao/hooks/useContaCartaoAtual"
import { useRelatorioCategoria } from "@/features/cartao/hooks/useRelatorioCategoria"
import { calcularTotalGeral, ordenarItensRelatorio } from "@/features/cartao/lib/relatorioCategoria"
import { formatarMesReferencia, mesAtualIso } from "@/features/cartao/lib/formatarData"
import { formatarMoeda } from "@/features/investimentos/lib/formatarMoeda"
import { Card, CardContent } from "@/shared/ui/card"
import { Alert, AlertDescription, AlertTitle } from "@/shared/ui/alert"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

const formatadorPercentual = new Intl.NumberFormat("pt-BR", {
  style: "percent",
  maximumFractionDigits: 0,
})

/**
 * Relatorio por categoria (regra de negocio item 12, visao CATEGORICA /
 * COMPETENCIA: soma as compras do cartao por categoria dentro do mes,
 * ignorando o pagamento da fatura - que e transferencia e vive so no fluxo
 * de caixa). Rota propria (/cartao/relatorio) em vez de secao dentro de
 * ContaCartaoPage: e um RELATORIO, pode olhar o mes inteiro independente da
 * fatura aberta, nao uma acao sobre o cartao (lancar compra, pagar fatura) -
 * misturar as duas facilitaria a leitura errada de "isto soma com o saldo do
 * cartao logo acima".
 *
 * Container: nao calcula nada de dominio - ordenacao e percentual vem de
 * lib/relatorioCategoria.ts (funcoes puras testaveis), este componente so
 * exibe.
 *
 * GAP CONHECIDO: o backend nao tem, hoje, nenhum controller/service de
 * relatorio (busca por "relatorio" no projeto inteiro nao retorna nenhum
 * arquivo). Esta tela chama o contrato que a funcionalidade precisa (ver
 * api.ts/obterRelatorioCategoria) e mostra o estado de erro correspondente
 * ate o backend implementar o endpoint - nenhum dado mockado.
 */
export function RelatorioCategoriaPage() {
  const [mes, setMes] = useState(mesAtualIso)
  const { contaCartaoAtual } = useContaCartaoAtual()
  const { data, isLoading, isError } = useRelatorioCategoria(mes, contaCartaoAtual?.id)

  const itens = data?.itens ?? []
  const itensOrdenados = ordenarItensRelatorio(itens)
  const totalGeral = calcularTotalGeral(itens)

  return (
    <div className="mx-auto flex min-h-svh max-w-2xl flex-col gap-6 px-4 py-8">
      <Link className="text-sm text-accent underline-offset-4 hover:underline" to="/cartao">
        Voltar para o cartao
      </Link>

      <header className="flex flex-col gap-1">
        <h1 className="text-[19px] font-medium text-text-primary">Relatorio por categoria</h1>
        <p className="text-sm text-text-muted">
          Quanto voce gastou em cada categoria neste mes. Visao de competencia: soma as compras do
          cartao por categoria, sem incluir o pagamento da fatura - esse numero e do fluxo de caixa
          e nao entra aqui.
        </p>
      </header>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="mesRelatorioCategoria">Mes de referencia</Label>
        <Input
          id="mesRelatorioCategoria"
          type="month"
          className="max-w-[180px]"
          value={mes}
          onChange={(event) => setMes(event.target.value)}
        />
      </div>

      {isLoading && <p className="text-sm text-text-muted">Carregando relatorio...</p>}

      {isError && (
        <Alert variant="destructive">
          <AlertTitle>Relatorio ainda nao disponivel</AlertTitle>
          <AlertDescription>
            O backend ainda nao expoe um endpoint de relatorio por categoria. Esta tela ja esta
            pronta para exibir os dados assim que ele existir.
          </AlertDescription>
        </Alert>
      )}

      {!isLoading && !isError && (
        <>
          <Card>
            <CardContent className="flex flex-col gap-1">
              <span className="text-[13px] text-text-muted">
                Total gasto em {formatarMesReferencia(mes)}
              </span>
              <span className="text-[28px] font-medium text-text-primary">
                {formatarMoeda(totalGeral)}
              </span>
            </CardContent>
          </Card>

          {itensOrdenados.length === 0 ? (
            <p className="text-sm text-text-muted">Nenhuma compra registrada neste mes.</p>
          ) : (
            <ul className="flex flex-col gap-3">
              {itensOrdenados.map((item) => (
                <li key={item.categoriaId ?? "sem-categoria"} className="flex flex-col gap-1.5">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-text-body">{item.nomeExibicao}</span>
                    <span className="text-sm font-medium text-text-primary">
                      {formatarMoeda(item.total)}
                    </span>
                  </div>
                  <div className="h-1.5 w-full overflow-hidden rounded-full bg-accent">
                    <div
                      className="h-full rounded-full bg-primary"
                      style={{ width: `${(item.percentual * 100).toFixed(1)}%` }}
                    />
                  </div>
                  <span className="text-[12px] text-text-faint">
                    {formatadorPercentual.format(item.percentual)}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </div>
  )
}
