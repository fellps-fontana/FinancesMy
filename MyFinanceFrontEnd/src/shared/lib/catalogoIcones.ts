import {
  Baby,
  Briefcase,
  Building2,
  Bus,
  Car,
  Coffee,
  CreditCard,
  DollarSign,
  Dumbbell,
  Film,
  Fuel,
  Gamepad2,
  Gift,
  GraduationCap,
  Heart,
  HeartPulse,
  Home,
  Landmark,
  PawPrint,
  PiggyBank,
  Plane,
  Shirt,
  ShoppingBag,
  Smartphone,
  Tag,
  TrendingUp,
  Utensils,
  Wallet,
  Wifi,
  Zap,
  type LucideIcon,
} from "lucide-react"

// Catalogo FIXO de icones, reutilizavel por qualquer modulo que precise de
// um icone de dominio por registro (hoje: Categoria - regra-de-negocio.md
// item 7; amanha: Conta, ainda pendente na fila de tasks). Fixo e nao-livre
// de proposito - o usuario escolhe um id deste catalogo, nunca faz
// upload/URL arbitraria, mesmo espirito da paleta fixa de cor da
// identidade-visual.md (sem color-picker livre).
//
// Vive em shared/lib (nao em features/categorias) porque e um recurso sem
// regra de negocio de UM modulo especifico - mesmo criterio de
// stack.md "Organizacao de pastas (Frontend), shared/lib": funcao/dado puro
// de proposito geral, sem fetch, sem estado.
export type IconeCatalogoOpcao = {
  id: string
  label: string
  Icon: LucideIcon
}

// Icone de fallback quando o registro nao tem `icone` salvo, ou quando o id
// salvo nao existe mais no catalogo (ex: catalogo mudou depois do registro
// ja ter sido salvo) - o render nunca quebra por icone desconhecido.
export const ICONE_CATALOGO_PADRAO: LucideIcon = Tag

export const CATALOGO_ICONES: IconeCatalogoOpcao[] = [
  { id: "home", label: "Casa", Icon: Home },
  { id: "building", label: "Predio", Icon: Building2 },
  { id: "shopping-bag", label: "Compras", Icon: ShoppingBag },
  { id: "utensils", label: "Alimentacao", Icon: Utensils },
  { id: "coffee", label: "Cafe", Icon: Coffee },
  { id: "car", label: "Carro", Icon: Car },
  { id: "bus", label: "Transporte publico", Icon: Bus },
  { id: "fuel", label: "Combustivel", Icon: Fuel },
  { id: "heart", label: "Saude", Icon: Heart },
  { id: "heart-pulse", label: "Saude (exame/plano)", Icon: HeartPulse },
  { id: "graduation-cap", label: "Educacao", Icon: GraduationCap },
  { id: "gamepad", label: "Jogos", Icon: Gamepad2 },
  { id: "film", label: "Entretenimento", Icon: Film },
  { id: "plane", label: "Viagem", Icon: Plane },
  { id: "dumbbell", label: "Academia", Icon: Dumbbell },
  { id: "gift", label: "Presente", Icon: Gift },
  { id: "smartphone", label: "Telefonia", Icon: Smartphone },
  { id: "wifi", label: "Internet", Icon: Wifi },
  { id: "zap", label: "Energia", Icon: Zap },
  { id: "credit-card", label: "Assinatura", Icon: CreditCard },
  { id: "wallet", label: "Carteira", Icon: Wallet },
  { id: "piggy-bank", label: "Cofrinho", Icon: PiggyBank },
  { id: "trending-up", label: "Investimento", Icon: TrendingUp },
  { id: "landmark", label: "Banco", Icon: Landmark },
  { id: "briefcase", label: "Trabalho", Icon: Briefcase },
  { id: "dollar-sign", label: "Renda", Icon: DollarSign },
  { id: "shirt", label: "Vestuario", Icon: Shirt },
  { id: "baby", label: "Filhos", Icon: Baby },
  { id: "paw-print", label: "Pet", Icon: PawPrint },
]

// Busca o componente de icone pelo id salvo no registro (ex: Categoria.icone).
export function obterIconeCatalogo(iconeId?: string | null): LucideIcon {
  if (!iconeId) {
    return ICONE_CATALOGO_PADRAO
  }
  const opcao = CATALOGO_ICONES.find((item) => item.id === iconeId)
  return opcao?.Icon ?? ICONE_CATALOGO_PADRAO
}
