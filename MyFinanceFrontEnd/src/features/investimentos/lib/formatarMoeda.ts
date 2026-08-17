// Reexport da fonte unica (correcao do style, Bloco G/Contas): formatarMoeda
// nao tem regra de negocio de investimentos especifica, e usada por 2+
// features (contas, cartao, contas-receber, contas-fixas, categorias,
// dashboard, limite-gasto) e por isso vive em shared/lib. Este arquivo so
// existe pra nao quebrar os consumidores que ja importam daqui dentro de
// features/investimentos - nao duplicar a implementacao.
export { formatarMoeda } from "@/shared/lib/formatarMoeda"
