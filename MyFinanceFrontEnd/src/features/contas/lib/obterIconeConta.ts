import {
  Banknote,
  Building2,
  Coins,
  CreditCard,
  Home,
  Landmark,
  PiggyBank,
  TrendingUp,
  Wallet,
  type LucideIcon,
} from "lucide-react"
import type { ContaResponse } from "@/features/contas/types"

// `icone` (TASK-127) e texto livre digitado no cadastro da conta - sem enum
// no backend (DTOs/Conta/CriarContaRequest.cs). O front so reconhece um
// vocabulario curado de nomes (mesmo espirito de nome de icone usado nos
// testes do backend: "home", "credit-card") e cai num fallback previsivel
// para qualquer outro valor, em vez de tentar renderizar string arbitraria
// como componente.
//
// EXPORTADO (correcao do style, Bloco G/Contas): fonte unica do catalogo de
// icones - components/FormNovaConta.tsx consome este mesmo mapa pro seletor
// de icone da criacao de conta, em vez de manter uma segunda lista
// nome->icone duplicada e divergente.
export const ICONE_POR_NOME: Record<string, LucideIcon> = {
  home: Home,
  wallet: Wallet,
  "credit-card": CreditCard,
  landmark: Landmark,
  "piggy-bank": PiggyBank,
  "trending-up": TrendingUp,
  banknote: Banknote,
  building: Building2,
  coins: Coins,
}

// Sem `icone` cadastrado, o subtipo (Conta tipo Banco, regra-de-negocio.md
// item 10) da uma pista melhor que um icone generico unico.
const ICONE_POR_SUBTIPO: Record<NonNullable<ContaResponse["subtipo"]>, LucideIcon> = {
  Corrente: Landmark,
  Poupanca: PiggyBank,
  DinheiroFisico: Wallet,
}

// Funcao pura e testavel: recebe os campos de exibicao da conta e devolve o
// componente de icone a renderizar. Nao decide cor - isso e responsabilidade
// do componente de apresentacao (ver components/ContaIcone.tsx), que aplica
// `conta.cor` quando cadastrada.
export function obterIconeConta(
  conta: Pick<ContaResponse, "icone" | "subtipo" | "tipo">,
): LucideIcon {
  if (conta.icone) {
    const iconePorNome = ICONE_POR_NOME[conta.icone]
    if (iconePorNome) {
      return iconePorNome
    }
  }

  if (conta.subtipo) {
    return ICONE_POR_SUBTIPO[conta.subtipo]
  }

  if (conta.tipo === "Investimento") {
    return TrendingUp
  }

  return Wallet
}
