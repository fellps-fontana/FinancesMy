import { ApiError } from "@/shared/api/client"
import { cn } from "@/shared/lib/utils"
import { useGastoVsLimite } from "@/features/limite-gasto/hooks/useGastoVsLimite"
import { decidirNivelAlerta } from "@/features/lancamentos/lib/limiarAlertaLimite"

type AvisoLimiteGastoProps = {
  categoriaId: string | undefined
  ano: number
  mes: number
}

// Mensagem por nivel (regra-de-negocio.md item 14, "Onde aparece": "Tela de
// lancamento: aviso ao selecionar/criar lancamento numa categoria perto ou
// acima do limite"). Cor segue identidade-visual.md: alerta = atencao,
// negativo = estado ja estourado - nunca bloqueia nada, so informa.
const MENSAGEM_POR_NIVEL = {
  perto: {
    texto: "Voce esta perto do limite desta categoria",
    className: "text-alerta",
  },
  estourado: {
    texto: "Limite desta categoria ja foi ultrapassado",
    className: "text-negativo",
  },
} as const

// Componente de apresentacao, standalone (ainda nao ha form de lancamento
// manual construido - ver features/lancamentos/.gitkeep). So exibe, nunca
// desabilita ou bloqueia submit: item 14 e explicito que o efeito do limite
// e SOMENTE alerta visual. `useGastoVsLimite` ja trata `enabled:
// Boolean(categoriaId)` internamente; passamos "" quando a categoria ainda
// nao foi selecionada so pra satisfazer a assinatura (string, nao
// string | undefined) sem alterar o hook (fora do escopo desta task).
export function AvisoLimiteGasto({ categoriaId, ano, mes }: AvisoLimiteGastoProps) {
  const { data, error } = useGastoVsLimite(categoriaId ?? "", ano, mes)

  // Categoria sem limite cadastrado responde 404 - nao e erro pro usuario,
  // so significa que nao ha limite definido pra essa categoria (regra-de-
  // negocio.md item 14: limite_gasto e 1:1 com categoria, opcional). Qualquer
  // outro erro tambem nao vira mensagem aqui: um aviso que falha em carregar
  // nao pode virar ruido de erro numa tela de lancamento, so ausencia de
  // aviso.
  const categoriaSemLimite = error instanceof ApiError && error.status === 404

  if (!data || categoriaSemLimite) {
    return null
  }

  const nivel = decidirNivelAlerta(data.percentualUtilizado, data.estourado)

  if (nivel === "ok") {
    return null
  }

  const { texto, className } = MENSAGEM_POR_NIVEL[nivel]

  return (
    <p role="status" className={cn("text-[13px] font-medium", className)}>
      {texto}
    </p>
  )
}
