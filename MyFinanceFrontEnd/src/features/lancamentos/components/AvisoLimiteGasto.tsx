import { cn } from "@/shared/lib/utils"
import { useGastoVsLimite } from "@/features/limite-gasto/hooks/useGastoVsLimite"
import { obterEstadoLimite } from "@/features/limite-gasto/lib/estadoLimite"

type AvisoLimiteGastoProps = {
  categoriaId: string | undefined
  ano: number
  mes: number
}

// Mensagem por nivel (regra-de-negocio.md item 14: aviso ao selecionar/criar
// lancamento numa categoria perto ou acima do sublimite). Cor segue
// identidade-visual.md, em chip com fundo: alerta = atencao,
// negativo = estado ja estourado - nunca bloqueia nada, so informa.
const MENSAGEM_POR_NIVEL = {
  perto: {
    texto: "Voce esta perto do sublimite desta categoria",
    className: "bg-alerta/15 text-alerta",
  },
  estourado: {
    texto: "Sublimite desta categoria ja foi ultrapassado",
    className: "bg-negativo/15 text-negativo",
  },
} as const

// Componente de apresentacao, standalone. So exibe, nunca desabilita ou
// bloqueia submit: item 14 e explicito que o efeito do sublimite e SOMENTE
// alerta visual. `categoriaId` ausente vira "" so pra satisfazer a assinatura
// do hook (string, nao string | undefined); sem categoria nao ha sublimite
// correspondente e nada e exibido.
export function AvisoLimiteGasto({ categoriaId, ano, mes }: AvisoLimiteGastoProps) {
  const { data } = useGastoVsLimite(categoriaId ?? "", ano, mes)

  // `ano`/`mes` sao os da DATA do lancamento (derivados no formulario), nao
  // o mes atual: o sublimite vem do plano daquele mes (item 20). Sem plano
  // ou sem sublimite na categoria, `data` e null e nao ha aviso. Falha de
  // carga tambem nao vira mensagem: um aviso que falha em carregar nao pode
  // virar ruido de erro numa tela de lancamento, so ausencia de aviso.
  if (!data) {
    return null
  }

  const nivel = obterEstadoLimite(data)

  if (nivel === "ok") {
    return null
  }

  const { texto, className } = MENSAGEM_POR_NIVEL[nivel]

  return (
    <p role="status" className={cn("w-fit rounded-[5px] px-2 py-0.5 text-[13px] font-medium", className)}>
      {texto}
    </p>
  )
}
