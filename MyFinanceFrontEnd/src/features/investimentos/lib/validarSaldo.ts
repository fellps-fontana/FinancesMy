// Reexport da fonte unica (correcao do style, Bloco G/Contas): validarSaldo/
// converterSaldoParaNumero viviam duplicados aqui e em features/contas, ambos
// validando o mesmo campo de dominio (Conta.saldo_manual, regra-de-negocio.md
// item 10) - a fonte unica agora vive em shared/lib. Este arquivo so existe
// pra nao quebrar quem ja importa daqui dentro de features/investimentos.
export { validarSaldo, converterSaldoParaNumero } from "@/shared/lib/validarSaldo"
