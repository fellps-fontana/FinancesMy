// Reexport da fonte unica (correcao do style, Bloco G/Contas): este arquivo
// era copia literal de features/investimentos/lib/validarSaldo.ts, ambos
// rejeitando saldo negativo - contradizia o backend (que ja aceita e testa
// saldo negativo, ver ContasControllerTests.cs) e a decisao do usuario
// (regra-de-negocio.md item 10: saldo_manual pode ser negativo em toda conta
// manual). A validacao real agora mora em shared/lib/validarSaldo.ts; este
// arquivo so existe pra nao quebrar features/contas/components/ContaItem.tsx,
// que ja importa `validarSaldo`/`converterSaldoParaNumero` deste caminho.
export { validarSaldo, converterSaldoParaNumero } from "@/shared/lib/validarSaldo"
